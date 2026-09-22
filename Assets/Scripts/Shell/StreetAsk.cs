namespace OutpostZero.Shell
{
    /// <summary>
    /// Street prompts and the district line follow the language.
    /// English keeps Search container, Find the cache, and Ash Market's alley line.
    /// </summary>
    public static class StreetAsk
    {
        public static string Search(string language) => Word("ask.search", language);
        public static string Take(string language) => Word("ask.take", language);
        public static string Radio(string language) => Word("ask.radio", language);
        public static string Cache(string language) => Word("ask.cache", language);
        public static string Gear(string language) => Word("ask.gear", language);
        public static string Back(string language) => Word("ask.back", language);

        public static string Along(string name, string language)
        {
            string line = Word("ask.along", language) + " " + Person(name, language);
            string tail = Word("ask.along2", language);
            if (!string.IsNullOrEmpty(tail)) line += " " + tail;
            return line;
        }

        public static string ToGate(string name, string language)
        {
            return Word("ask.togate", language) + " " + Person(name, language) + " " + Word("ask.togate2", language);
        }

        public static string With(string name, string language)
        {
            return Person(name, language) + " " + Word("ask.with", language);
        }

        public static string Takes(string name, string language)
        {
            return Person(name, language) + " " + Word("ask.takes", language);
        }

        public static string Stays(string name, string language)
        {
            return Person(name, language) + " " + Word("ask.stays", language);
        }

        public static string Cry(string name, string language)
        {
            return Person(name, language) + " " + Word("ask.cry", language);
        }

        public static string Stowed(bool radio, string language)
        {
            return Word(radio ? "ask.stowed" : "ask.searched", language);
        }

        public static string Kept(bool gear, string language)
        {
            return Word(gear ? "ask.kept" : "ask.nameonly", language);
        }

        public static string Brief(string id, string fallback, string language)
        {
            string key = "ask.brief." + (id ?? "");
            string line = Word(key, language);
            if (line == key || string.IsNullOrEmpty(line)) return fallback ?? "";
            return line;
        }

        public static string Place(string id, string fallbackName, string encounter, string language)
        {
            string name = string.IsNullOrEmpty(language) ? Loc.District(id) : Loc.District(id, language);
            if (string.IsNullOrEmpty(name) || name == (id ?? "")) name = string.IsNullOrEmpty(fallbackName) ? (id ?? "") : fallbackName;
            return name + " — " + Brief(id, encounter, language);
        }

        private static string Person(string name, string language)
        {
            if (!string.IsNullOrEmpty(name) && name != "Survivor") return name;
            return Word("ask.survivor", language);
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Loc.T(key);
            return Loc.T(key, language);
        }
    }
}
