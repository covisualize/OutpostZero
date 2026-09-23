using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    public class NightRaidController : MonoBehaviour
    {
        public static NightRaidController Instance { get; private set; }

        [SerializeField] private float duration = 75f;
        private float endsAt;
        private float nextStrike;
        private float strikeInterval = 1.2f;
        private int pressure = 6;
        private string approach = "gate";
        private bool running;
        private bool warning;
        private float warningEnds;
        private bool broadcast;
        private int phase;
        private int raidDay = 1;
        private int raidTowers;
        private float nextTurret;
        private float nextTrap;
        private float nextGuard;
        private int calledDay = -1;
        private bool breached;
        private bool bittenTold;
        private int fronts = 1;
        private int sightedDay = -1;

        public string Approach => approach;
        public int Fronts => fronts;

        public string SideAt(int slot)
        {
            int span = fronts < 1 ? 1 : fronts;
            int index = slot < 0 ? 0 : slot;
            return RaidPlan.Side(raidDay, raidTowers, index % span);
        }

        public int PostOnSide(int slot)
        {
            if (slot < 0) slot = 0;
            if (fronts < 1) return slot;
            return slot / fronts;
        }
        public bool Running => running;
        public bool Warning => warning;
        public float WarningLeft => warning ? UnityEngine.Mathf.Max(0f, warningEnds - UnityEngine.Time.time) : 0f;
        public float Remaining => running ? Mathf.Max(0f, endsAt - Time.time) : 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void Begin()
        {
            if (running || warning) return;
            float hold = TraitHook.Warning(TowerCount(), GuardCount(), TraitCount("Watchful"), TraitCount("Light Sleeper"));
            hold = TraitHook.NightStretch(hold, TraitCount("Night Owl"));
            if (hold <= 0f)
            {
                Open(false);
                return;
            }
            warning = true;
            warningEnds = Time.time + hold;
            GameplayFeedback.Toast(Loc.T("camp.warn") + " " + Mathf.CeilToInt(hold));
        }

        public void BeginBroadcast()
        {
            Open(true);
        }

        private void Open(bool tower)
        {
            if (GameManager.Instance == null) return;
            if (tower && (WorldMapService.Instance == null || !WorldMapService.Instance.ReadyToBroadcast))
            {
                GameplayFeedback.Toast(FightSay.Tower(null));
                return;
            }
            int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            int placed = GridBuilder.Instance != null ? GridBuilder.Instance.CountKind("Watchtower") : 0;
            int sceneTower = CampServices.Instance != null && CampServices.Instance.WatchtowerOnline ? 1 : 0;
            int towers = placed > sceneTower ? placed : sceneTower;
            raidDay = day;
            raidTowers = towers;
            broadcast = tower;
            ApplyWave(0, false);
            running = true;
            calledDay = day;
            endsAt = Time.time + duration;
            nextStrike = Time.time + strikeInterval;
            nextTurret = Time.time + TurretBeat.Interval;
            nextTrap = Time.time + TrapHit.Gap;
            nextGuard = Time.time + GuardVolley.Interval;
            bittenTold = false;
            GameManager.Instance.SetState(GameState.RaidActive);
            AudioManager.Instance?.Sting("raid");
            int difficulty = WorldMapService.Instance != null ? WorldMapService.Instance.Difficulty : 2;
            fronts = RaidPlan.Fronts(day, difficulty);
            int perimeter = GridBuilder.Instance != null ? GridBuilder.Instance.PerimeterScore : 0;
            int spawn = Perimeter.Crowd(RaidPlan.SpawnCount(day, towers) + (tower ? 4 : 0), perimeter);
            if (HordeDirector.Instance != null)
            {
                for (int i = 0; i < fronts; i++)
                {
                    int share = RaidPlan.Share(spawn, fronts, i);
                    if (share > 0) HordeDirector.Instance.BeginRaid(share, RaidPlan.Side(day, towers, i));
                }
            }
            string openLine = RaidSay.Open(tower, approach, null);
            if (fronts > 1) openLine += "  " + fronts + " " + Loc.T("camp.sides");
            openLine += "  " + Loc.T("camp.dark");
            if (GridBuilder.Instance != null && GridBuilder.Instance.BarricadeCount() > 0)
                openLine += "  " + Loc.T("camp.chew");
            openLine += "  " + Loc.T("camp.perimeter") + " " + perimeter + "%";
            if (GuardsOnTheLine() > 0) openLine += "  " + Loc.T("camp.line");
            GameplayFeedback.Toast(openLine);
        }

        public bool HoldWatch(float added)
        {
            if (running || warning) return true;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.CampManagement) return false;
            float hour = WorldClock.Instance != null ? WorldClock.Instance.Hour : 12f;
            int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            if (calledDay == day) return false;
            if (!RaidWatch.Crosses(hour, added)) return false;
            if (!LikelyNow()) return false;
            if (!RaidWatch.Night(hour)) WorldClock.Instance?.Set(day, RaidWatch.Dusk);
            GameplayFeedback.Toast(Loc.T("camp.dusk"));
            Begin();
            return true;
        }

        public bool HoldTheNight()
        {
            if (running || warning) return true;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.CampManagement) return false;
            int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            if (calledDay == day) return false;
            if (!LikelyNow()) return false;
            float hour = WorldClock.Instance != null ? WorldClock.Instance.Hour : 12f;
            if (!RaidWatch.Night(hour)) WorldClock.Instance?.Set(day, RaidWatch.Dusk);
            GameplayFeedback.Toast(Loc.T("camp.dusk"));
            Begin();
            return true;
        }

        public bool RaidLikely => LikelyNow();

        /// <summary>A pack seen near the fence comes that night, whatever the odds said.</summary>
        public void Sight(int day)
        {
            sightedDay = day;
        }

        private bool LikelyNow()
        {
            int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            if (sightedDay == day) return true;
            int security = ColonyStorage.Instance != null ? ColonyStorage.Instance.Security : 0;
            int shots = ColonyStorage.Instance != null ? ColonyStorage.Instance.Shots : 0;
            bool endless = WorldMapService.Instance != null && WorldMapService.Instance.Endless;
            bool generator = CampServices.Instance != null && CampServices.Instance.GeneratorOnline;
            int walls = GridBuilder.Instance != null ? GridBuilder.Instance.BarricadeCount() : 0;
            int difficulty = WorldMapService.Instance != null ? WorldMapService.Instance.Difficulty : 2;
            return RaidCall.Likely(day, security, endless, shots, generator, walls, difficulty);
        }

        private void Update()
        {
            if (!running && !warning) HoldWatch(0f);
            if (warning)
            {
                if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.CampManagement)
                {
                    warning = false;
                    return;
                }
                if (Time.time < warningEnds) return;
                warning = false;
                Open(false);
                return;
            }
            if (!running) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.RaidActive)
            {
                running = false;
                return;
            }
            TickTurret();
            TickTraps();
            TickGuards();
            float elapsed = duration - Remaining;
            int nextPhase = RaidPlan.PhaseAt(elapsed, duration);
            if (nextPhase != phase) ApplyWave(nextPhase, true);

            if (Time.time >= nextStrike)
            {
                nextStrike = Time.time + strikeInterval;
                int guards = 0;
                if (SurvivorRoster.Instance != null)
                {
                    foreach (var survivor in SurvivorRoster.Instance.Survivors)
                    {
                        if (survivor.alive && survivor.task == "Guard") guards++;
                    }
                }
                int cover = GridBuilder.Instance != null ? GridBuilder.Instance.CoverCount(approach) : 0;
                int hit = RaidPlan.Strike(pressure, guards, cover);
                int blow = RaidBreach.Blow(hit, RaidBreach.Brute(phase));
                bool hasWalls = GridBuilder.Instance != null && GridBuilder.Instance.BarricadeCount() > 0;
                if (BoardBite.CrowdChews(ZombieAI.PostedCount()))
                {
                    blow = 0;
                }
                if (hasWalls && blow > 0)
                {
                    if (GridBuilder.Instance.StrikeFrom(approach, blow))
                        SurvivorRoster.Instance?.WoundFromRaid(raidDay + phase);
                }
                else if (blow > 0 && RaidBreach.Brute(phase) && !breached)
                {
                    breached = true;
                    SurvivorRoster.Instance?.WoundFromRaid(raidDay + phase);
                }
            }
            if (Time.time < endsAt) return;
            running = false;
            int fellBack = ZombieAI.RecallRaid();
            if (SurvivorRoster.Instance != null && CampEnd.Wiped(SurvivorRoster.Instance.LivingCount()))
            {
                GameplayFeedback.Toast(Loc.T("camp.wiped"));
                GameManager.Instance?.SetState(GameState.GameOver);
                SaveSystem.Instance?.Save(false);
                broadcast = false;
                return;
            }
            int walls = GridBuilder.Instance != null ? GridBuilder.Instance.BarricadeCount() : 0;
            int lights = GridBuilder.Instance != null ? GridBuilder.Instance.CountKind("Lamp") : 0;
            bool held = RaidOutcome.Holds(raidDay, walls, GuardsOnTheLine(), lights, breached);
            int dropped = YardDead.Dropped(pressure, held);
            if (dropped > 0) ColonyStorage.Instance?.AddBodies(dropped);
            if (held)
            {
                SurvivorRoster.Instance?.TickTasks();
                if (SurvivorRoster.Instance != null)
                {
                    foreach (var survivor in SurvivorRoster.Instance.Survivors)
                    {
                        if (survivor.alive) survivor.morale = Mathf.Min(100f, survivor.morale + 6f);
                    }
                }
                int left = ColonyStorage.Instance != null ? ColonyStorage.Instance.Bodies : dropped;
                string heldLine = RaidSay.Held(broadcast, null);
                if (left > 0) heldLine += "  " + Loc.T("camp.bodies") + " " + left;
                GameplayFeedback.Toast(heldLine);
                if (broadcast && WorldMapService.Instance != null && WorldMapService.Instance.GeneratorBuilt)
                {
                    WorldMapService.Instance.NoteBroadcast();
                    GameManager.Instance?.SetState(GameState.Victory);
                    SaveSystem.Instance?.Save(false);
                    broadcast = false;
                    return;
                }
            }
            else
            {
                int scrap = ColonyStorage.Instance != null ? ColonyStorage.Instance.Scrap : 0;
                int food = ColonyStorage.Instance != null ? ColonyStorage.Instance.Food : 0;
                int lostScrap = breached ? RaidSpoil.Scrap(true, scrap) : 6;
                int lostFood = breached ? RaidSpoil.Meals(true, food) : 0;
                if (lostScrap > 0) ColonyStorage.Instance?.AddScrap(-lostScrap);
                if (lostFood > 0) ColonyStorage.Instance?.AddFood(-lostFood);
                string broke = breached ? Loc.T("camp.spoiled") : RaidSay.Broke(null);
                if (breached && lostScrap > 0) broke += "  " + Loc.T("camp.scrap") + " -" + lostScrap;
                if (breached && lostFood > 0) broke += "  " + Loc.T("camp.food") + " -" + lostFood;
                if (dropped > 0) broke += "  " + Loc.T("camp.bodies") + " " + dropped;
                GameplayFeedback.Toast(broke);
            }
            bool bodiesLeft = ColonyStorage.Instance != null && ColonyStorage.Instance.Bodies > 0;
            bool damaged = GridBuilder.Instance != null && GridBuilder.Instance.MendCount() > 0;
            string morning = SurvivorRoster.Instance != null ? SurvivorRoster.Instance.PostRaid(bodiesLeft, damaged) : "";
            if (!string.IsNullOrEmpty(morning)) GameplayFeedback.Toast(Loc.T(morning));
            if (fellBack > 0) GameplayFeedback.Toast(Loc.T("camp.quiet") + " " + fellBack);
            broadcast = false;
            GameManager.Instance?.SetState(GameState.CampManagement);
        }

        private void TickTurret()
        {
            if (Time.time < nextTurret) return;
            int guns = GridBuilder.Instance != null ? GridBuilder.Instance.CountKind("Turret") : 0;
            bool powered = CampServices.Instance != null && CampServices.Instance.GeneratorOnline;
            int tier = GridBuilder.Instance != null ? GridBuilder.Instance.BenchTier() : 1;
            int stored = ColonyStorage.Instance != null ? ColonyStorage.Instance.Rounds : 0;
            if (!TurretBeat.Fed(powered, guns, tier, stored)) return;

            Vector3 origin = TurretOrigin();
            var horde = Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
            var living = new System.Collections.Generic.List<ZombieAI>();
            var distance = new System.Collections.Generic.List<float>();
            for (int i = 0; i < horde.Length; i++)
            {
                var zombie = horde[i];
                if (zombie == null || zombie.CurrentState == ZombieAI.ZombieState.Dead) continue;
                float dx = zombie.transform.position.x - origin.x;
                float dz = zombie.transform.position.z - origin.z;
                living.Add(zombie);
                distance.Add((float)System.Math.Sqrt(dx * dx + dz * dz));
            }
            int mark = TurretBeat.Pick(distance.ToArray(), TurretBeat.Range);
            int spent = TurretBeat.Draw(stored);
            if (mark < 0 || spent <= 0) return;
            ColonyStorage.Instance?.TakeRounds(spent);

            var target = living[mark];
            var health = target.GetComponent<HealthSystem>();
            if (health != null)
                health.TakeDamage(TurretBeat.Damage, target.transform.position, (origin - target.transform.position).normalized, gameObject);
            if (Sensory.NoiseManager.Instance != null)
                Sensory.NoiseManager.Instance.EmitNoise(origin, 18f, 0.7f, NoiseType.GunshotLoud, gameObject);
            nextTurret = Time.time + TurretBeat.Interval;
        }

        private void TickTraps()
        {
            if (Time.time < nextTrap || GridBuilder.Instance == null) return;
            if (!TrapHit.Due(TrapHit.Gap)) return;
            var spikes = new System.Collections.Generic.List<PlacedModule>();
            foreach (var module in GridBuilder.Instance.Placed)
            {
                if (module.kind == "Spikes" && BuildSite.Ready(module.site, module.integrity)) spikes.Add(module);
            }
            if (spikes.Count == 0) return;
            nextTrap = Time.time + TrapHit.Gap;
            var horde = Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
            var living = new System.Collections.Generic.List<ZombieAI>();
            for (int i = 0; i < horde.Length; i++)
            {
                if (horde[i] != null && horde[i].CurrentState != ZombieAI.ZombieState.Dead) living.Add(horde[i]);
            }
            if (living.Count == 0) return;
            var distance = new float[living.Count];
            for (int s = 0; s < spikes.Count; s++)
            {
                var spike = spikes[s];
                for (int i = 0; i < living.Count; i++)
                {
                    float dx = living[i].transform.position.x - spike.x;
                    float dz = living[i].transform.position.z - spike.z;
                    distance[i] = (float)System.Math.Sqrt(dx * dx + dz * dz);
                }
                int mark = TrapHit.Victim(distance, TrapHit.Radius);
                if (mark < 0) continue;
                var target = living[mark];
                var health = target.GetComponent<HealthSystem>();
                if (health != null && !health.IsDead)
                    health.TakeDamage(TrapHit.Damage, target.transform.position, new Vector3(target.transform.position.x - spike.x, 0f, target.transform.position.z - spike.z), gameObject);
                GridBuilder.Instance.Chip(spike, TrapHit.Wear);
            }
        }

        private void TickGuards()
        {
            if (Time.time < nextGuard) return;
            var crew = new System.Collections.Generic.List<Survivor>();
            if (SurvivorRoster.Instance != null)
            {
                foreach (var survivor in SurvivorRoster.Instance.Survivors)
                {
                    if (survivor == null || !survivor.alive || survivor.leader) continue;
                    if (GuardStand.Face(survivor.task, survivor.morale, survivor.injury) != "Guard") continue;
                    crew.Add(survivor);
                }
            }
            int stored = ColonyStorage.Instance != null ? ColonyStorage.Instance.Rounds : 0;
            int budget = GuardVolley.Rounds(crew.Count, stored);
            if (!GuardVolley.Ready(GuardVolley.Interval, crew.Count) || budget <= 0)
            {
                nextGuard = Time.time + GuardVolley.Interval;
                return;
            }
            var horde = Object.FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
            var living = new System.Collections.Generic.List<ZombieAI>();
            for (int i = 0; i < horde.Length; i++)
            {
                if (horde[i] != null && horde[i].CurrentState != ZombieAI.ZombieState.Dead) living.Add(horde[i]);
            }
            int shots = 0;
            for (int g = 0; g < crew.Count && shots < budget; g++)
            {
                Vector3 origin = GuardPost(g);
                var distance = new float[living.Count];
                for (int i = 0; i < living.Count; i++)
                {
                    float dx = living[i].transform.position.x - origin.x;
                    float dz = living[i].transform.position.z - origin.z;
                    distance[i] = (float)System.Math.Sqrt(dx * dx + dz * dz);
                }
                float reach = PostBite.Range(crew[g].injury);
                for (int i = 0; i < distance.Length; i++)
                {
                    if (distance[i] > reach) distance[i] = -1f;
                }
                int mark = GuardVolley.Pick(distance);
                if (mark < 0) continue;
                if (ColonyStorage.Instance == null || ColonyStorage.Instance.TakeRounds(1) <= 0) break;
                shots++;
                if (crew[g].injury > 0 && !bittenTold)
                {
                    bittenTold = true;
                    GameplayFeedback.Toast(PostBite.Line(null));
                }
                var target = living[mark];
                Vector3 aim = target.transform.position + Vector3.up * 1.1f;
                Vector3 direction = aim - origin;
                var health = target.GetComponent<HealthSystem>();
                if (health != null && !health.IsDead)
                    health.TakeDamage(PostBite.Damage(crew[g].injury), aim, direction.normalized, gameObject);
                Vector3 eject = Vector3.Cross(direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.forward, Vector3.up);
                CombatVfx.Shot(origin, direction, aim, eject);
                if (Sensory.NoiseManager.Instance != null)
                    Sensory.NoiseManager.Instance.EmitNoise(origin, 14f, 0.45f, NoiseType.GunshotQuiet, gameObject);
            }
            nextGuard = Time.time + GuardVolley.Interval;
        }

        private int GuardsOnTheLine()
        {
            int count = 0;
            if (SurvivorRoster.Instance == null) return 0;
            foreach (var survivor in SurvivorRoster.Instance.Survivors)
            {
                if (survivor == null || !survivor.alive || survivor.leader) continue;
                if (GuardStand.Face(survivor.task, survivor.morale, survivor.injury) == "Guard") count++;
            }
            return count;
        }

        private Vector3 GuardPost(int slot)
        {
            var grid = GridBuilder.Instance;
            int count = grid != null ? grid.Placed.Count : 0;
            var kinds = new string[count];
            var sites = new int[count];
            var integrity = new int[count];
            var xs = new float[count];
            var zs = new float[count];
            for (int i = 0; i < count; i++)
            {
                var module = grid.Placed[i];
                kinds[i] = module.kind;
                sites[i] = module.site;
                integrity[i] = module.integrity;
                xs[i] = module.x;
                zs[i] = module.z;
            }
            string side = SideAt(slot);
            int post = PostOnSide(slot);
            GuardStand.Mark(side, post, kinds, xs, zs, sites, integrity, out float x, out float z);
            return new Vector3(x, 1.6f, z);
        }

        private static Vector3 TurretOrigin()
        {
            if (GridBuilder.Instance != null)
            {
                foreach (var module in GridBuilder.Instance.Placed)
                {
                    if (module.kind == "Turret" && BuildSite.Ready(module.site, module.integrity))
                        return new Vector3(module.x, 1.2f, module.z);
                }
            }
            return new Vector3(-12f, 1.2f, -12f);
        }

        private static int TowerCount()
        {
            int placed = GridBuilder.Instance != null ? GridBuilder.Instance.CountKind("Watchtower") : 0;
            int sceneTower = CampServices.Instance != null && CampServices.Instance.WatchtowerOnline ? 1 : 0;
            return placed > sceneTower ? placed : sceneTower;
        }

        private static int GuardCount()
        {
            int guards = 0;
            if (SurvivorRoster.Instance == null) return 0;
            foreach (var survivor in SurvivorRoster.Instance.Survivors)
            {
                if (survivor.alive && survivor.task == "Guard") guards++;
            }
            return guards;
        }

        private static int TraitCount(string trait)
        {
            int count = 0;
            if (SurvivorRoster.Instance == null || string.IsNullOrEmpty(trait)) return 0;
            foreach (var survivor in SurvivorRoster.Instance.Survivors)
            {
                if (survivor.alive && (survivor.trait == trait || survivor.aside == trait || survivor.mark == trait)) count++;
            }
            return count;
        }

        private static int LampsOn(string approach)
        {
            if (GridBuilder.Instance == null) return 0;
            bool powered = CampServices.Instance != null && CampServices.Instance.GeneratorOnline;
            int count = 0;
            foreach (var module in GridBuilder.Instance.Placed)
            {
                if (module.kind == "Lamp") count++;
            }
            var x = new float[count];
            var z = new float[count];
            var sites = new int[count];
            var integrity = new int[count];
            int cursor = 0;
            foreach (var module in GridBuilder.Instance.Placed)
            {
                if (module.kind != "Lamp") continue;
                x[cursor] = module.x;
                z[cursor] = module.z;
                sites[cursor] = module.site;
                integrity[cursor] = module.integrity;
                cursor++;
            }
            return FloodBeam.Covering(approach, x, z, sites, integrity, powered);
        }

        private void ApplyWave(int index, bool announce)
        {
            var wave = RaidPlan.WaveAt(raidDay, raidTowers, index);
            approach = broadcast && index == 0 ? "gate" : wave.Approach;
            bool generator = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("Generator");
            int walls = GridBuilder.Instance != null ? GridBuilder.Instance.BarricadeCount() : 0;
            pressure = CampYield.RaidPressure(wave.Pressure + (broadcast ? 4 : 0), generator, walls);
            if (GridBuilder.Instance != null) pressure = Perimeter.Pressure(pressure, Perimeter.Side(approach, GridBuilder.Instance.Walls()));
            strikeInterval = broadcast && index == 0 ? 1.2f : wave.Interval;
            int lamps = LampsOn(approach);
            pressure = FloodBeam.ApproachPressure(pressure, lamps);
            strikeInterval = FloodBeam.ApproachGap(strikeInterval, lamps);
            phase = index;
            breached = false;
            if (!announce) return;
            string line = RaidSay.Coming(approach, null);
            if (RaidBreach.Brute(index)) line += "  " + Loc.T("camp.brute");
            if (lamps > 0) line += "  " + Loc.T("camp.lamps");
            GameplayFeedback.Toast(line);
            int extra = RaidPlan.Reinforcements(index);
            if (extra > 0 && HordeDirector.Instance != null) HordeDirector.Instance.BeginRaid(extra, approach);
        }
    }
}
