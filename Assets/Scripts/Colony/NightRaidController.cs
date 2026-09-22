using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Player;
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
        private bool broadcast;
        private int phase;
        private int raidDay = 1;
        private int raidTowers;
        private float nextTurret;
        private float nextTrap;
        private float nextGuard;

        public bool Running => running;
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
            Open(false);
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
                GameplayFeedback.Toast("The tower is not ready");
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
            endsAt = Time.time + duration;
            nextStrike = Time.time + strikeInterval;
            nextTurret = Time.time + TurretBeat.Interval;
            nextTrap = Time.time + TrapHit.Gap;
            nextGuard = Time.time + GuardVolley.Interval;
            GameManager.Instance.SetState(GameState.RaidActive);
            int spawn = RaidPlan.SpawnCount(day, towers) + (tower ? 4 : 0);
            if (HordeDirector.Instance != null) HordeDirector.Instance.BeginRaid(spawn);
            GameplayFeedback.Toast(tower ? "Broadcast night — hold the tower" : "Night raid from the " + approach);
        }

        private void Update()
        {
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
                if (GridBuilder.Instance != null && GridBuilder.Instance.BarricadeCount() > 0)
                {
                    if (GridBuilder.Instance.StrikeFrom(approach, hit))
                        SurvivorRoster.Instance?.WoundFromRaid(raidDay + phase);
                }
            }
            if (Time.time < endsAt) return;
            running = false;
            int security = ColonyStorage.Instance != null ? ColonyStorage.Instance.Security : 0;
            bool held = security > 0 || (HordeDirector.Instance != null && HordeDirector.Instance.Tension < 80f);
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
                GameplayFeedback.Toast(broadcast ? "The broadcast went out" : "The gate held");
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
                ColonyStorage.Instance?.AddScrap(-6);
                GameplayFeedback.Toast("The raid broke the stores");
            }
            broadcast = false;
            GameManager.Instance?.SetState(GameState.CampManagement);
        }

        private void TickTurret()
        {
            if (Time.time < nextTurret) return;
            int guns = GridBuilder.Instance != null ? GridBuilder.Instance.CountKind("Turret") : 0;
            bool powered = CampServices.Instance != null && CampServices.Instance.GeneratorOnline;
            var player = PlayerRegistry.Current;
            var carried = player != null ? player.GetComponentsInChildren<FirearmWeapon>(true) : System.Array.Empty<FirearmWeapon>();
            var rifle = new bool[carried.Length];
            var rounds = new int[carried.Length];
            int total = 0;
            for (int i = 0; i < carried.Length; i++)
            {
                if (carried[i] == null) continue;
                rounds[i] = carried[i].CurrentAmmo + carried[i].ReserveAmmo;
                rifle[i] = carried[i].Type == WeaponType.Rifle;
                total += rounds[i];
            }
            if (!TurretBeat.Ready(TurretBeat.Interval, powered, guns, total)) return;

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
            if (mark < 0) return;
            int feed = TurretBeat.Prefer(rifle, rounds);
            if (feed < 0 || !carried[feed].TrySpendRound()) return;

            var target = living[mark];
            var health = target.GetComponent<HealthSystem>();
            if (health != null)
                health.TakeDamage(TurretBeat.Damage, target.transform.position, (origin - target.transform.position).normalized, player != null ? player.gameObject : gameObject);
            if (Sensory.NoiseManager.Instance != null)
                Sensory.NoiseManager.Instance.EmitNoise(origin, 18f, 0.7f, NoiseType.GunshotLoud, player != null ? player.gameObject : gameObject);
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
            int guards = 0;
            if (SurvivorRoster.Instance != null)
            {
                foreach (var survivor in SurvivorRoster.Instance.Survivors)
                {
                    if (survivor.alive && survivor.task == "Guard") guards++;
                }
            }
            if (!GuardVolley.Ready(GuardVolley.Interval, guards)) return;
            Vector3 origin = GuardOrigin();
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
            int mark = GuardVolley.Pick(distance.ToArray());
            if (mark < 0)
            {
                nextGuard = Time.time + GuardVolley.Interval;
                return;
            }
            var target = living[mark];
            var health = target.GetComponent<HealthSystem>();
            if (health != null && !health.IsDead)
                health.TakeDamage(GuardVolley.Hit(guards), target.transform.position, (origin - target.transform.position).normalized, gameObject);
            nextGuard = Time.time + GuardVolley.Interval;
        }

        private static Vector3 GuardOrigin()
        {
            if (GridBuilder.Instance != null)
            {
                foreach (var module in GridBuilder.Instance.Placed)
                {
                    if (module.kind == "Watchtower" && BuildSite.Ready(module.site, module.integrity))
                        return new Vector3(module.x, 2.2f, module.z);
                }
            }
            return new Vector3(-12f, 1.6f, -12f);
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

        private void ApplyWave(int index, bool announce)
        {
            var wave = RaidPlan.WaveAt(raidDay, raidTowers, index);
            approach = broadcast && index == 0 ? "gate" : wave.Approach;
            bool generator = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("Generator");
            int walls = GridBuilder.Instance != null ? GridBuilder.Instance.BarricadeCount() : 0;
            pressure = CampYield.RaidPressure(wave.Pressure + (broadcast ? 4 : 0), generator, walls);
            strikeInterval = broadcast && index == 0 ? 1.2f : wave.Interval;
            phase = index;
            if (!announce) return;
            GameplayFeedback.Toast("They come from the " + approach);
            int extra = RaidPlan.Reinforcements(index);
            if (extra > 0 && HordeDirector.Instance != null) HordeDirector.Instance.BeginRaid(extra);
        }
    }
}
