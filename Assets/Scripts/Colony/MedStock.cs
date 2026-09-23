namespace OutpostZero.Colony
{
    /// <summary>
    /// A medic on shift opens one stocked med when somebody needs it: the worst hurt survivor
    /// mends one more injury level and the leader takes fifteen more health.
    /// Nobody hurt keeps the stock sealed.
    /// </summary>
    public static class MedStock
    {
        public const float Heal = 15f;

        public static bool Open(int stock, int worstInjury, float leaderMissing)
        {
            if (stock <= 0) return false;
            return worstInjury > 0 || leaderMissing > 0.5f;
        }

        public static int Worst(int[] injuries)
        {
            if (injuries == null) return -1;
            int worst = -1;
            for (int i = 0; i < injuries.Length; i++)
            {
                if (injuries[i] <= 0) continue;
                if (worst < 0 || injuries[i] > injuries[worst]) worst = i;
            }
            return worst;
        }
    }
}
