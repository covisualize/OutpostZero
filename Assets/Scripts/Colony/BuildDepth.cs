namespace OutpostZero.Colony
{
    /// <summary>
    /// A builder past the fourth shift raises one more hour for each point after it.
    /// Skill 4 stays on the old bonus. Skill 8 raises four more. Twelve stays at four.
    /// </summary>
    public static class BuildDepth
    {
        public static int Raise(int skill)
        {
            if (skill <= 4) return 0;
            int over = skill - 4;
            if (over > 4) return 4;
            return over;
        }
    }
}
