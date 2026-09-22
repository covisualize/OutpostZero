namespace OutpostZero.Colony
{
    /// <summary>
    /// Spikes in the yard cut the nearest zombie that steps on them, then wear down.
    /// </summary>
    public static class TrapHit
    {
        public const float Radius = 1.4f;
        public const float Gap = 0.6f;
        public const float Damage = 12f;
        public const int Wear = 8;

        public static bool Due(float since)
        {
            if (since < 0f) return false;
            return since >= Gap;
        }

        public static int Victim(float[] distance, float radius)
        {
            if (distance == null) return -1;
            int best = -1;
            float near = radius;
            for (int i = 0; i < distance.Length; i++)
            {
                if (distance[i] < 0f || distance[i] > near) continue;
                near = distance[i];
                best = i;
            }
            return best;
        }

        public static int WearDown(int integrity, int amount)
        {
            if (integrity < 0) integrity = 0;
            if (amount < 0) amount = 0;
            int next = integrity - amount;
            return next > 0 ? next : 0;
        }
    }
}
