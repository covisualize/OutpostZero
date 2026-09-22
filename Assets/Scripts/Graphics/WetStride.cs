namespace OutpostZero.Graphics
{
    /// <summary>
    /// Rain shortens a step. A puddle shortens it again.
    /// Fog and a dry street leave the pace alone.
    /// </summary>
    public static class WetStride
    {
        public const float Rain = 0.9f;
        public const float Puddle = 0.75f;

        public static float Scale(float wetness, bool puddle)
        {
            if (puddle && RainPuddle.Shows(wetness)) return Puddle;
            if (wetness >= 0.65f) return Rain;
            return 1f;
        }

        public static float Pace(float speed, float wetness, bool puddle)
        {
            if (speed < 0f) speed = 0f;
            return speed * Scale(wetness, puddle);
        }
    }
}
