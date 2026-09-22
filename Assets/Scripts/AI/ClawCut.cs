namespace OutpostZero.AI
{
    /// <summary>
    /// A runner claw opens a bleed. Any bite can still start an infection.
    /// </summary>
    public static class ClawCut
    {
        public const float RunnerBleed = 0.35f;
        public const float BiteInfect = 0.2f;

        public static bool Opens(bool runner, float roll)
        {
            if (!runner) return false;
            if (roll < 0f) roll = 0f;
            if (roll > 1f) roll = 1f;
            return roll < RunnerBleed;
        }

        public static bool Infects(float roll)
        {
            if (roll < 0f) roll = 0f;
            if (roll > 1f) roll = 1f;
            return roll < BiteInfect;
        }
    }
}
