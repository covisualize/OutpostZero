namespace OutpostZero.Colony
{
    /// <summary>
    /// A street dose eases the camp wound by one step. A clear leader stays clear.
    /// </summary>
    public static class WoundEase
    {
        public static bool Helps(int injury)
        {
            return injury > 0;
        }

        public static int After(int injury)
        {
            if (injury <= 0) return 0;
            return injury - 1;
        }

        public static string Line(string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T("ease.wound");
            return OutpostZero.Shell.Loc.T("ease.wound", language);
        }

        public static string Note(string dose, bool eased, string language)
        {
            if (!eased) return dose ?? "";
            string ease = Line(language);
            if (string.IsNullOrEmpty(dose)) return ease;
            return dose + "  " + ease;
        }
    }
}
