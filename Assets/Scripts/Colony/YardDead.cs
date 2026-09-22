namespace OutpostZero.Colony
{
    /// <summary>
    /// A raid leaves bodies in the yard. A steady clear shift hauls two.
    /// What is still there in the morning costs morale, and the cost stops at eighteen.
    /// </summary>
    public static class YardDead
    {
        public const int Haul = 2;
        public const int Mood = 6;
        public const int MoodCap = 18;

        public static int Dropped(int pressure, bool held)
        {
            if (pressure < 1) return 0;
            if (held) return pressure;
            return pressure / 2;
        }

        public static int Hands(float morale)
        {
            if (morale < 10f) return 0;
            if (morale < 30f) return 1;
            return Haul;
        }

        public static int Left(int bodies, int hauled)
        {
            if (bodies < 0) bodies = 0;
            if (hauled < 0) hauled = 0;
            int next = bodies - hauled;
            return next < 0 ? 0 : next;
        }

        public static int MoodHit(int left)
        {
            if (left <= 0) return 0;
            int hit = left * Mood;
            if (hit > MoodCap) return MoodCap;
            return hit;
        }
    }
}
