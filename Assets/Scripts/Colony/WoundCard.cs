namespace OutpostZero.Colony
{
    /// <summary>
    /// The camp card names a wound. One is a bite, two is a fever, three is critical.
    /// A clear colonist stays unmarked.
    /// </summary>
    public static class WoundCard
    {
        public static string Line(int injury, string language)
        {
            if (injury <= 0) return "";
            if (injury == 1) return Word("hurt.bite", language);
            if (injury == 2) return Word("hurt.fever", language);
            return Word("hurt.critical", language);
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}
