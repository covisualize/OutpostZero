namespace OutpostZero.Colony
{
    /// <summary>
    /// A person lost on the street hurts the camp less than a leader's death.
    /// The memorial cause stays "street".
    /// </summary>
    public static class StreetMourn
    {
        public const float Loss = 8f;

        public static float After(float morale)
        {
            if (morale < 0f) morale = 0f;
            float next = morale - Loss;
            return next < 0f ? 0f : next;
        }

        public static string Cause()
        {
            return "street";
        }
    }
}
