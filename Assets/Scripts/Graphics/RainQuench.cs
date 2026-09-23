namespace OutpostZero.Graphics
{
    /// <summary>
    /// Rain and storm eat a fire faster. Clear, fog, and overcast leave the burn alone.
    /// The dry lifetimes stay on the fire itself.
    /// </summary>
    public static class RainQuench
    {
        public const float Rain = 2.5f;
        public const float Storm = 3.5f;

        public static float Pace(WeatherKind kind)
        {
            if (kind == WeatherKind.Storm) return Storm;
            if (kind == WeatherKind.Rain) return Rain;
            return 1f;
        }

        public static float Step(float dt, WeatherKind kind)
        {
            if (dt < 0f) dt = 0f;
            return dt * Pace(kind);
        }

        public static float Now(float dt)
        {
            var sky = WeatherController.Instance;
            if (sky == null) return dt < 0f ? 0f : dt;
            return Step(dt, sky.Kind);
        }
    }
}
