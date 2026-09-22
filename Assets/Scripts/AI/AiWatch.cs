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
            if (cooldown < 0f) cooldown = 0f;
            if (string.IsNullOrEmpty(state)) state = "-";
            if (string.IsNullOrEmpty(target)) target = "-";
            int tenths = (int)(cooldown * 10f + 0.5f);
            if (tenths < 0) tenths = 0;
            return state + "  " + target + "  " + (tenths / 10) + "." + (tenths % 10);
        }

        public static string Page(string[] lines, int limit)
        {
            if (!Open) return "";
            if (limit < 1) limit = 1;
            int count = lines == null ? 0 : lines.Length;
            int shown = count < limit ? count : limit;
            string text = "watch";
            for (int i = 0; i < shown; i++)
            {
                string row = lines[i];
                if (string.IsNullOrEmpty(row)) row = "-";
                text += "\n" + row;
            }
            if (count > shown) text += "\n+";
            return text;
        }
    }
}
