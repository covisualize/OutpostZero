namespace OutpostZero.Shell
{
    /// <summary>
    /// The body speaks when it is failing. A pulse under a quarter of health,
    /// and a breath under a fifth of stamina. A dead body is quiet.
    /// </summary>
    public static class BodyCue
    {
        public const float HeartLine = 0.25f;
        public const float BreathLine = 0.2f;
        public const float HeartSlow = 1.1f;
        public const float HeartFast = 0.42f;
        public const float BreathGap = 2.4f;

        public static bool Heart(float ratio)
        {
            return ratio > 0f && ratio < HeartLine;
        }

        public static bool Breath(float ratio)
        {
            return ratio > 0f && ratio < BreathLine;
        }

        public static float HeartGap(float ratio)
        {
            if (!Heart(ratio)) return 0f;
            float t = ratio / HeartLine;
            return HeartFast + (HeartSlow - HeartFast) * t;
        }

        public static float HeartPitch(float ratio)
        {
            if (!Heart(ratio)) return 1f;
            float t = 1f - ratio / HeartLine;
            return 0.92f - 0.22f * t;
        }

        public static bool Due(float now, float last, float gap)
        {
            if (gap <= 0f) return false;
            if (last <= 0f) return true;
            return OutpostZero.Core.Tick.Past(now, last, gap);
        }
    }
}
