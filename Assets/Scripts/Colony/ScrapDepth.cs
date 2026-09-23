namespace OutpostZero.Colony
{
    /// <summary>
    /// A scavenger past the fourth shift brings one more scrap for each point after it.
    /// Skill 4 stays on the old bonus. Skill 8 brings four more. Twelve stays at four.
    /// </summary>
    public static class ScrapDepth
    {
        public static int Extra(int skill)
        {
            if (skill <= 4) return 0;
            int over = skill - 4;
            if (over > 4) return 4;
            return over;
        }
    }
}
