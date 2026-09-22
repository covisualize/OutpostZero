namespace OutpostZero.Colony
{
    /// <summary>
    /// Raid toasts follow the language. The approach id stays gate, alley, yard, or fence.
    /// </summary>
    public static class RaidSay
    {
        public static string Open(bool tower, string approach, string language)
        {
            if (tower) return Word("raid.tower", language);
            return Word("raid.from", language) + " " + Face(approach, language);
        }

        public static string Held(bool broadcast, string language)
        {
            return Word(broadcast ? "raid.broadcast" : "raid.held", language);
        }

        public static string Broke(string language)
        {
            return Word("raid.broke", language);
        }

        public static string Coming(string approach, string language)
        {
            return Word("raid.coming", language) + " " + Face(approach, language);
        }

        public static string Face(string approach, string language)
        {
            string key = approach == "alley" ? "raid.alley"
                : approach == "yard" ? "raid.yard"
                : approach == "fence" ? "raid.fence"
                : approach == "gate" ? "raid.gate"
                : "";
            if (key.Length == 0) return approach ?? "";
            return Word(key, language);
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}
