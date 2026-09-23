namespace OutpostZero.Graphics
{
    /// <summary>
    /// An even rain day becomes a storm. Fog, clear, and the market stay as they were.
    /// A storm bolt comes sooner than a rain bolt.
    /// </summary>
    public static class SkyBand
    {
        public const float BoltGap = 4.5f;

        public static WeatherKind Cast(WeatherKind kind, int day)
        {
            if (kind != WeatherKind.Rain) return kind;
            if (day <= 0) return kind;
            if ((day & 1) == 0) return WeatherKind.Storm;
            return WeatherKind.Rain;
        }

        public static bool Rains(WeatherKind kind)
        {
            return kind == WeatherKind.Rain || kind == WeatherKind.Storm;
        }

        public static bool BoltDue(WeatherKind kind, float lastBolt, float now)
        {
            if (kind != WeatherKind.Storm) return false;
            if (lastBolt <= 0f) return now >= BoltGap;
            return now - lastBolt >= BoltGap;
        }
    }
}
