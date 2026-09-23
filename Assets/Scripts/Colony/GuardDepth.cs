namespace OutpostZero.Colony
{
    /// <summary>
    /// A guard past the fourth shift holds one more post for each point after it.
    /// Skill 4 stays on the old bonus. Skill 8 holds four more. Twelve stays at four.
    /// </summary>
    public static class GuardDepth
    {
        public static int Post(int skill)
        {
            if (skill <= 4) return 0;
            int over = skill - 4;
            if (over > 4) return 4;
            return over;
        }
    }
}
