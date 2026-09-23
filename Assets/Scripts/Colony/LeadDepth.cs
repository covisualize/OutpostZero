namespace OutpostZero.Colony
{
    /// <summary>
    /// A leader past the fourth shift quiets one more feud point for each step after it.
    /// Skill 4 stays on the old cut. Skill 8 can take the last of a halved quarrel.
    /// </summary>
    public static class LeadDepth
    {
        public static int Ease(int cut, int skill)
        {
            if (cut <= 0) return 0;
            if (skill <= 4) return cut;
            int over = skill - 4;
            if (over > 4) over = 4;
            int next = cut - over;
            return next < 0 ? 0 : next;
        }
    }
}
