using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A fire between a zombie and a person breaks the view.
    /// Standing in the fire does not hide anyone. The cover slabs stay as they are.
    /// </summary>
    public static class SmokeVeil
    {
        public static bool Inside(float x, float z, float originX, float originZ, float radius)
        {
            if (radius <= 0f) return false;
            float dx = x - originX;
            float dz = z - originZ;
            return dx * dx + dz * dz <= radius * radius;
        }

        public static bool Between(float x0, float z0, float x1, float z1, float originX, float originZ, float radius)
        {
            if (radius <= 0f) return false;
            if (Inside(x0, z0, originX, originZ, radius) || Inside(x1, z1, originX, originZ, radius))
                return false;
            float bx = x1 - x0;
            float bz = z1 - z0;
            float len2 = bx * bx + bz * bz;
            float t = 0f;
            if (len2 > 0.0001f)
            {
                float ax = x0 - originX;
                float az = z0 - originZ;
                t = -(ax * bx + az * bz) / len2;
                if (t < 0f) t = 0f;
                if (t > 1f) t = 1f;
            }
            float cx = x0 + bx * t - originX;
            float cz = z0 + bz * t - originZ;
            return cx * cx + cz * cz <= radius * radius;
        }
    }

    /// <summary>A live street fire the sight check can ask about.</summary>
    public class SmokeMark : MonoBehaviour
    {
        private static readonly List<SmokeMark> OpenMarks = new List<SmokeMark>();

        public float Radius = 2.4f;

        public static void Pin(GameObject host, float radius)
        {
            if (host == null) return;
            var mark = host.GetComponent<SmokeMark>();
            if (mark == null) mark = host.AddComponent<SmokeMark>();
            mark.Radius = radius;
        }

        public static bool Hides(float x0, float z0, float x1, float z1)
        {
            for (int i = 0; i < OpenMarks.Count; i++)
            {
                var mark = OpenMarks[i];
                if (mark == null) continue;
                if (SmokeVeil.Between(x0, z0, x1, z1, mark.transform.position.x, mark.transform.position.z, mark.Radius))
                    return true;
            }
            return false;
        }

        private void OnEnable()
        {
            if (!OpenMarks.Contains(this)) OpenMarks.Add(this);
        }

        private void OnDisable()
        {
            OpenMarks.Remove(this);
        }
    }
}
