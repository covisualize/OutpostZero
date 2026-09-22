namespace OutpostZero.Combat
{
    /// <summary>
    /// Bright lights stay under three a second.
    /// Turning flashes off skips the light entirely. A storm bolt waits eight seconds.
    /// </summary>
    public static class FlashCap
    {
        public const float Gap = 0.34f;
        public const float Storm = 8f;

        private static float stamp;

        public static float Stamp => stamp;

        public static bool Take(float now, bool quiet)
        {
            if (!Allow(stamp, now, quiet)) return false;
            stamp = now;
            return true;
        }

        public static bool Allow(float last, float now, bool quiet)
        {
            if (quiet) return false;
            if (now < last) return true;
            return now - last >= Gap;
        }

        public static bool Due(bool rain, float lastBolt, float now)
        {
            if (!rain) return false;
            if (lastBolt <= 0f) return now >= Storm;
            return now - lastBolt >= Storm;
        }
    }
}
