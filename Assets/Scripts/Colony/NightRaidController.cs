using UnityEngine;
using OutpostZero.AI;
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
        private bool broadcast;
        private int phase;
        private int raidDay = 1;
        private int raidTowers;

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
