namespace OutpostZero.AI
{
    /// <summary>
    /// F3 shows each zombie's state, who they are after, and how long the special stays down.
    /// </summary>
    public static class AiWatch
    {
        public static bool Open { get; private set; }

        public static void Toggle() => Open = !Open;

        public static void Close() => Open = false;

        public static string Line(string state, string target, float cooldown)
        {
            return Line(state, target, cooldown, "en");
        }

        public static string Line(string state, string target, float cooldown, string language)
        {
            if (cooldown < 0f) cooldown = 0f;
            if (string.IsNullOrEmpty(state)) state = "-";
            if (string.IsNullOrEmpty(target)) target = "-";
            int tenths = (int)(cooldown * 10f + 0.5f);
            if (tenths < 0) tenths = 0;
            return Face(state, language) + "  " + Who(target, language) + "  " + (tenths / 10) + "." + (tenths % 10);
        }

        public static string Page(string[] lines, int limit)
        {
            return Page(lines, limit, "en");
        }

        public static string Page(string[] lines, int limit, string language)
        {
            if (!Open) return "";
            if (limit < 1) limit = 1;
            int count = lines == null ? 0 : lines.Length;
            int shown = count < limit ? count : limit;
            string text = Word("watch.title", language);
            for (int i = 0; i < shown; i++)
            {
                string row = lines[i];
                if (string.IsNullOrEmpty(row)) row = "-";
                text += "\n" + row;
            }
            if (count > shown) text += "\n+";
            return text;
        }

        private static string Face(string state, string language)
        {
            if (state == "-") return state;
            string key = "watch.state." + state.ToLowerInvariant();
            string line = Word(key, language);
            return line == key ? state : line;
        }

        private static string Who(string target, string language)
        {
            if (target == "-" || target != "Player") return target;
            return Word("watch.player", language);
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}
