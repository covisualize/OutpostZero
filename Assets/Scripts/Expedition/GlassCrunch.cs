using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// A broken pane leaves a shard on the street.
    /// The first step on it crunches and nicks. Later steps only crunch.
    /// </summary>
    public static class GlassCrunch
    {
        public const float Radius = 0.55f;
        public const float Reach = 1.28f;
        public const float Nick = 3f;
        public const int Cap = 8;

        public static bool On(float x, float z, float shardX, float shardZ)
        {
            float dx = x - shardX;
            float dz = z - shardZ;
            return dx * dx + dz * dz <= Radius * Radius;
        }
    }

    public class GlassShard : MonoBehaviour
    {
        private static readonly List<GlassShard> all = new List<GlassShard>();
        private bool cut;

        public static bool Covers(float x, float z)
        {
            for (int i = 0; i < all.Count; i++)
            {
                var shard = all[i];
                if (shard == null) continue;
                if (GlassCrunch.On(x, z, shard.transform.position.x, shard.transform.position.z)) return true;
            }
            return false;
        }

        public static bool BiteAt(float x, float z)
        {
            for (int i = 0; i < all.Count; i++)
            {
                var shard = all[i];
                if (shard == null) continue;
                if (!GlassCrunch.On(x, z, shard.transform.position.x, shard.transform.position.z)) continue;
                return shard.Bite();
            }
            return false;
        }

        public static void Leave(Vector3 at)
        {
            while (all.Count >= GlassCrunch.Cap)
            {
                var old = all[0];
                all.RemoveAt(0);
                if (old != null) Destroy(old.gameObject);
            }
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Shard_glass";
            var collider = body.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            body.transform.position = new Vector3(at.x, 0.03f, at.z);
            body.transform.localScale = new Vector3(0.42f, 0.02f, 0.28f);
            if (!PaneGlass.Coat(body.GetComponent<Renderer>()))
            {
                var block = new MaterialPropertyBlock();
                var renderer = body.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.GetPropertyBlock(block);
                    block.SetColor("_BaseColor", PaneGlass.Tint);
                    renderer.SetPropertyBlock(block);
                }
            }
            body.AddComponent<GlassShard>();
        }

        private bool Bite()
        {
            if (cut) return false;
            cut = true;
            return true;
        }

        private void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
        }

        private void OnDisable()
        {
            all.Remove(this);
        }
    }
}
