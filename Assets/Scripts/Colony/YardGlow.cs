using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A finished fire drifts embers. A lamp that still stands but is not whole spits sparks.
    /// An unbuilt site and a dead lamp stay dark.
    /// </summary>
    public static class YardGlow
    {
        public const float Gap = 0.6f;
        public const int Embers = 8;
        public const int Sparks = 5;
        public const float Spit = 0.18f;

        public static bool EmbersDue(bool fireReady, float now, float last)
        {
            if (!fireReady) return false;
            return Due(now, last);
        }

        public static bool SparksDue(int site, int integrity, float now, float last)
        {
            if (!MendBoard.Needs(site, integrity)) return false;
            return Due(now, last);
        }

        private static bool Due(float now, float last)
        {
            if (last <= 0f) return true;
            if (now < last) return true;
            return now - last >= Gap;
        }
    }

    /// <summary>Remembers the last ember or spark so a yard piece does not spit every frame.</summary>
    public class YardMote : MonoBehaviour
    {
        public float Last;
    }
}
