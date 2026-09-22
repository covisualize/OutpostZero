namespace OutpostZero.Player
{
    /// <summary>
    /// A wound from camp follows the leader onto the street.
    /// A bite trims the pace. A fever and a critical wound refuse a sprint.
    /// </summary>
    public static class StreetLimp
    {
        public const float Bite = 0.92f;
        public const float Fever = 0.78f;
        public const float Critical = 0.62f;

        public static float Pace(float speed, int injury)
        {
            if (speed < 0f) speed = 0f;
            if (injury <= 0) return speed;
            float scale = injury == 1 ? Bite : injury == 2 ? Fever : Critical;
            return speed * scale;
        }

        public static bool AllowsSprint(int injury)
        {
            return injury < OutpostZero.Colony.FeverSpread.Sick;
        }

        public static string Line(int injury, string language)
        {
            if (injury <= 0) return "";
            if (injury == 1) return Word("limp.bite", language);
            if (injury == 2) return Word("limp.fever", language);
            return Word("limp.critical", language);
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}
