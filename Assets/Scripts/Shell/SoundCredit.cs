namespace OutpostZero.Shell
{
    /// <summary>
    /// Every played tone is generated in the game. A missing id has no credit.
    /// </summary>
    public static class SoundCredit
    {
        public const string Source = "generated";

        public static int Count => ClipBook.Ids.Length;

        public static bool Covers(string id)
        {
            return ClipBook.Has(id);
        }

        public static string Line(string id)
        {
            if (!Covers(id)) return "";
            return id + " — " + Source;
        }
    }
}
