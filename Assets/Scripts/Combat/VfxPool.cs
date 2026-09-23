using System;
using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;
using Object = UnityEngine.Object;

namespace OutpostZero.Combat
{
    /// <summary>
    /// Reuses effect instances per <see cref="VfxEvent"/>. A rented instance comes back active at the
    /// requested spot and parks itself after its life; the caller re-arms it each time. Authored
    /// prefabs from the <see cref="VfxLibrary"/> win over the procedural builder.
    /// </summary>
    public static class VfxPool
    {
        private static readonly Dictionary<VfxEvent, Stack<GameObject>> Idle = new Dictionary<VfxEvent, Stack<GameObject>>();
        private static Transform root;
        private static bool warmed;

        /// <summary>
        /// Rents an instance of <paramref name="id"/>; <paramref name="fresh"/> is true when it was just
        /// built, so one-off setup (components, materials) can run once.
        /// </summary>
        public static GameObject Rent(VfxEvent id, Vector3 position, Quaternion rotation, Func<GameObject> build, out bool fresh)
        {
            var entry = VfxLibrary.Active != null ? VfxLibrary.Active.Find(id) : null;
            GameObject go = null;
            if (Idle.TryGetValue(id, out var stack))
            {
                while (stack.Count > 0 && go == null) go = stack.Pop();
            }
            fresh = go == null;
            if (fresh)
            {
                go = entry != null && entry.prefab != null ? Object.Instantiate(entry.prefab) : build();
                if (go.GetComponent<VfxSeat>() == null) go.AddComponent<VfxSeat>();
                go.AddComponent<VfxReturn>();
                VfxStats.Make();
            }
            else
            {
                VfxStats.Reuse();
            }
            go.transform.SetParent(null, false);
            go.transform.SetPositionAndRotation(position, rotation);
            float life = entry != null && entry.life > 0f ? entry.life : VfxBook.Life(id);
            go.GetComponent<VfxReturn>().Arm(id, Time.time + life);
            go.SetActive(true);
            if (entry != null && entry.prefab != null) Replay(go);
            return go;
        }

        /// <summary>Plays <paramref name="id"/> from the library prefab if one is authored; false leaves it to the caller.</summary>
        public static bool PlayAuthored(VfxEvent id, Vector3 position, Quaternion rotation)
        {
            var entry = VfxLibrary.Active != null ? VfxLibrary.Active.Find(id) : null;
            if (entry == null || entry.prefab == null) return false;
            Rent(id, position, rotation, null, out _);
            return true;
        }

        /// <summary>Parks a pooled instance, or destroys a plain one.</summary>
        public static void Release(GameObject go)
        {
            if (go == null) return;
            var tag = go.GetComponent<VfxReturn>();
            if (tag == null)
            {
                Object.Destroy(go);
                return;
            }
            Park(go, tag.Id);
        }

        internal static void Park(GameObject go, VfxEvent id)
        {
            if (!go.activeSelf && go.transform.parent == Root) return;
            if (!Idle.TryGetValue(id, out var stack))
            {
                stack = new Stack<GameObject>();
                Idle[id] = stack;
            }
            var entry = VfxLibrary.Active != null ? VfxLibrary.Active.Find(id) : null;
            int keep = entry != null && entry.keep > 0 ? entry.keep : VfxBook.Keep(id);
            if (!VfxBook.Keeps(stack.Count, keep))
            {
                VfxStats.Drop();
                Object.Destroy(go);
                return;
            }
            go.SetActive(false);
            go.transform.SetParent(Root, false);
            stack.Push(go);
            VfxStats.Park();
        }

        /// <summary>Builds the library's prewarm counts up front so the first firefight doesn't allocate.</summary>
        public static void Prewarm()
        {
            if (warmed) return;
            warmed = true;
            var library = VfxLibrary.Active;
            if (library == null || library.entries == null) return;
            foreach (var entry in library.entries)
            {
                if (entry == null || entry.prefab == null) continue;
                for (int i = 0; i < entry.prewarm; i++)
                {
                    var go = Rent(entry.id, Vector3.zero, Quaternion.identity, null, out _);
                    Park(go, entry.id);
                }
            }
        }

        /// <summary>Drops every idle instance, e.g. when a scene unloads.</summary>
        public static void Clear()
        {
            foreach (var stack in Idle.Values)
            {
                while (stack.Count > 0)
                {
                    var go = stack.Pop();
                    VfxStats.Forget();
                    if (go != null) Object.Destroy(go);
                }
            }
        }

        private static Transform Root
        {
            get
            {
                if (root == null)
                {
                    var go = new GameObject("VfxPool");
                    Object.DontDestroyOnLoad(go);
                    root = go.transform;
                }
                return root;
            }
        }

        private static void Replay(GameObject go)
        {
            foreach (var particles in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Clear(true);
                particles.Play(true);
            }
            foreach (var trail in go.GetComponentsInChildren<TrailRenderer>(true)) trail.Clear();
        }
    }

    /// <summary>Returns a pooled effect once its life runs out.</summary>
    public class VfxReturn : MonoBehaviour
    {
        public VfxEvent Id { get; private set; }
        private float until;

        public void Arm(VfxEvent id, float at)
        {
            Id = id;
            until = at;
        }

        private void Update()
        {
            if (Time.time >= until) VfxPool.Park(gameObject, Id);
        }
    }
}
