namespace OutpostZero.Colony
{
    /// <summary>
    /// A finished module below full integrity can be mended. Open sites come first.
    /// The worst board is the one a builder takes.
    /// </summary>
    public static class MendBoard
    {
        public const int Step = 25;

        public static bool Needs(int site, int integrity)
        {
            return site == 0 && integrity > 0 && integrity < 100;
        }

        public static int Mend(int integrity, int pace)
        {
            if (integrity <= 0) return integrity < 0 ? 0 : integrity;
            if (pace < 0) pace = 0;
            int next = integrity + pace * Step;
            if (next > 100) return 100;
            return next;
        }

        public static int Pick(int[] sites, int[] integrity)
        {
            if (sites == null || integrity == null) return -1;
            int count = sites.Length < integrity.Length ? sites.Length : integrity.Length;
            int best = -1;
            int lowest = 100;
            for (int i = 0; i < count; i++)
            {
                if (!Needs(sites[i], integrity[i])) continue;
                if (integrity[i] < lowest)
                {
                    lowest = integrity[i];
                    best = i;
                }
            }
            return best;
        }
    }
}
