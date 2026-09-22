namespace OutpostZero.Colony
{
    /// <summary>
    /// A medic past the fourth shift mends six more health for each point after it.
    /// Skill 4 stays on the old eighteen. Skill 8 mends twenty-four more. Twelve stays there.
    /// </summary>
    public static class MedDepth
    {
        public static int Mend(int skill)
        {
            if (skill <= 4) return 0;
            int over = skill - 4;
            if (over > 4) over = 4;
            return over * 6;
        }
    }
}
