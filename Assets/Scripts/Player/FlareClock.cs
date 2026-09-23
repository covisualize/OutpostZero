namespace OutpostZero.Player
{
    /// <summary>
    /// A lit flare burns for twenty seconds and calls the dead back on a steady pulse.
    /// </summary>
    public static class FlareClock
    {
        public const float Duration = 20f;
        public const float Pulse = 2.5f;
        public const float Radius = 16f;

        public static bool Lit(float elapsed)
        {
            return Lit(elapsed, Duration);
        }

        public static bool Lit(float elapsed, float burn)
        {
            return elapsed >= 0f && elapsed < burn;
        }

        public static bool PulseDue(float previous, float now)
        {
            if (now < 0f || now < previous) return false;
            if (!Lit(now)) return false;
            if (previous < 0f) return true;
            int before = (int)(previous / Pulse);
            int after = (int)(now / Pulse);
            return after > before;
        }
    }
}
