using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Flies sit on a dumpster and thin out as the ear walks away.
    /// Only the nearest bin inside the reach is voiced.
    /// </summary>
    public static class FlyBed
    {
        public const float Volume = 0.16f;
        public const float Reach = 8f;

        public static bool Counts(string name)
        {
            return !string.IsNullOrEmpty(name) && name.IndexOf("Dumpster", System.StringComparison.Ordinal) >= 0;
        }

        public static float Gain(float distance)
        {
            if (distance < 0f || distance >= Reach) return 0f;
            return Volume * (1f - distance / Reach);
        }

        public static int Nearest(float[] distances)
        {
            if (distances == null) return -1;
            int best = -1;
            float near = 0f;
            for (int i = 0; i < distances.Length; i++)
            {
                float d = distances[i];
                if (d < 0f || d >= Reach) continue;
                if (best < 0 || d < near)
                {
                    best = i;
                    near = d;
                }
            }
            return best;
        }
    }

    /// <summary>
    /// A dumpster that can carry the fly bed. The installer adds one when the name matches.
    /// </summary>
    public class FlyMark : MonoBehaviour
    {
        public static readonly List<FlyMark> Live = new List<FlyMark>();

        private void OnEnable()
        {
            if (!Live.Contains(this)) Live.Add(this);
        }

        private void OnDisable()
        {
            Live.Remove(this);
        }
    }
}
