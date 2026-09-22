namespace OutpostZero.Colony
{
    /// <summary>
    /// Belt, drop, pack, and poison lines follow the language.
    /// English keeps Belt is full, Dropped Bandage, Pack is full, and Poisoned.
    /// </summary>
    public static class PackSay
    {
        public static string Full(string language)
        {
            return Word("pack.fullbelt", language);
        }

        public static string Clear(string language)
        {
            return Word("pack.clear", language);
        }

        public static string Slot(int key, string language)
        {
            if (key < 5) key = 5;
            if (key > 8) key = 8;
            return Word("pack.belt", language) + " " + key;
        }

        public static string Dropped(string id, string name, string language)
        {
            string named = string.IsNullOrEmpty(language) ? Shell.Loc.Item(id) : Shell.Loc.Item(id, language);
            if ((string.IsNullOrEmpty(named) || named == (id ?? "")) && !string.IsNullOrEmpty(name)) named = name;
            if (string.IsNullOrEmpty(named)) named = id ?? "";
            return Word("pack.dropped", language) + " " + named;
        }

        public static string Pack(string language)
        {
            return Word("pack.pack", language);
        }

        public static string Poison(string language)
        {
            return Word("pack.poison", language);
        }

        public static string Made(string id, string fallback, string language)
        {
            string key = "recipe." + (id ?? "");
            string line = Word(key, language);
            if (line == key) line = string.IsNullOrEmpty(fallback) ? (id ?? "") : fallback;
            return Word("pack.made", language) + " " + line;
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Shell.Loc.T(key);
            return Shell.Loc.T(key, language);
        }
    }
}
