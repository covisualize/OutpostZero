namespace OutpostZero.Colony
{
    /// <summary>
    /// A bitten guard stays on the line and shoots short.
    /// A fever or a critical wound is already off the line.
    /// </summary>
    public static class PostBite
    {
        public const float Reach = 0.72f;
        public const float Hit = 0.7f;

        public static float Range(int injury)
        {
            if (injury >= 2) return 0f;
            if (injury <= 0) return GuardVolley.Range;
            return GuardVolley.Range * Reach;
        }

        public static float Damage(int injury)
        {
            if (injury >= 2) return 0f;
            if (injury <= 0) return GuardVolley.Damage;
            return GuardVolley.Damage * Hit;
        }

        public static string Line(string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T("post.bite");
            return OutpostZero.Shell.Loc.T("post.bite", language);
        }
    }
}
