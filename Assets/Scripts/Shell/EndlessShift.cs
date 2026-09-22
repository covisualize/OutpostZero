namespace OutpostZero.Shell
{
    /// <summary>
    /// After the broadcast, days on the street add pressure and shorten the quiet.
    /// Day 0 is the campaign. The bonus stops climbing after six days.
    /// </summary>
    public static class EndlessShift
    {
        public static int Bonus(int days)
        {
            if (days < 1) return 0;
            if (days > 6) return 6;
            return days;
        }

        public static float Tension(int days)
        {
            return Bonus(days) * 4f;
        }

        public static float IntervalScale(int days)
        {
            float scale = 1f - Bonus(days) * 0.08f;
            if (scale < 0.55f) return 0.55f;
            return scale;
        }
    }
}
