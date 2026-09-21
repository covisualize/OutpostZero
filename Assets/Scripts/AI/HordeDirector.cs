using System;
using UnityEngine;
using OutpostZero.Core;

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

        public float Tension => tension;
        public TensionState State => state;
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
            float drain = state == TensionState.Relax ? 4f : 1.2f;
            tension = Mathf.Clamp(tension - drain * Time.deltaTime, 0f, 100f);
            TensionState next = Evaluate(tension);
            if (next != state)
            {
                state = next;
                OnTensionStateChanged?.Invoke(state);
            }

            if (spawner == null || Time.time < nextSpawn) return;
            nextSpawn = Time.time + spawnInterval;
            int batch = state == TensionState.Peak ? 4 : state == TensionState.BuildUp ? 2 : 1;
            if (state != TensionState.Calm)
            {
                spawner.SpawnZombies(batch);
            }
        }

        private void OnNoise(Vector3 origin, float radius, NoiseType type)
        {
            if (type == NoiseType.GunshotLoud || type == NoiseType.Explosion) tension = Mathf.Min(100f, tension + 12f);
            else if (type == NoiseType.GunshotQuiet) tension = Mathf.Min(100f, tension + 4f);
            else if (type == NoiseType.SprintFootstep) tension = Mathf.Min(100f, tension + 1f);

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

        public void BeginRaid()
        {
            tension = 90f;
            state = TensionState.Peak;
            if (spawner != null) spawner.SpawnZombies(8);
            OnTensionStateChanged?.Invoke(state);
        }
    }
}
