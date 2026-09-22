using System;

namespace OutpostZero.Combat
{
    /// <summary>
    /// The magazine reads low at one third or under. The warning breathes slowly instead of strobing.
    /// </summary>
    public static class MagPulse
    {
        public static bool Low(int current, int magazine, bool reloading)
        {
            if (reloading || magazine <= 0) return false;
            if (current <= 0) return true;
            return current * 3 <= magazine;
        }

        public static float Fill(float elapsed, float duration)
        {
            if (duration <= 0.01f) return 1f;
            if (elapsed <= 0f) return 0f;
            if (elapsed >= duration) return 1f;
            return elapsed / duration;
        }

        public static float Alpha(float time, bool low)
        {
            if (!low) return 1f;
            float wave = (float)Math.Abs(Math.Sin(time * 2.2));
            return 0.45f + 0.55f * wave;
        }
    }
}
