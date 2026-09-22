using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Graphics;
using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    public enum ModuleKind
    {
        Barricade,
        Cot,
        Water,
        Watchtower,
        Generator,
        Workbench,
        TradingPost,
        Farm,
        Purifier,
        Turret,
        Spikes,
        Oil,
        Crate,
        Lamp,
        Campfire
    }

    [Serializable]
    public class PlacedModule
    {
        public string kind;
        public float x;
        public float z;
        public int rotation;
        public int integrity = 100;
        public int age;
        public int site;
        public int hours;
        public int tier;
        public int job;
        public float lit = -1f;
    }

    public class GridBuilder : MonoBehaviour
    {
        public static GridBuilder Instance { get; private set; }

        [SerializeField] private float cell = 2f;
        [SerializeField] private List<PlacedModule> placed = new List<PlacedModule>();
        private readonly List<GameObject> views = new List<GameObject>();
        private ModuleKind selected = ModuleKind.Barricade;
        private bool buildMode;
        private int facing;
        private float nextOil;

        public bool BuildMode => buildMode;
        public ModuleKind Selected => selected;
        public int Facing => facing;
        public IReadOnlyList<PlacedModule> Placed => placed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            TickOil(Time.time);
            TickLamps();
            TickFires();
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.CampManagement) return;
            if (Player.ExpeditionInput.BuildPressed)
            {
                buildMode = !buildMode;
                GameplayFeedback.Toast(YardSay.Mode(buildMode, null));
            }
            if (!buildMode) return;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                facing = ScrapRefund.Turn(facing);
                GameplayFeedback.Toast(YardSay.Facing(facing, null));
            }
            if (Player.ExpeditionInput.Pointer.x > Screen.width - 400f) return;
            if (PointerRight()) TryDemolishAtPointer();
            else if (PointerPressed()) TryPlaceAtPointer();
        }

        private static bool PointerPressed()
        {
            return UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
        }

        private static bool PointerRight()
        {
            return UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.rightButton.wasPressedThisFrame;
        }

        public void Select(ModuleKind kind) => selected = kind;

        public bool TryDemolish(float worldX, float worldZ)
        {
            float x = Mathf.Round(worldX / cell) * cell;
            float z = Mathf.Round(worldZ / cell) * cell;
            PlacedModule target = null;
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (Mathf.Abs(module.x - x) < 0.01f && Mathf.Abs(module.z - z) < 0.01f) target = module;
            }
            if (target == null) return false;
            int refund = 0;
            if (System.Enum.TryParse(target.kind, out ModuleKind kind)) refund = ScrapRefund.Half(Cost(kind));
            placed.Remove(target);
            RefreshViews();
            if (refund > 0) ColonyStorage.Instance?.RestoreScrap(refund);
            GameplayFeedback.Toast(YardSay.Recovered(refund, null));
            return true;
        }

        private void TryDemolishAtPointer()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Player.ExpeditionInput.Pointer);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float enter)) return;
            Vector3 point = ray.GetPoint(enter);
            TryDemolish(point.x, point.z);
        }

        public void Grow()
        {
            var plots = new CampYield.Plot[placed.Count];
            for (int i = 0; i < placed.Count; i++)
            {
                plots[i].Kind = placed[i].site == 0 ? placed[i].kind : "";
                plots[i].Age = placed[i].age;
                plots[i].Integrity = placed[i].integrity;
            }
            CampYield.Advance(plots);
            bool rain = WeatherController.Instance != null && SkyBand.Rains(WeatherController.Instance.Kind);
            CampYield.Produce(plots, rain, out int food, out int water);
            for (int i = 0; i < placed.Count; i++) placed[i].age = plots[i].Age;
            if (ColonyStorage.Instance == null) return;
            if (food > 0) ColonyStorage.Instance.AddFood(food);
            if (water > 0) ColonyStorage.Instance.AddWater(water);
        }

        public void TryPlaceAtPointer()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Player.ExpeditionInput.Pointer);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float enter)) return;
            Vector3 point = ray.GetPoint(enter);
            TryPlace(selected, point);
        }

        public bool TryPlace(ModuleKind kind, Vector3 world)
        {
            float x = Mathf.Round(world.x / cell) * cell;
            float z = Mathf.Round(world.z / cell) * cell;
            if (Occupied(placed, x, z))
            {
                GameplayFeedback.Toast(YardSay.Taken(null));
                return false;
            }

            int cost = Cost(kind);
            if (ColonyStorage.Instance == null || !ColonyStorage.Instance.TrySpendScrap(cost))
            {
                GameplayFeedback.Toast(YardSay.Need(cost, null));
                return false;
            }
            var record = new PlacedModule { kind = kind.ToString(), x = x, z = z, rotation = facing, integrity = 100, site = 1 };
            placed.Add(record);
            SpawnView(record);
            GameplayFeedback.Toast(YardSay.Marked(kind.ToString(), null));
            return true;
        }

        public bool Raise(int pace)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.site == 0 || module.integrity <= 0) continue;
                BuildSite.Work(module.site, module.hours, BuildSite.Need(module.kind), pace, out int nextSite, out int nextHours, out bool finished);
                module.site = nextSite;
                module.hours = nextHours;
                RefreshViews();
                if (finished) GameplayFeedback.Toast(YardSay.Up(module.kind, null));
                return true;
            }
            return false;
        }

        public bool Patch(int pace)
        {
            int best = -1;
            int lowest = 100;
            for (int i = 0; i < placed.Count; i++)
            {
                if (!MendBoard.Needs(placed[i].site, placed[i].integrity)) continue;
                if (placed[i].integrity >= lowest) continue;
                lowest = placed[i].integrity;
                best = i;
            }
            if (best < 0) return false;
            var module = placed[best];
            int next = MendBoard.Mend(module.integrity, pace);
            bool whole = next >= 100;
            module.integrity = next;
            RefreshViews();
            GameplayFeedback.Toast(YardSay.Mend(module.kind, whole, null));
            return true;
        }

        public void Restore(PlacedModule[] modules)
        {
            ClearViews();
            placed = modules == null ? new List<PlacedModule>() : new List<PlacedModule>(modules);
            foreach (var module in placed) SpawnView(module);
        }

        public void ClearAll()
        {
            placed.Clear();
            ClearViews();
        }

        private void TickLamps()
        {
            bool powered = CampServices.Instance != null && CampServices.Instance.GeneratorOnline;
            int count = placed.Count < views.Count ? placed.Count : views.Count;
            for (int i = 0; i < count; i++)
            {
                if (placed[i].kind != "Lamp" || views[i] == null) continue;
                bool on = powered && BuildSite.Ready(placed[i].site, placed[i].integrity);
                var bulb = views[i].GetComponent<Light>();
                if (bulb != null) bulb.enabled = on;
                var source = views[i].GetComponent<LightSource>();
                if (source != null) source.enabled = on;
                var mote = views[i].GetComponent<YardMote>();
                if (mote == null) mote = views[i].AddComponent<YardMote>();
                if (YardGlow.SparksDue(placed[i].site, placed[i].integrity, Time.time, mote.Last))
                {
                    mote.Last = Time.time;
                    CombatVfx.Sparks(views[i].transform.position);
                    AudioManager.Instance?.PlayAt("spit", views[i].transform.position, YardGlow.Spit);
                }
                else if (!MendBoard.Needs(placed[i].site, placed[i].integrity))
                {
                    mote.Last = 0f;
                }
            }
        }

        private void TickFires()
        {
            int count = placed.Count < views.Count ? placed.Count : views.Count;
            for (int i = 0; i < count; i++)
            {
                if (placed[i].kind != "Campfire" || views[i] == null) continue;
                bool ready = BuildSite.Ready(placed[i].site, placed[i].integrity);
                var ember = views[i].GetComponent<Light>();
                if (ember != null)
                {
                    ember.enabled = ready;
                    ember.intensity = FirePulse.Peak * FirePulse.Scale(Time.time, ready);
                }
                var mote = views[i].GetComponent<YardMote>();
                if (mote == null) mote = views[i].AddComponent<YardMote>();
                if (YardGlow.EmbersDue(ready, Time.time, mote.Last))
                {
                    mote.Last = Time.time;
                    CombatVfx.Embers(views[i].transform.position);
                }
                else if (!ready)
                {
                    mote.Last = 0f;
                }
            }
        }

        private void SpawnView(PlacedModule module)
        {
            var view = GameObject.CreatePrimitive(PrimitiveType.Cube);
            view.name = "Module_" + module.kind;
            bool flat = module.kind == "Spikes" || module.kind == "Oil" || module.kind == "Campfire";
            float y = flat ? 0.04f : module.kind == "Lamp" ? 1.2f : 0.6f;
            view.transform.position = new Vector3(module.x, y, module.z);
            view.transform.rotation = Quaternion.Euler(0f, module.rotation, 0f);
            view.transform.localScale = Scale(module.kind, module.integrity);
            if (module.site != 0)
            {
                float bulk = BuildSite.Bulk(module.hours);
                view.transform.localScale = new Vector3(
                    view.transform.localScale.x * bulk,
                    view.transform.localScale.y * bulk,
                    view.transform.localScale.z * bulk);
            }
            var renderer = view.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = module.site != 0
                    ? Color.Lerp(ColorFor(module.kind), new Color(0.72f, 0.7f, 0.58f), module.hours > 0 ? 0.35f : 0.7f)
                    : module.kind == "Oil" && module.lit >= 0f
                    ? new Color(0.95f, 0.42f, 0.08f)
                    : ColorFor(module.kind);
            }
            if (module.kind == "Spikes" || module.kind == "Oil" || module.kind == "Campfire")
            {
                var pad = view.GetComponent<Collider>();
                if (pad != null) Destroy(pad);
            }
            else
            {
                var obstacle = view.AddComponent<NavMeshObstacle>();
                obstacle.carving = true;
                obstacle.shape = NavMeshObstacleShape.Box;
            }
            if (module.kind == "Barricade")
            {
                view.layer = GameLayers.Environment;
                view.AddComponent<Combat.StreetBoard>().LinkToCamp();
            }
            if (module.kind == "Lamp")
            {
                var bulb = view.AddComponent<Light>();
                bulb.type = LightType.Point;
                bulb.range = FloodBeam.Radius;
                bulb.intensity = 3.4f;
                bulb.color = new Color(1f, 0.93f, 0.75f);
                bulb.enabled = false;
                var source = view.AddComponent<LightSource>();
                source.Configure(FloodBeam.Radius);
                source.enabled = false;
            }
            if (module.kind == "Generator") YardFlood.Raise(view);
            if (module.kind == "Campfire")
            {
                var ember = view.AddComponent<Light>();
                ember.type = LightType.Point;
                ember.range = 6f;
                ember.intensity = FirePulse.Peak;
                ember.color = new Color(1f, 0.45f, 0.15f);
                ember.enabled = false;
            }
            views.Add(view);
        }

        private void ClearViews()
        {
            foreach (var view in views)
            {
                if (view != null) Destroy(view);
            }
            views.Clear();
        }

        public int MendCount()
        {
            int count = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                if (MendBoard.Needs(placed[i].site, placed[i].integrity)) count++;
            }
            return count;
        }

        public int CountKind(string kind)
        {
            int count = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].kind == kind && BuildSite.Ready(placed[i].site, placed[i].integrity)) count++;
            }
            return count;
        }

        public int CoverCount(string approach)
        {
            RaidPlan.AnchorOf(approach, out float ax, out float az);
            int count = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.kind != "Barricade" || !BuildSite.Ready(module.site, module.integrity)) continue;
                if (RaidPlan.Covers(ax, az, module.x, module.z)) count++;
            }
            return count;
        }

        public bool StrikeBarricade(int amount)
        {
            return StrikeFrom("gate", amount);
        }

        public bool StrikeAt(float x, float z, float amount)
        {
            PlacedModule target = null;
            float best = 6.25f;
            foreach (var module in placed)
            {
                if (module.kind != "Barricade" || !BuildSite.Ready(module.site, module.integrity)) continue;
                float dx = module.x - x;
                float dz = module.z - z;
                float distance = dx * dx + dz * dz;
                if (distance > best) continue;
                best = distance;
                target = module;
            }
            if (target == null) return false;
            target.integrity = Mathf.Max(0, target.integrity - Mathf.Max(1, Mathf.RoundToInt(amount)));
            if (target.integrity > 0)
            {
                RefreshViews();
                return false;
            }
            placed.Remove(target);
            RefreshViews();
            GameplayFeedback.Toast(YardSay.Barricade(null));
            return true;
        }

        public bool StrikeFrom(string approach, int amount)
        {
            RaidPlan.AnchorOf(approach, out float ax, out float az);
            PlacedModule target = null;
            float best = float.MaxValue;
            foreach (var module in placed)
            {
                if (module.kind != "Barricade" || !BuildSite.Ready(module.site, module.integrity)) continue;
                float dx = module.x - ax;
                float dz = module.z - az;
                float distance = dx * dx + dz * dz;
                if (distance >= best) continue;
                best = distance;
                target = module;
            }
            if (target == null) return false;
            target.integrity = Mathf.Max(0, target.integrity - Mathf.Max(1, amount));
            if (target.integrity > 0)
            {
                RefreshViews();
                return false;
            }
            placed.Remove(target);
            RefreshViews();
            GameplayFeedback.Toast(YardSay.Barricade(null));
            return true;
        }

        public bool BoardStands(float x, float z)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.kind != "Barricade" || !BuildSite.Ready(module.site, module.integrity)) continue;
                float dx = module.x - x;
                float dz = module.z - z;
                if (dx * dx + dz * dz <= 0.16f) return true;
            }
            return false;
        }

        public bool BoardFor(string approach, int slot, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            RaidPlan.AnchorOf(approach, out float ax, out float az);
            int covered = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.kind != "Barricade" || !BuildSite.Ready(module.site, module.integrity)) continue;
                if (RaidPlan.Covers(ax, az, module.x, module.z)) covered++;
            }
            int ready = covered;
            if (ready == 0)
            {
                for (int i = 0; i < placed.Count; i++)
                {
                    var module = placed[i];
                    if (module.kind == "Barricade" && BuildSite.Ready(module.site, module.integrity)) ready++;
                }
            }
            if (ready == 0) return false;
            var scores = new int[ready];
            var px = new float[ready];
            var pz = new float[ready];
            int cursor = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.kind != "Barricade" || !BuildSite.Ready(module.site, module.integrity)) continue;
                if (covered > 0 && !RaidPlan.Covers(ax, az, module.x, module.z)) continue;
                scores[cursor] = module.integrity;
                px[cursor] = module.x;
                pz[cursor] = module.z;
                cursor++;
            }
            var order = new int[ready];
            BoardBite.Rank(scores, order);
            int pick = BoardBite.Assign(slot, ready);
            x = px[order[pick]];
            z = pz[order[pick]];
            return true;
        }

        public bool Chip(PlacedModule module, int amount)
        {
            if (module == null || !placed.Contains(module)) return false;
            module.integrity = TrapHit.WearDown(module.integrity, amount);
            if (module.integrity > 0)
            {
                RefreshViews();
                return false;
            }
            placed.Remove(module);
            RefreshViews();
            GameplayFeedback.Toast(YardSay.Spikes(null));
            return true;
        }

        public void IgniteNear(float x, float z, float now)
        {
            bool caught = false;
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.kind != "Oil" || !BuildSite.Ready(module.site, module.integrity) || module.lit >= 0f) continue;
                if (!OilBurn.Ignites(module.x - x, module.z - z)) continue;
                module.lit = now;
                caught = true;
            }
            if (!caught) return;
            nextOil = now;
            RefreshViews();
            GameplayFeedback.Toast(YardSay.Oil(null));
        }

        public void TickOil(float now)
        {
            if (now < nextOil) return;
            nextOil = now + OilBurn.Gap;
            var pool = new List<PlacedModule>();
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].kind == "Oil" && BuildSite.Ready(placed[i].site, placed[i].integrity)) pool.Add(placed[i]);
            }
            if (pool.Count == 0) return;
            bool changed = false;
            var burning = new List<PlacedModule>();
            for (int i = 0; i < pool.Count; i++)
            {
                var module = pool[i];
                if (module.lit < 0f) continue;
                if (!OilBurn.Burning(module.lit, now))
                {
                    placed.Remove(module);
                    changed = true;
                    continue;
                }
                burning.Add(module);
            }
            if (changed) RefreshViews();
            if (burning.Count == 0) return;
            var horde = Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
            var living = new List<ZombieAI>();
            for (int i = 0; i < horde.Length; i++)
            {
                if (horde[i] != null && horde[i].CurrentState != ZombieAI.ZombieState.Dead) living.Add(horde[i]);
            }
            if (living.Count == 0) return;
            var distance = new float[living.Count];
            for (int s = 0; s < burning.Count; s++)
            {
                var oil = burning[s];
                for (int i = 0; i < living.Count; i++)
                {
                    float dx = living[i].transform.position.x - oil.x;
                    float dz = living[i].transform.position.z - oil.z;
                    distance[i] = (float)Math.Sqrt(dx * dx + dz * dz);
                }
                int mark = OilBurn.Victim(distance);
                if (mark < 0) continue;
                var target = living[mark];
                var health = target.GetComponent<HealthSystem>();
                if (health != null && !health.IsDead)
                    health.TakeDamage(OilBurn.Damage, target.transform.position, new Vector3(target.transform.position.x - oil.x, 0f, target.transform.position.z - oil.z), gameObject);
            }
        }

        public int BenchTier()
        {
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.kind != "Workbench" || !BuildSite.Ready(module.site, module.integrity)) continue;
                if (module.tier >= 2 || module.job >= CraftGate.Done) return 2;
            }
            return 1;
        }

        public bool BenchOrdered()
        {
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.kind != "Workbench" || !BuildSite.Ready(module.site, module.integrity)) continue;
                if (CraftGate.Ordered(module.job)) return true;
            }
            return false;
        }

        public int BenchWork()
        {
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.kind != "Workbench" || !BuildSite.Ready(module.site, module.integrity)) continue;
                if (CraftGate.Ordered(module.job) || module.job >= CraftGate.Done) return CraftGate.Worked(module.job);
            }
            return 0;
        }

        public bool OrderBench()
        {
            if (BenchTier() >= 2 || BenchOrdered()) return false;
            PlacedModule bench = null;
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.kind != "Workbench" || !BuildSite.Ready(module.site, module.integrity)) continue;
                if (module.job != 0 || module.tier >= 2) continue;
                bench = module;
                break;
            }
            if (bench == null) return false;
            var storage = ColonyStorage.Instance;
            if (storage == null || !storage.TrySpendBill(CraftGate.UpgradeScrap, CraftGate.UpgradeCloth, 0, CraftGate.UpgradeTape))
            {
                GameplayFeedback.Toast(YardSay.Short(null));
                return false;
            }
            bench.job = 1;
            GameplayFeedback.Toast(Loc.T("camp.bench_raise"));
            return true;
        }

        public bool Lift(int pace)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.kind != "Workbench" || !BuildSite.Ready(module.site, module.integrity)) continue;
                if (!CraftGate.Ordered(module.job)) continue;
                CraftGate.Advance(module.job, pace, out int next, out bool done);
                if (next == module.job) return false;
                module.job = next;
                if (done) module.tier = 2;
                RefreshViews();
                GameplayFeedback.Toast(done ? Loc.T("camp.bench_t2") : Loc.T("camp.bench_raise"));
                return true;
            }
            return false;
        }

        public bool RepairGenerator()
        {
            return Wear("Generator", true);
        }

        public bool BraceWall()
        {
            return Wear("Barricade", false);
        }

        private bool Wear(string kind, bool generator)
        {
            var scores = new int[placed.Count];
            for (int i = 0; i < placed.Count; i++)
            {
                var module = placed[i];
                if (module.kind != kind || !BuildSite.Ready(module.site, module.integrity)) scores[i] = 100;
                else scores[i] = module.integrity;
            }
            int mark = CraftGate.PickWorn(scores);
            if (mark < 0) return false;
            placed[mark].integrity = generator ? CraftGate.MendGenerator(placed[mark].integrity) : CraftGate.BraceWall(placed[mark].integrity);
            RefreshViews();
            return true;
        }

        public bool HasKind(string kind)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].kind == kind && BuildSite.Ready(placed[i].site, placed[i].integrity)) return true;
            }
            return false;
        }

        public static bool Occupied(IReadOnlyList<PlacedModule> modules, float x, float z)
        {
            if (modules == null) return false;
            for (int i = 0; i < modules.Count; i++)
            {
                var module = modules[i];
                if (module == null) continue;
                if (Mathf.Abs(module.x - x) < 0.01f && Mathf.Abs(module.z - z) < 0.01f) return true;
            }
            return false;
        }

        public int BarricadeCount()
        {
            int count = 0;
            foreach (var module in placed)
            {
                if (module.kind == "Barricade" && BuildSite.Ready(module.site, module.integrity)) count++;
            }
            return count;
        }

        private void RefreshViews()
        {
            var copy = placed.ToArray();
            Restore(copy);
        }

        public static int Cost(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Barricade: return 6;
                case ModuleKind.Cot: return 8;
                case ModuleKind.Water: return 10;
                case ModuleKind.Watchtower: return 16;
                case ModuleKind.Generator: return 14;
                case ModuleKind.Workbench: return 12;
                case ModuleKind.TradingPost: return 20;
                case ModuleKind.Farm: return 18;
                case ModuleKind.Purifier: return 15;
                case ModuleKind.Turret: return 22;
                case ModuleKind.Spikes: return 8;
                case ModuleKind.Oil: return 9;
                case ModuleKind.Crate: return 10;
                case ModuleKind.Lamp: return 13;
                case ModuleKind.Campfire: return 7;
                default: return 6;
            }
        }

        private static Vector3 Scale(string kind, int integrity)
        {
            float health = Mathf.Clamp01((integrity <= 0 ? 100 : integrity) / 100f);
            switch (kind)
            {
                case "Cot": return new Vector3(1.4f, 0.4f, 0.7f);
                case "Water": return new Vector3(0.8f, 1.1f, 0.8f);
                case "Watchtower": return new Vector3(1.2f, 2.4f, 1.2f);
                case "Generator": return new Vector3(1.1f, 0.8f, 0.7f);
                case "Workbench": return new Vector3(1.6f, 0.9f, 0.8f);
                case "TradingPost": return new Vector3(1.8f, 1.4f, 1.2f);
                case "Farm": return new Vector3(2.2f, 0.25f, 2.2f);
                case "Purifier": return new Vector3(0.7f, 1.3f, 0.7f);
                case "Turret": return new Vector3(0.45f, 1.5f, 0.45f);
                case "Spikes": return new Vector3(1.6f, 0.08f, 1.6f);
                case "Oil": return new Vector3(2.4f, 0.06f, 2.4f);
                case "Crate": return new Vector3(1.1f, 0.9f, 0.8f);
                case "Lamp": return new Vector3(0.35f, 2.2f, 0.35f);
                case "Campfire": return new Vector3(1.2f, 0.2f, 1.2f);
                default: return new Vector3(1.8f * health, 1.1f * Mathf.Lerp(0.35f, 1f, health), 0.4f);
            }
        }

        private static Color ColorFor(string kind)
        {
            switch (kind)
            {
                case "Cot": return new Color(0.45f, 0.55f, 0.62f);
                case "Water": return new Color(0.25f, 0.55f, 0.75f);
                case "Watchtower": return new Color(0.42f, 0.36f, 0.28f);
                case "Generator": return new Color(0.55f, 0.48f, 0.18f);
                case "Workbench": return new Color(0.38f, 0.32f, 0.26f);
                case "TradingPost": return new Color(0.55f, 0.32f, 0.22f);
                case "Farm": return new Color(0.28f, 0.48f, 0.24f);
                case "Purifier": return new Color(0.35f, 0.7f, 0.78f);
                case "Turret": return new Color(0.22f, 0.24f, 0.28f);
                case "Spikes": return new Color(0.35f, 0.36f, 0.38f);
                case "Oil": return new Color(0.12f, 0.1f, 0.08f);
                case "Crate": return new Color(0.42f, 0.3f, 0.18f);
                case "Lamp": return new Color(0.85f, 0.8f, 0.55f);
                case "Campfire": return new Color(0.72f, 0.28f, 0.12f);
                default: return new Color(0.48f, 0.42f, 0.32f);
            }
        }
    }
}
