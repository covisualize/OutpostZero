namespace OutpostZero.Expedition
{
    /// <summary>
    /// A barred street door stays shut until a blade lands three times.
    /// The way back out is never barred.
    /// </summary>
    public static class DoorBar
    {
        public const int Hits = 3;
        public const float Noise = 8f;
        public const float BreakNoise = 12f;

        public static bool Holds(int hitsLeft)
        {
            return hitsLeft > 0;
        }

        public static int After(int hitsLeft)
        {
            if (hitsLeft <= 0) return 0;
            return hitsLeft - 1;
        }

        public static string Face(string language)
        {
            return Word("door.bar", language);
        }

        public static string Hold(string language)
        {
            return Word("door.hold", language);
        }

        public static string Gives(string language)
        {
            return Word("door.gives", language);
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}
