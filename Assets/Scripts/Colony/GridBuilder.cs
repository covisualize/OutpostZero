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
        Campfire,
        Memorial
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
        private GameObject ghost;
        private MaterialPropertyBlock ghostBlock;
        private string ghostKind = "";

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
            ModuleBook.Ensure();
        }

        private void Update()
        {
            TickOil(Time.time);
            TickLamps();
            TickFires();
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.CampManagement)
            {
                HideGhost();
                return;
            }
            if (Player.ExpeditionInput.BuildPressed)
            {
                buildMode = !buildMode;
                GameplayFeedback.Toast(YardSay.Mode(buildMode, null));
            }
            if (!buildMode)
            {
                HideGhost();
                return;
            }
            ShowGhost();
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

        private void ShowGhost()
        {
            var cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Player.ExpeditionInput.Pointer);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float enter))
            {
                HideGhost();
                return;
            }
            Vector3 point = ray.GetPoint(enter);
            float x = BuildGhost.Snap(point.x, cell);
            float z = BuildGhost.Snap(point.z, cell);
            var verdict = BuildGhost.Check(placed, x, z, Bill(selected), ColonyStorage.Instance);
            if (ghost == null)
            {
                ghost = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ghost.name = "BuildGhost";
                Destroy(ghost.GetComponent<Collider>());
                var renderer = ghost.GetComponent<Renderer>();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                ghostBlock = new MaterialPropertyBlock();
                ghostKind = "";
            }
            string kind = selected.ToString();
            if (kind != ghostKind)
            {
                ghostKind = kind;
                ghost.transform.localScale = Scale(kind, 100);
            }
            float y = ghost.transform.localScale.y * 0.5f;
            ghost.transform.SetPositionAndRotation(new Vector3(x, y, z), Quaternion.Euler(0f, facing, 0f));
            var paint = ghost.GetComponent<Renderer>();
            paint.GetPropertyBlock(ghostBlock);
            Color tint = BuildGhost.Tint(verdict);
            ghostBlock.SetColor("_BaseColor", tint);
            ghostBlock.SetColor("_Color", tint);
            paint.SetPropertyBlock(ghostBlock);
            if (!ghost.activeSelf) ghost.SetActive(true);
        }

        private void HideGhost()
        {
            if (ghost != null && ghost.activeSelf) ghost.SetActive(false);
        }

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
            var refund = new ModuleBill();
            if (System.Enum.TryParse(target.kind, out ModuleKind kind)) refund = Bill(kind).Half();
            placed.Remove(target);
            RefreshViews();
            var storage = ColonyStorage.Instance;
            if (storage != null)
            {
                if (refund.Scrap > 0) storage.RestoreScrap(refund.Scrap);
                if (refund.Cloth > 0) storage.RestoreCloth(refund.Cloth);
                if (refund.Chemicals > 0) storage.RestoreChemicals(refund.Chemicals);
                if (refund.Tape > 0) storage.RestoreTape(refund.Tape);
            }
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
            WeatherKind sky = WeatherController.Instance != null ? WeatherController.Instance.Kind : WeatherKind.Clear;
            int caught = 0;
            for (int i = 0; i < placed.Count; i++)
                caught += RainCatch.Extra(placed[i].kind, placed[i].integrity, placed[i].site, sky);
            water += caught;
            for (int i = 0; i < placed.Count; i++) placed[i].age = plots[i].Age;
            int worn = 0;
            for (int i = 0; i < placed.Count; i++)
            {
                int next = StormWear.After(placed[i].integrity, placed[i].site, placed[i].kind, sky);
                if (next < placed[i].integrity) worn++;
                placed[i].integrity = next;
            }
            if (worn > 0) GameplayFeedback.Toast(StormWear.Line(sky, null));
            if (ColonyStorage.Instance == null) return;
            if (food > 0) ColonyStorage.Instance.AddFood(food);
            if (water > 0) ColonyStorage.Instance.AddWater(water);
            if (caught > 0) GameplayFeedback.Toast(RainCatch.Line(sky, null));
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
            float x = BuildGhost.Snap(world.x, cell);
            float z = BuildGhost.Snap(world.z, cell);
            if (Occupied(placed, x, z))
            {
                GameplayFeedback.Toast(YardSay.Taken(null));
                return false;
            }
            if (!MapRim.Inside(x, z))
            {
                GameplayFeedback.Toast(YardSay.Outside(null));
                return false;
            }

            var bill = Bill(kind);
            if (ColonyStorage.Instance == null || !ColonyStorage.Instance.TrySpendBill(bill.Scrap, bill.Cloth, bill.Chemicals, bill.Tape))
            {
                GameplayFeedback.Toast(YardSay.Need(bill, null));
                return false;
            }
            var record = new PlacedModule { kind = kind.ToString(), x = x, z = z, rotation = facing, integrity = 100, site = 1 };
            placed.Add(record);
            SpawnView(record);
            GameplayFeedback.Toast(YardSay.Marked(kind.ToString(), null));
            if (kind == ModuleKind.Barricade) CodexDirector.Hear("barricade");
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

        /// <summary>An open site or a damaged module is waiting for a builder.</summary>
        public bool WorkWaiting()
        {
            for (int i = 0; i < placed.Count; i++)
            {
                if (placed[i].site != 0 && placed[i].integrity > 0) return true;
                if (MendBoard.Needs(placed[i].site, placed[i].integrity)) return true;
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
            var look = module.site == 0 ? ModuleLooks.For(module.kind) : null;
            bool flat = module.kind == "Spikes" || module.kind == "Oil" || module.kind == "Campfire";
            float y = flat ? 0.04f : module.kind == "Lamp" ? 1.2f : 0.6f;
            GameObject view;
            if (look != null)
            {
                view = new GameObject();
                var box = view.AddComponent<BoxCollider>();
                box.size = Scale(module.kind, module.integrity);
            }
            else view = GameObject.CreatePrimitive(PrimitiveType.Cube);
            view.name = "Module_" + module.kind;
            view.transform.position = new Vector3(module.x, y, module.z);
            view.transform.rotation = Quaternion.Euler(0f, module.rotation, 0f);
            if (look != null) Wear(view, look, y, module.integrity);
            else view.transform.localScale = Scale(module.kind, module.integrity);
            if (look == null && module.site != 0)
            {
                float bulk = BuildSite.Bulk(module.hours);
                view.transform.localScale = new Vector3(
                    view.transform.localScale.x * bulk,
                    view.transform.localScale.y * bulk,
                    view.transform.localScale.z * bulk);
            }
            var renderer = look == null ? view.GetComponent<Renderer>() : null;
            if (renderer != null)
            {
                var color = module.site != 0
                    ? Color.Lerp(ColorFor(module.kind), new Color(0.72f, 0.7f, 0.58f), module.hours > 0 ? 0.35f : 0.7f)
                    : module.kind == "Oil" && module.lit >= 0f
                    ? new Color(0.95f, 0.42f, 0.08f)
                    : ColorFor(module.kind);
                var family = module.site != 0 ? SurfaceFamily.Plywood : FamilyFor(module.kind);
                if (!MaterialLibrary.Dress(renderer, family, MaterialLibrary.TintFor(color))) renderer.material.color = color;
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
                obstacle.size = look != null ? Scale(module.kind, module.integrity) : Vector3.one;
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

        /// <summary>
        /// Hangs the baked model under the collider root, feet on the ground. A battered module sits lower
        /// and darker, the way the box stand-in shrank.
        /// </summary>
        private static void Wear(GameObject root, GameObject look, float y, int integrity)
        {
            var model = Instantiate(look, root.transform);
            model.name = look.name;
            float health = Mathf.Clamp01((integrity <= 0 ? 100 : integrity) / 100f);
            model.transform.localPosition = new Vector3(0f, -y - (1f - health) * 0.15f, 0f);
            model.transform.localRotation = Quaternion.identity;
            foreach (var collider in model.GetComponentsInChildren<Collider>()) Destroy(collider);
            if (health >= 0.99f) return;
            var block = new MaterialPropertyBlock();
            var shade = Color.Lerp(new Color(0.45f, 0.4f, 0.36f), Color.white, health);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.GetPropertyBlock(block);
                block.SetColor("_Tint", shade);
                renderer.SetPropertyBlock(block);
            }
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
            var horde = UnityEngine.Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
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
            return ModuleTable.TryRow(kind.ToString(), out var row) ? row.Scrap : CodeCost(kind);
        }

        public static ModuleBill Bill(ModuleKind kind)
        {
            if (ModuleTable.TryRow(kind.ToString(), out var row)) return new ModuleBill(row.Scrap, row.Cloth, row.Chemicals, row.Tape);
            CodeSupplies(kind, out int cloth, out int chemicals, out int tape);
            return new ModuleBill(CodeCost(kind), cloth, chemicals, tape);
        }

        /// <summary>Supplies a module needs beside its scrap: canvas for beds and banners, tape and chemicals for machines.</summary>
        public static void CodeSupplies(ModuleKind kind, out int cloth, out int chemicals, out int tape)
        {
            cloth = 0;
            chemicals = 0;
            tape = 0;
            switch (kind)
            {
                case ModuleKind.Cot: cloth = 1; break;
                case ModuleKind.Water: tape = 1; break;
                case ModuleKind.Watchtower: tape = 1; break;
                case ModuleKind.Generator: chemicals = 1; tape = 1; break;
                case ModuleKind.Workbench: tape = 1; break;
                case ModuleKind.TradingPost: cloth = 2; break;
                case ModuleKind.Farm: chemicals = 1; break;
                case ModuleKind.Purifier: chemicals = 2; break;
                case ModuleKind.Turret: chemicals = 1; tape = 2; break;
                case ModuleKind.Lamp: tape = 1; break;
                case ModuleKind.Memorial: cloth = 1; break;
            }
        }

        public static int CodeCost(ModuleKind kind)
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
                case ModuleKind.Memorial: return 6;
                default: return 6;
            }
        }

        public static Vector3 Scale(string kind, int integrity)
        {
            Vector3 size;
            bool wears;
            if (ModuleTable.TryRow(kind, out var row))
            {
                size = row.Size;
                wears = row.Wears;
            }
            else
            {
                size = CodeSize(kind);
                wears = CodeWears(kind);
            }
            if (!wears) return size;
            float health = Mathf.Clamp01((integrity <= 0 ? 100 : integrity) / 100f);
            return new Vector3(size.x * health, size.y * Mathf.Lerp(0.35f, 1f, health), size.z);
        }

        /// <summary>Kinds with no entry of their own are walls: they shrink as they wear.</summary>
        public static bool CodeWears(string kind)
        {
            return !System.Enum.TryParse(kind, out ModuleKind parsed) || parsed == ModuleKind.Barricade;
        }

        public static Vector3 CodeSize(string kind)
        {
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
                case "Memorial": return new Vector3(1.8f, 1.4f, 0.3f);
                default: return new Vector3(1.8f, 1.1f, 0.4f);
            }
        }

        /// <summary>Library surface for a module stand-in; lamps and fires keep flat colour.</summary>
        public static SurfaceFamily FamilyFor(string kind)
        {
            return ModuleTable.TryRow(kind, out var row) ? row.Family : CodeFamily(kind);
        }

        public static SurfaceFamily CodeFamily(string kind)
        {
            switch (kind)
            {
                case "Cot": return SurfaceFamily.Cloth;
                case "Water":
                case "Purifier": return SurfaceFamily.MetalPainted;
                case "Watchtower":
                case "Workbench":
                case "TradingPost":
                case "Memorial":
                case "Crate": return SurfaceFamily.Plywood;
                case "Generator":
                case "Turret":
                case "Spikes": return SurfaceFamily.MetalRusted;
                case "Oil": return SurfaceFamily.Rubber;
                case "Farm": return SurfaceFamily.TarpFabric;
                case "Lamp":
                case "Campfire": return SurfaceFamily.None;
                default: return SurfaceFamily.ConcreteCracked;
            }
        }

        public static Color ColorFor(string kind)
        {
            return ModuleTable.TryRow(kind, out var row) ? row.Tint : CodeColor(kind);
        }

        public static Color CodeColor(string kind)
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
                case "Memorial": return new Color(0.5f, 0.45f, 0.38f);
                default: return new Color(0.48f, 0.42f, 0.32f);
            }
        }
    }
}
