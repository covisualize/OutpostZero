using OutpostZero.Graphics;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A storm drinks the generator harder once the night burn has already started.
    /// A clear night, a rain night, and a tank that did not move stay as they were.
    /// </summary>
    public static class StormBurn
    {
        public const float Pull = 0.8f;

        public static float After(float before, float after, WeatherKind sky)
        {
            if (before < 0f) before = 0f;
            if (after < 0f) after = 0f;
            if (after > before) after = before;
            if (sky != WeatherKind.Storm) return after;
            float burned = before - after;
            if (burned <= 0f) return after;
            float next = after - burned * Pull;
            return next < 0f ? 0f : next;
        }
    }
}
