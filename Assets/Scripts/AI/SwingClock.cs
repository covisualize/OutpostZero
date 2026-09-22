namespace OutpostZero.AI
{
    /// <summary>
    /// A bite winds up and connects at 60% of the swing. Stepping out before that misses.
    /// </summary>
    public static class SwingClock
    {
        public const float HitAt = 0.6f;

        public static float Advance(float t, float dt, float duration)
        {
            if (t < 0f) t = 0f;
            if (duration <= 0.01f) return 1f;
            if (dt < 0f) dt = 0f;
            float next = t + dt / duration;
            return next > 1f ? 1f : next;
        }

        public static bool Connects(float before, float after)
        {
            return before < HitAt && after >= HitAt;
        }
    }
}
