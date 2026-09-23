namespace OutpostZero.Colony
{
    /// <summary>
    /// Yard build lines follow the language.
    /// English keeps Build mode: click the yard, Facing 90, and Site marked Generator.
    /// </summary>
    public static class YardSay
    {
        public static string Mode(bool on, string language)
        {
            return Word(on ? "yard.mode" : "yard.off", language);
        }

        public static string Facing(int degrees, string language)
        {
            return Word("yard.facing", language) + " " + degrees;
        }

        public static string Recovered(int scrap, string language)
        {
            if (scrap < 0) scrap = 0;
            return Word("yard.recovered", language) + " " + scrap + " " + Word("yard.scrap", language);
        }

        public static string Taken(string language)
        {
            return Word("yard.taken", language);
        }

        public static string Outside(string language)
        {
            return Word("yard.outside", language);
        }

        public static string Need(int cost, string language)
        {
            if (cost < 0) cost = 0;
            return Word("yard.need", language) + " " + cost + " " + Word("yard.needtail", language);
        }

        public static string Marked(string kind, string language)
        {
            return Word("yard.marked", language) + " " + Kind(kind, language);
        }

        public static string Up(string kind, string language)
        {
            return Kind(kind, language) + " " + Word("yard.up", language);
        }

        public static string Mend(string kind, bool whole, string language)
        {
            return Word(whole ? "yard.patched" : "yard.mended", language) + " " + Kind(kind, language);
        }

        public static string Barricade(string language)
        {
            return Word("yard.gave", language);
        }

        public static string Spikes(string language)
        {
            return Word("yard.spikes", language);
        }

        public static string Oil(string language)
        {
            return Word("yard.oil", language);
        }

        public static string Short(string language)
        {
            return Word("stall.short", language);
        }

        public static string Stores(string language)
        {
            return Word("camp.strip_full", language);
        }

        public static string Kind(string kind, string language)
        {
            if (string.IsNullOrEmpty(kind)) return "";
            string key = "yard.kind." + kind.ToLowerInvariant();
            string line = Word(key, language);
            return line == key ? kind : line;
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Shell.Loc.T(key);
            return Shell.Loc.T(key, language);
        }
    }
}
