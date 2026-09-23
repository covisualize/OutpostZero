namespace OutpostZero.Combat
{
    /// <summary>
    /// Fire that catches on a body keeps burning after the patch is left behind.
    /// It does not refresh a body that is already alight, so two of them can burn out.
    /// </summary>
    public static class Ember
    {
        public const float Seconds = 3.5f;
        public const float Gap = 0.5f;
        public const float Damage = 4f;
        public const float Spread = 1.4f;

        public static bool Alight(float left)
        {
            return left > 0f;
        }

        public static float Catch(float left)
        {
            if (left > 0f) return left;
            return Seconds;
        }

        public static float Tick(float left, float dt)
        {
            if (left <= 0f) return 0f;
            if (dt <= 0f) return left;
            float next = left - dt;
            return next < 0f ? 0f : next;
        }

        public static bool Due(float before, float after)
        {
            if (before <= 0f || after <= 0f) return false;
            int earlier = (int)(before / Gap);
            int later = (int)(after / Gap);
            return earlier != later;
        }

        public static bool Reaches(float dx, float dz)
        {
            return dx * dx + dz * dz <= Spread * Spread;
        }
    }
}
