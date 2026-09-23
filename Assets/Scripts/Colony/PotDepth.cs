namespace OutpostZero.Colony
{
    /// <summary>
    /// A cook past the fourth shift plates one more meal for each point after it.
    /// Skill 4 stays on the old plate. Skill 8 plates four more. Twelve stays at four.
    /// </summary>
    public static class PotDepth
    {
        public static int Plate(int skill)
        {
            if (skill <= 4) return 0;
            int over = skill - 4;
            if (over > 4) return 4;
            return over;
        }
    }
}
