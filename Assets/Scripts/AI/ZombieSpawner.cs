using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using OutpostZero.Sensory;
using OutpostZero.Core;
using OutpostZero.Colony;

namespace OutpostZero.AI
{
    public class ZombieSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private GameObject[] zombiePrefabVariants;
        [SerializeField] private int initialCount = 15;
        [SerializeField] private int maxAliveZombies = 35;
        [SerializeField] private float spawnRadius = 30f;
        [SerializeField] private float minDistanceFromPlayer = 12f;

        [Header("Continuous Spawning")]
        [SerializeField] private bool periodicSpawn = true;
        [SerializeField] private bool directorOwnsSpawns;
        [SerializeField] private float spawnInterval = 8f;
        [SerializeField] private int spawnBatchSize = 2;

        private readonly List<GameObject> activeZombies = new List<GameObject>();
        private float nextSpawnTime;
        private Transform playerTransform;
        private ZombiePool pool;
        private string preferredName = "";
        private string[] variantNames;

        public void Prefer(string nameFragment)
        {
            preferredName = nameFragment ?? "";
        }

        public void UseDirectorForSpawns()
        {
            periodicSpawn = false;
            directorOwnsSpawns = true;
        }

        private int scriptedCap;

        public int MaxAlive => scriptedCap > 0 ? scriptedCap : maxAliveZombies;

        public void ApplyCap(int max)
        {
            maxAliveZombies = Mathf.Max(4, max);
        }

        public void Script(int cap)
        {
            scriptedCap = Mathf.Max(0, cap);
        }

        public void Clear()
        {
            for (int i = activeZombies.Count - 1; i >= 0; i--)
            {
                var zombie = activeZombies[i];
                if (zombie == null) continue;
                if (pool != null) pool.Release(zombie, 0f);
                else Destroy(zombie);
            }
            activeZombies.Clear();
        }

        public IReadOnlyList<GameObject> Active => activeZombies;

        public bool SpawnAt(float x, float z, string variant) => SpawnAt(x, z, variant, out _);

        public bool SpawnAt(float x, float z, string variant, out GameObject zombie)
        {
            zombie = null;
            if (activeZombies.Count >= MaxAlive) return false;
            GameObject fallback = zombiePrefab;
            if (fallback == null && zombiePrefabVariants != null && zombiePrefabVariants.Length > 0) fallback = zombiePrefabVariants[0];
            if (fallback == null) return false;
            GameObject chosen = Named(variant) ?? fallback;
            Vector3 pos = new Vector3(x, 0f, z);
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 6f, NavMesh.AllAreas)) pos = hit.position;
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            zombie = pool != null ? pool.Rent(chosen, pos, rotation) : Instantiate(chosen, pos, rotation);
            if (zombie == null) return false;
            zombie.SetActive(true);
            activeZombies.Add(zombie);
            return true;
        }

        public float Nearest(Vector3 point)
        {
            float best = float.MaxValue;
            for (int i = 0; i < activeZombies.Count; i++)
            {
                var zombie = activeZombies[i];
                if (zombie == null || !zombie.activeInHierarchy) continue;
                float d = (zombie.transform.position - point).sqrMagnitude;
                if (d < best) best = d;
            }
            return best == float.MaxValue ? best : Mathf.Sqrt(best);
        }

        private GameObject Named(string fragment)
        {
            if (string.IsNullOrEmpty(fragment) || zombiePrefabVariants == null) return null;
            for (int i = 0; i < zombiePrefabVariants.Length; i++)
            {
                var candidate = zombiePrefabVariants[i];
                if (candidate != null && candidate.name.IndexOf(fragment, System.StringComparison.Ordinal) >= 0) return candidate;
            }
            return null;
        }

        public void Configure(GameObject prefab, GameObject[] variants, int initial, int maxAlive)
        {
            zombiePrefab = prefab;
            zombiePrefabVariants = variants;
            variantNames = null;
            initialCount = initial;
            maxAliveZombies = maxAlive;
        }

        private void Awake()
        {
            pool = Attach.Ensure<ZombiePool>(gameObject);
        }

        private void Start()
        {
            var player = PlayerRegistry.Current;
            if (player != null) playerTransform = player.transform;

            if (pool != null)
            {
                pool.OnReleased += HandleReleased;
                pool.RememberPrefab(zombiePrefab);
                if (zombiePrefabVariants != null)
                {
                    foreach (var variant in zombiePrefabVariants)
                    {
                        pool.RememberPrefab(variant);
                    }
                }
            }

            SpawnInitialHorde();

            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.OnNoiseEmitted += HandleLoudNoiseAlert;
            }
        }

        private void OnDestroy()
        {
            if (pool != null)
            {
                pool.OnReleased -= HandleReleased;
            }

            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.OnNoiseEmitted -= HandleLoudNoiseAlert;
            }
        }

        private void HandleReleased(GameObject zombie)
        {
            activeZombies.Remove(zombie);
        }

        private void Update()
        {
            if (playerTransform == null && PlayerRegistry.Current != null)
            {
                playerTransform = PlayerRegistry.Current.transform;
            }

            if (periodicSpawn && Time.time >= nextSpawnTime)
            {
                nextSpawnTime = Time.time + spawnInterval;
                if (activeZombies.Count < maxAliveZombies)
                {
                    SpawnZombies(spawnBatchSize);
                }
            }
        }

        private void SpawnInitialHorde()
        {
            SpawnZombies(initialCount);
        }

        public void SpawnRaid(int count, string approach)
        {
            GameObject defaultPrefab = zombiePrefab;
            if (defaultPrefab == null && zombiePrefabVariants != null && zombiePrefabVariants.Length > 0)
            {
                defaultPrefab = zombiePrefabVariants[0];
            }
            if (defaultPrefab == null) return;
            if (count < 1) count = 1;

            for (int i = 0; i < count; i++)
            {
                if (activeZombies.Count >= maxAliveZombies) break;
                RaidDrop.Point(approach, i, count, out float dropX, out float dropZ);
                Vector3 candidate = new Vector3(dropX, 0f, dropZ);
                Vector3 pos = candidate;
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 6f, NavMesh.AllAreas)) pos = hit.position;
                GameObject chosenPrefab = ChoosePrefab(defaultPrefab);
                Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                GameObject zombie = pool != null
                    ? pool.Rent(chosenPrefab, pos, rotation)
                    : Instantiate(chosenPrefab, pos, rotation);
                if (zombie == null) continue;
                zombie.SetActive(true);
                activeZombies.Add(zombie);
                var ai = zombie.GetComponent<ZombieAI>();
                if (ai == null) continue;
                ai.MarkRaid();
                if (GridBuilder.Instance != null && GridBuilder.Instance.BoardFor(approach, i, out float boardX, out float boardZ))
                    ai.PostAt(boardX, boardZ);
            }
        }

        public void SpawnZombies(int count)
        {
            GameObject defaultPrefab = zombiePrefab;
            if (defaultPrefab == null && zombiePrefabVariants != null && zombiePrefabVariants.Length > 0)
            {
                defaultPrefab = zombiePrefabVariants[0];
            }
            if (defaultPrefab == null) return;

            Vector3 center = playerTransform != null ? playerTransform.position : transform.position;

            var camera = Camera.main;
            for (int i = 0; i < count; i++)
            {
                if (activeZombies.Count >= maxAliveZombies) break;

                GameObject chosenPrefab = ChoosePrefab(defaultPrefab);
                bool placed = false;
                for (int attempt = 0; attempt < 8 && !placed; attempt++)
                {
                    float reach = Mathf.Max(SpawnRing.MinDistance, minDistanceFromPlayer);
                    Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(reach, spawnRadius);
                    Vector3 candidatePos = center + new Vector3(randomCircle.x, 0f, randomCircle.y);
                    if (!NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 5f, NavMesh.AllAreas)) continue;
                    if (!SpawnRing.Allowed(hit.position.x, hit.position.z, center.x, center.z)) continue;
                    if (camera != null)
                    {
                        Vector3 forward = camera.transform.forward;
                        Vector3 right = camera.transform.right;
                        Vector3 up = camera.transform.up;
                        Vector3 origin = camera.transform.position;
                        if (ViewVolume.Seen(
                            origin.x, origin.y, origin.z,
                            forward.x, forward.y, forward.z,
                            right.x, right.y, right.z,
                            up.x, up.y, up.z,
                            camera.fieldOfView, camera.aspect, camera.nearClipPlane, camera.farClipPlane,
                            hit.position.x, hit.position.y + 1f, hit.position.z,
                            0.4f, 0.9f, 0.4f)) continue;
                    }

                    Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    GameObject zombie = pool != null
                        ? pool.Rent(chosenPrefab, hit.position, rotation)
                        : Instantiate(chosenPrefab, hit.position, rotation);
                    if (zombie == null) continue;
                    zombie.SetActive(true);
                    activeZombies.Add(zombie);
                    placed = true;
                }
            }
        }

        private GameObject ChoosePrefab(GameObject fallback)
        {
            if (zombiePrefabVariants == null || zombiePrefabVariants.Length == 0) return fallback;
            if (!string.IsNullOrEmpty(preferredName) && Random.value < 0.72f)
            {
                int matches = 0;
                GameObject pick = null;
                for (int i = 0; i < zombiePrefabVariants.Length; i++)
                {
                    var candidate = zombiePrefabVariants[i];
                    if (candidate == null || candidate.name.IndexOf(preferredName, System.StringComparison.Ordinal) < 0) continue;
                    matches++;
                    if (Random.Range(0, matches) == 0) pick = candidate;
                }
                if (pick != null) return pick;
            }

            int index = DifficultyTable.Pick(VariantNames(), DifficultyTable.Of(DifficultyProfile.Active), Random.value);
            if (index < 0) index = Random.Range(0, zombiePrefabVariants.Length);
            var chosen = zombiePrefabVariants[index];
            return chosen != null ? chosen : fallback;
        }

        private string[] VariantNames()
        {
            if (variantNames != null && variantNames.Length == zombiePrefabVariants.Length) return variantNames;
            variantNames = new string[zombiePrefabVariants.Length];
            for (int i = 0; i < zombiePrefabVariants.Length; i++)
                variantNames[i] = zombiePrefabVariants[i] != null ? zombiePrefabVariants[i].name : "";
            return variantNames;
        }

        private void HandleLoudNoiseAlert(Vector3 origin, float radius, NoiseType type)
        {
            // Loud gunshots or explosions attract additional roving zombies
            if (directorOwnsSpawns) return;
            if (type == NoiseType.GunshotLoud || type == NoiseType.Explosion)
            {
                if (activeZombies.Count < maxAliveZombies)
                {
                    SpawnZombies(2);
                }
            }
        }
    }
}
