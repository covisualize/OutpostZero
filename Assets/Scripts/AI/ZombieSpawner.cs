using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using OutpostZero.Sensory;
using OutpostZero.Core;

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
        [SerializeField] private float spawnInterval = 8f;
        [SerializeField] private int spawnBatchSize = 2;

        private readonly List<GameObject> activeZombies = new List<GameObject>();
        private float nextSpawnTime;
        private Transform playerTransform;

        private void Start()
        {
            var player = FindObjectOfType<Player.PlayerController>();
            if (player != null) playerTransform = player.transform;

            SpawnInitialHorde();

            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.OnNoiseEmitted += HandleLoudNoiseAlert;
            }
        }

        private void OnDestroy()
        {
            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.OnNoiseEmitted -= HandleLoudNoiseAlert;
            }
        }

        private void Update()
        {
            // Prune dead/destroyed zombies
            activeZombies.RemoveAll(z => z == null);

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

        public void SpawnZombies(int count)
        {
            GameObject defaultPrefab = zombiePrefab;
            if (defaultPrefab == null && zombiePrefabVariants != null && zombiePrefabVariants.Length > 0)
            {
                defaultPrefab = zombiePrefabVariants[0];
            }
            if (defaultPrefab == null) return;

            Vector3 center = playerTransform != null ? playerTransform.position : transform.position;

            for (int i = 0; i < count; i++)
            {
                if (activeZombies.Count >= maxAliveZombies) break;

                GameObject chosenPrefab = defaultPrefab;
                if (zombiePrefabVariants != null && zombiePrefabVariants.Length > 0)
                {
                    chosenPrefab = zombiePrefabVariants[Random.Range(0, zombiePrefabVariants.Length)];
                }

                Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minDistanceFromPlayer, spawnRadius);
                Vector3 candidatePos = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (NavMesh.SamplePosition(candidatePos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    GameObject zombie = Instantiate(chosenPrefab, hit.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                    zombie.SetActive(true);
                    activeZombies.Add(zombie);
                }
            }
        }

        private void HandleLoudNoiseAlert(Vector3 origin, float radius, NoiseType type)
        {
            // Loud gunshots or explosions attract additional roving zombies
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
