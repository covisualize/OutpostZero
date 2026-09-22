using OutpostZero.Graphics;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A storm wears the outdoor yard. Rain nicks it. A roofed module and a clear day stay whole.
    /// </summary>
    public static class StormWear
    {
        public const int StormHit = 6;
        public const int RainHit = 2;

        public static bool Outdoor(string kind)
        {
            return kind == "Barricade" || kind == "Water" || kind == "Watchtower"
                || kind == "Generator" || kind == "Farm" || kind == "Purifier"
                || kind == "Turret" || kind == "Spikes" || kind == "Oil" || kind == "Lamp";
        }

        public static int After(int integrity, int site, string kind, WeatherKind sky)
        {
            if (integrity <= 0) return 0;
            if (site != 0) return integrity;
            int hit = sky == WeatherKind.Storm ? StormHit : sky == WeatherKind.Rain ? RainHit : 0;
            if (hit <= 0 || !Outdoor(kind)) return integrity;
            int next = integrity - hit;
            return next < 0 ? 0 : next;
        }

        public static string Line(WeatherKind sky, string language)
        {
            if (sky == WeatherKind.Storm) return Word("yard.storm", language);
            if (sky == WeatherKind.Rain) return Word("yard.rain", language);
            return "";
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}
