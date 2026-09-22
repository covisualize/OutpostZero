namespace OutpostZero.Colony
{
    /// <summary>
    /// The last push of a raid brings a brute. That blow hits the boards harder.
    /// A defender already at three wounds dies when the line breaks.
    /// </summary>
    public static class RaidBreach
    {
        public const int BrutePhase = 2;
        public const int Scale = 2;
        public const int Cap = 40;
        public const int Fatal = 3;

        public static bool Brute(int phase)
        {
            return phase >= BrutePhase;
        }

        public static int Blow(int hit, bool brute)
        {
            if (hit < 1) hit = 1;
            if (!brute) return hit;
            int next = hit * Scale;
            return next > Cap ? Cap : next;
        }

        public static int Wound(int injury, bool breach, out bool fallen)
        {
            fallen = false;
            if (injury < 0) injury = 0;
            if (!breach) return injury;
            if (injury >= Fatal)
            {
                fallen = true;
                return Fatal;
            }
            return injury + 1;
        }
    }
}
