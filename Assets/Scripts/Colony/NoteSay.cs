namespace OutpostZero.Colony
{
    /// <summary>
    /// The camp day line follows the language.
    /// The stored notes stay grief, fever, and stew.
    /// </summary>
    public static class NoteSay
    {
        public static string Read(string packed, string language)
        {
            if (string.IsNullOrEmpty(packed)) return "";
            string[] parts = packed.Split(',');
            string text = "";
            for (int i = 0; i < parts.Length; i++)
            {
                string id = parts[i].Trim();
                if (id.Length == 0) continue;
                if (text.Length > 0) text += ", ";
                text += One(id, language);
            }
            return text;
        }

        public static string One(string id, string language)
        {
            if (string.IsNullOrEmpty(id)) return "";
            string key = "note." + id.ToLowerInvariant();
            string line = Word(key, language);
            return line == key ? id : line;
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Shell.Loc.T(key);
            return Shell.Loc.T(key, language);
        }
    }
}
