namespace OutpostZero.Shell
{
    /// <summary>
    /// Reads the seed typed on the New Game card. Blank or "random" rolls a fresh world;
    /// digits are used as they are; any other word hashes to a stable seed so it can be shared.
    /// </summary>
    public static class NewGamePlan
    {
        public const int MaxLength = 24;

        public static bool TryParse(string text, out int seed)
        {
            seed = 0;
            if (text == null) return false;
            text = text.Trim();
            if (text.Length == 0) return false;
            if (text.Length > MaxLength) text = text.Substring(0, MaxLength);
            if (string.Equals(text, "random", System.StringComparison.OrdinalIgnoreCase)) return false;
            if (int.TryParse(text, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int number))
            {
                seed = DistrictGenerator.Resolve(number);
                return true;
            }
            seed = DistrictGenerator.Resolve(Hash(text.ToLowerInvariant()));
            return true;
        }

        public static int Roll(int tick)
        {
            int rolled = Hash(tick.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return DistrictGenerator.Resolve(rolled);
        }

        public static int Camp(int worldSeed)
        {
            int camp = worldSeed ^ 0x5eed;
            return camp == 0 ? DistrictGenerator.DefaultSeed + 3 : camp;
        }

        private static int Hash(string text)
        {
            uint hash = 2166136261u;
            for (int i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 16777619u;
            }
            return (int)(hash & 0x7fffffff);
        }
    }
}
