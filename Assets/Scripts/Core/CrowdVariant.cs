namespace OutpostZero.Core
{
    /// <summary>
    /// Three looks for a crowd built from two idle takes and two gaits, so a pack of the same
    /// zombie doesn't breathe and step in lockstep.
    /// </summary>
    public static class CrowdVariant
    {
        public const int Count = 3;

        /// <summary>Blend-tree threshold for the <paramref name="index"/>th of <paramref name="takes"/> alternates.</summary>
        public static float Threshold(int index, int takes)
        {
            if (takes <= 1) return 0f;
            if (index <= 0) return 0f;
            if (index >= takes - 1) return 1f;
            return index / (float)(takes - 1);
        }

        /// <summary>Stable variant for one body; the same seed always looks the same.</summary>
        public static int Pick(int seed)
        {
            return (int)(Hash((uint)seed) % Count);
        }

        private static uint Hash(uint h)
        {
            h ^= h >> 16;
            h *= 0x7feb352du;
            h ^= h >> 15;
            h *= 0x846ca68bu;
            h ^= h >> 16;
            return h;
        }

        /// <summary>Idle blend: the plain slouch, the deeper one, or halfway between.</summary>
        public static float Idle(int variant)
        {
            switch (variant)
            {
                case 1: return 1f;
                case 2: return 0.5f;
                default: return 0f;
            }
        }

        /// <summary>Gait blend is never mixed: two gaits with different strides would skate.</summary>
        public static float Walk(int variant)
        {
            return variant == 2 ? 1f : 0f;
        }

        public static string WalkClip(int variant)
        {
            return variant == 2 ? "Shamble" : "Walk";
        }

        /// <summary>One of the three falls, picked apart from the idle so looks and deaths don't pair up.</summary>
        public static float Death(int seed)
        {
            return Threshold((int)(Hash((uint)seed ^ 0x9e3779b9u) % 3u), 3);
        }

        /// <summary>Where in its loop the body starts, 0 to 1, so neighbours don't step together.</summary>
        public static float CycleOffset(int seed)
        {
            return Hash((uint)seed * 31u + 7u) % 1000u / 1000f;
        }
    }
}
