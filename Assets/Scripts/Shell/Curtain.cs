namespace OutpostZero.Shell
{
    /// <summary>
    /// The fade to black around every travel: the screen darkens before the loading card shows its
    /// beats, and lifts after the arrival hooks have run, so no half-built step is ever on screen.
    /// </summary>
    public static class Curtain
    {
        public const float Down = 0.25f;
        public const float Up = 0.3f;

        /// <summary>Eased 0 to 1 over <paramref name="duration"/> seconds; a zero duration is already there.</summary>
        public static float Rise(float elapsed, float duration)
        {
            if (duration <= 0f || elapsed >= duration) return 1f;
            if (elapsed <= 0f) return 0f;
            float t = elapsed / duration;
            return t * t * (3f - 2f * t);
        }

        public static float Fall(float elapsed, float duration) => 1f - Rise(elapsed, duration);

        /// <summary>Reduced motion drops the fade and cuts straight to the card.</summary>
        public static float Length(float duration, bool reduced) => reduced ? 0f : duration;
    }
}
