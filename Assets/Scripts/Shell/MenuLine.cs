namespace OutpostZero.Shell
{
    /// <summary>
    /// Save slots and binding rows follow the language.
    /// English keeps the same slot line and the same key prompt.
    /// </summary>
    public static class MenuLine
    {
        public static string Title(string language)
        {
            return Word("slot.title", language);
        }

        public static string Slot(int number, int day, string leader, bool occupied, string language)
        {
            if (number < 1) number = 1;
            if (day < 0) day = 0;
            if (leader == null) leader = "";
            if (!occupied) return Word("slot.word", language) + " " + number + "  " + Word("slot.empty", language);
            return Word("slot.word", language) + " " + number + "  " + Word("slot.day", language) + " " + day + "  " + leader;
        }

        public static string Auto(int day, string leader, string language)
        {
            if (day < 0) day = 0;
            if (leader == null) leader = "";
            return Word("slot.auto", language) + "  " + Word("slot.day", language) + " " + day + "  " + leader;
        }

        public static string KeyWait(string action, string language)
        {
            return Word("bind.press", language) + " " + Action(action, language);
        }

        public static string KeyBound(string action, string binding, string language)
        {
            if (binding == null) binding = "";
            return Action(action, language) + ": " + binding;
        }

        public static string PadWait(string action, string language)
        {
            return Word("bind.button", language) + " " + Action(action, language);
        }

        public static string PadBound(string action, string binding, string language)
        {
            if (binding == null) binding = "";
            return Word("bind.pad", language) + " " + Action(action, language) + ": " + binding;
        }

        public static string Action(string action, string language)
        {
            if (string.IsNullOrEmpty(action)) return "";
            string key = "bind." + action;
            string word = Word(key, language);
            return word == key ? action : word;
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Loc.T(key);
            return Loc.T(key, language);
        }
    }
}
