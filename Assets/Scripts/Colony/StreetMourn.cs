namespace OutpostZero.Colony
{
    /// <summary>
    /// A person lost on the street hurts the camp less than a leader's death.
    /// The memorial cause stays "street".
    /// </summary>
    public static class StreetMourn
    {
        public const float Loss = 8f;
        public const float Back = 4f;

        public static float After(float morale)
        {
            if (morale < 0f) morale = 0f;
            float next = morale - Loss;
            return next < 0f ? 0f : next;
        }

        public static float Lift(float morale)
        {
            if (morale < 0f) morale = 0f;
            float next = morale + Back;
            return next > 100f ? 100f : next;
        }

        public static bool Named(string cause, string bodyName, string memorialName)
        {
            if (cause != Cause()) return false;
            if (string.IsNullOrEmpty(bodyName) || string.IsNullOrEmpty(memorialName)) return false;
            return bodyName == memorialName;
        }

        public static string Cause()
        {
            return "street";
        }

        public static string Line(string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T("camp.home");
            return OutpostZero.Shell.Loc.T("camp.home", language);
        }
    }
}
