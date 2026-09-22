namespace OutpostZero.Colony
{
    /// <summary>
    /// The fourth shift is the first change. Each point after that still matters.
    /// Skill 4 stays on the old numbers. Skill 8 is the deep end.
    /// </summary>
    public static class HandDepth
    {
        public static float Spread(int skill)
        {
            return 1f - 0.03f * Over(skill);
        }

        public static float Reload(int skill)
        {
            return 1f - 0.025f * Over(skill);
        }

        public static int Heal(int skill)
        {
            return Over(skill);
        }

        public static int Scrap(int skill)
        {
            return Over(skill);
        }

        private static int Over(int skill)
        {
            if (skill <= 4) return 0;
            int over = skill - 4;
            if (over > 4) return 4;
            return over;
        }
    }
}
