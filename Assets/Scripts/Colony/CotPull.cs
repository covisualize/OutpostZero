namespace OutpostZero.Colony
{
    /// <summary>
    /// Quarantine is a bed for someone already hurt. A healthy colonist stays on the board.
    /// </summary>
    public static class CotPull
    {
        public static bool Holds(int injury)
        {
            return injury > 0;
        }

        public static string Bed(string language)
        {
            return Word("cot.bed", language);
        }

        public static string Refuse(string language)
        {
            return Word("cot.well", language);
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}
