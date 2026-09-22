namespace OutpostZero.Colony
{
    /// <summary>
    /// The day line and the morning toast follow the language.
    /// English keeps Day 3  18:30 and Day 4  morning watch.
    /// </summary>
    public static class ClockFace
    {
        public static string Read(int day, float hour, string language)
        {
            if (day < 1) day = 1;
            if (hour < 0f) hour = 0f;
            if (hour >= 24f) hour %= 24f;
            int h = (int)hour;
            int m = (int)((hour - h) * 60f);
            if (m < 0) m = 0;
            if (m > 59) m = 59;
            return Word("clock.day", language) + " " + day + "  " + h.ToString("00") + ":" + m.ToString("00");
        }

        public static string Morning(int day, string language)
        {
            if (day < 1) day = 1;
            return Word("clock.day", language) + " " + day + "  " + Word("clock.morning", language);
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Shell.Loc.T(key);
            return Shell.Loc.T(key, language);
        }
    }
}
