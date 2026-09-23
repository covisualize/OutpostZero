using System;
using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Core;
using OutpostZero.Expedition;
using OutpostZero.Shell;

namespace OutpostZero.AI
{
    public enum TensionState
    {
        Calm,
        BuildUp,
        Peak,
        Relax
    }

    /// <summary>
    /// Paces the horde. Quiet stretches decay tension; gunfire and time push it toward a peak,
    /// and spawns are requested through the existing <see cref="ZombieSpawner"/>.
    /// </summary>
    public class HordeDirector : MonoBehaviour
    {
        public static HordeDirector Instance { get; private set; }

        [SerializeField] private float tension;
        [SerializeField] private TensionState state = TensionState.Calm;
        [SerializeField] private int maxAlive = 32;
        [SerializeField] private float spawnInterval = 9f;

        private ZombieSpawner spawner;
        private float nextSpawn;
        private float nextAllowedReinforcement;
        private bool subscribedNoise;
        private int runDifficulty = 2;
        private int tier = 1;
        private float elapsed;
        private float eventCursor;
        private bool runnersOut;
        private bool overtimeCalled;
        private string openingPrefer = "";
        private bool scripted;
        private bool nearHeard;
        private float nextNearCheck;

        public float Tension => tension;
        public TensionState State => state;
        public bool Scripted => scripted;
        public int AliveCap => spawner != null ? spawner.MaxAlive : maxAlive;
        public event Action<TensionState> OnTensionStateChanged;

        private void Awake()
        {
            Instance = this;
            spawner = GetComponent<ZombieSpawner>();
        }

        private void OnEnable() => TrySubscribe();

        private void Start() => TrySubscribe();

        private void OnDisable()
        {
            if (!subscribedNoise || Sensory.NoiseManager.Instance == null) return;
            Sensory.NoiseManager.Instance.OnNoiseEmitted -= OnNoise;
            subscribedNoise = false;
        }

        private void TrySubscribe()
        {
            if (subscribedNoise || Sensory.NoiseManager.Instance == null) return;
            Sensory.NoiseManager.Instance.OnNoiseEmitted += OnNoise;
            subscribedNoise = true;
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.ExpeditionActive && GameManager.Instance.CurrentState != GameState.RaidActive)
            {
                return;
            }

            TrySubscribe();
            tension = PressureClock.Advance(tension, state, Time.deltaTime, 0f);
            TensionState next = Evaluate(tension);
            if (next != state)
            {
                state = next;
                OnTensionStateChanged?.Invoke(state);
            }

            if (!nearHeard && spawner != null && Time.time >= nextNearCheck && PlayerRegistry.Current != null)
            {
                nextNearCheck = Time.time + 0.5f;
                if (TutorialRun.Close(spawner.Nearest(PlayerRegistry.Current.transform.position)))
                {
                    nearHeard = true;
                    CodexDirector.Hear("near");
                }
            }

            if (scripted) return;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.ExpeditionActive)
            {
                elapsed += Time.deltaTime;
                if (StreetTerms.Overtime(StreetTerms.Active, GameManager.Instance.ExpeditionTime))
                {
                    tension = 100f;
                    if (!overtimeCalled)
                    {
                        overtimeCalled = true;
                        GameplayFeedback.Toast(Loc.T("street.overtime_warn"));
                    }
                }
                for (int n = 0; n < 3; n++)
                {
                    float advanced = HordeSchedule.Advance(elapsed, eventCursor, tier, out string kind);
                    if (advanced == eventCursor) break;
                    eventCursor = advanced;
                    if (string.IsNullOrEmpty(kind) || spawner == null) continue;
                    string prefer = HordeSchedule.Prefer(kind);
                    if (!string.IsNullOrEmpty(prefer)) spawner.Prefer(prefer);
                    spawner.SpawnZombies(HordeSchedule.Count(kind));
                    spawner.Prefer(openingPrefer);
                }
                bool night = WorldClock.Instance != null && RaidWatch.Night(WorldClock.Instance.Hour);
                if (HordeSchedule.RunnerPack(elapsed, night, tier, runnersOut))
                {
                    runnersOut = true;
                    if (spawner != null)
                    {
                        spawner.Prefer(HordeSchedule.Prefer("runners"));
                        spawner.SpawnZombies(HordeSchedule.Count("runners"));
                        spawner.Prefer(openingPrefer);
                    }
                }
            }

            if (spawner == null || Time.time < nextSpawn) return;
            nextSpawn = Time.time + spawnInterval;
            int batch = DifficultyProfile.Batch((int)state, runDifficulty);
            if (batch > 0) spawner.SpawnZombies(batch);
        }

        private void OnNoise(Vector3 origin, float radius, NoiseType type)
        {
            if (type == NoiseType.GunshotLoud || type == NoiseType.Explosion) tension = Mathf.Min(100f, tension + 12f);
            else if (type == NoiseType.GunshotQuiet) tension = Mathf.Min(100f, tension + 4f);
            else if (type == NoiseType.SprintFootstep) tension = Mathf.Min(100f, tension + 1f);

            if (scripted) return;
            if ((type == NoiseType.GunshotLoud || type == NoiseType.Explosion) && Time.time >= nextAllowedReinforcement && spawner != null)
            {
                nextAllowedReinforcement = Time.time + 20f;
                spawner.SpawnZombies(2);
            }
        }

        public static TensionState Evaluate(float value)
        {
            if (value > 75f) return TensionState.Peak;
            if (value > 40f) return TensionState.BuildUp;
            if (value > 15f) return TensionState.Relax;
            return TensionState.Calm;
        }

        public void ApplyOpening(float openingTension, float interval, string preferredVariant, int difficulty = 2, int threat = 1)
        {
            runDifficulty = DifficultyProfile.Resolve(difficulty);
            DifficultyBook.Ensure();
            DifficultyProfile.Active = runDifficulty;
            int quality = OutpostZero.Core.SettingsService.Instance != null ? OutpostZero.Core.SettingsService.Instance.Quality : 2;
            spawner?.ApplyCap(DifficultyProfile.AliveCap(quality, runDifficulty));
            tier = threat < 1 ? 1 : threat;
            elapsed = 0f;
            eventCursor = 0f;
            runnersOut = false;
            overtimeCalled = false;
            openingPrefer = preferredVariant ?? "";
            scripted = false;
            nearHeard = false;
            spawner?.Script(0);
            tension = Mathf.Clamp(openingTension, 0f, 100f);
            spawnInterval = Mathf.Max(3f, interval);
            state = Evaluate(tension);
            nextSpawn = Time.time + 2f;
            spawner?.Prefer(preferredVariant);
            OnTensionStateChanged?.Invoke(state);
        }

        public void BeginTutorial(Vector3 origin, Vector3 forward)
        {
            scripted = true;
            tension = TutorialRun.Tension;
            state = Evaluate(tension);
            OnTensionStateChanged?.Invoke(state);
            if (spawner == null) return;
            spawner.Script(TutorialRun.Cap);
            spawner.Clear();
            for (int i = 0; i < TutorialRun.Beats.Length; i++)
            {
                if (!TutorialRun.Point(i, origin.x, origin.z, forward.x, forward.z, out float x, out float z)) continue;
                spawner.SpawnAt(x, z, TutorialRun.Beats[i].Variant);
            }
        }

        public void DropAmbush(bool ambush)
        {
            if (scripted) return;
            int extra = AmbushBeat.Bodies(ambush, AliveCap);
            if (extra <= 0 || spawner == null) return;
            spawner.SpawnZombies(extra);
        }

        public void BeginRaid()
        {
            BeginRaid(8);
        }

        public void BeginRaid(int zombies)
        {
            BeginRaid(zombies, "");
        }

        public void BeginRaid(int zombies, string approach)
        {
            tension = 90f;
            state = TensionState.Peak;
            int count = Mathf.Max(1, zombies);
            if (spawner != null)
            {
                if (string.IsNullOrEmpty(approach)) spawner.SpawnZombies(count);
                else spawner.SpawnRaid(count, approach);
            }
            OnTensionStateChanged?.Invoke(state);
        }
    }
}
