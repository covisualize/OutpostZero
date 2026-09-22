namespace OutpostZero.Expedition
{
    /// <summary>
    /// A medkit spent on the person following you eases their bite by one.
    /// It does not heal the leader.
    /// </summary>
    public static class FollowEase
    {
        public static bool Helps(int injury, int kits)
        {
            return injury > 0 && kits > 0;
        }

        public static int After(int injury)
        {
            return OutpostZero.Colony.WoundEase.After(injury);
        }

        public static string Prompt(string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T("ease.follow");
            return OutpostZero.Shell.Loc.T("ease.follow", language);
        }

        public static string Line(string language)
        {
            return OutpostZero.Colony.WoundEase.Line(language);
        }
    }
}
