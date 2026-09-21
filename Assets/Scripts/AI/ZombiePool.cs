using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.AI
{
    public class PooledZombie : MonoBehaviour
    {
        public GameObject SourcePrefab;
    }

    /// <summary>
    /// Reuses zombie instances so the horde does not allocate a new object on every spawn.
    /// </summary>
    public class ZombiePool : MonoBehaviour
    {
        public static ZombiePool Instance { get; private set; }

        private readonly Dictionary<GameObject, Stack<GameObject>> available = new Dictionary<GameObject, Stack<GameObject>>();

        public event Action<GameObject> OnReleased;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void RememberPrefab(GameObject prefab)
        {
            if (prefab == null || available.ContainsKey(prefab)) return;
            available[prefab] = new Stack<GameObject>();
        }

        public GameObject Rent(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            RememberPrefab(prefab);

            GameObject zombie = null;
            var stack = available[prefab];
            while (stack.Count > 0 && zombie == null)
            {
                zombie = stack.Pop();
            }

            if (zombie == null)
            {
                zombie = Instantiate(prefab);
                var marker = zombie.GetComponent<PooledZombie>() ?? zombie.AddComponent<PooledZombie>();
                marker.SourcePrefab = prefab;
            }

            zombie.transform.SetPositionAndRotation(position, rotation);
            zombie.SetActive(true);

            var ai = zombie.GetComponent<ZombieAI>();
            if (ai != null)
            {
                ai.ResetForSpawn();
            }

            return zombie;
        }

        public void Release(GameObject zombie, float delay)
        {
            if (zombie == null) return;
            OnReleased?.Invoke(zombie);
            StartCoroutine(DisableLater(zombie, delay));
        }

        private IEnumerator DisableLater(GameObject zombie, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (zombie == null) yield break;

            zombie.SetActive(false);
            zombie.transform.position = new Vector3(0f, -100f, 0f);

            var marker = zombie.GetComponent<PooledZombie>();
            if (marker == null || marker.SourcePrefab == null) yield break;

            if (!available.TryGetValue(marker.SourcePrefab, out var stack))
            {
                stack = new Stack<GameObject>();
                available[marker.SourcePrefab] = stack;
            }
            stack.Push(zombie);
        }
    }
}
