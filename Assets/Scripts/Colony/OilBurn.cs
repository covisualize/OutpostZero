namespace OutpostZero.Colony
{
    /// <summary>
    /// A trench of oil stays dark until fire reaches it, then burns for eight seconds.
    /// </summary>
    public static class OilBurn
    {
        public const float Duration = 8f;
        public const float Gap = 0.5f;
        public const float Radius = 2.2f;
        public const float Ignite = 3.5f;
        public const float Damage = 14f;

        public static bool Burning(float started, float now)
        {
            if (started < 0f) return false;
            float elapsed = now - started;
            return elapsed >= 0f && elapsed < Duration;
        }

        public static bool Ignites(float dx, float dz)
        {
            return dx * dx + dz * dz <= Ignite * Ignite;
        }

        public static int Victim(float[] distance)
        {
            if (distance == null) return -1;
            int best = -1;
            float near = Radius;
            for (int i = 0; i < distance.Length; i++)
            {
                if (distance[i] < 0f || distance[i] > near) continue;
                near = distance[i];
                best = i;
            }
            return best;
        }
    }
}
