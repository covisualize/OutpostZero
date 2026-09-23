using OutpostZero.Graphics;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A finished water collector catches more in the rain, and still more in a storm.
    /// A clear day, a broken tank, and every other module add nothing.
    /// </summary>
    public static class RainCatch
    {
        public const int RainExtra = 1;
        public const int StormExtra = 2;

        public static int Extra(string kind, int integrity, int site, WeatherKind sky)
        {
            if (site != 0 || integrity <= 0) return 0;
            if (kind != "Water") return 0;
            if (sky == WeatherKind.Storm) return StormExtra;
            if (sky == WeatherKind.Rain) return RainExtra;
            return 0;
        }

        public static string Line(WeatherKind sky, string language)
        {
            if (sky == WeatherKind.Storm) return Word("rain.storm", language);
            if (sky == WeatherKind.Rain) return Word("rain.catch", language);
            return "";
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}
