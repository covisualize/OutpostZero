namespace OutpostZero.Colony
{
    /// <summary>
    /// A cooked meal fills more than a raw scrap and does not sour the day.
    /// A leader cuts a feud in half and keeps the argument from landing.
    /// Resting in a cot without a wound is idle: the shelter helps, the empty shift does not.
    /// </summary>
    public static class MealTable
    {
        public const int CookedFill = 48;
        public const int RawFill = 22;
        public const int RawMood = 4;

        public static void Serve(ref int food, ref int raw, ref float hunger, ref float morale)
        {
            if (hunger >= 92f) return;
            if (food > 0)
            {
                food--;
                hunger = Fill(hunger, CookedFill);
                return;
            }
            if (raw > 0)
            {
                raw--;
                hunger = Fill(hunger, RawFill);
                morale -= RawMood;
            }
        }

        public static float RestMood(bool cot, bool idle)
        {
            if (!cot) return -5f;
            if (idle) return 1f;
            return 6f;
        }

        public static int FeudShift(int drop, bool leaderPresent)
        {
            if (drop < 0) drop = 0;
            if (!leaderPresent) return drop;
            int eased = drop / 2;
            return eased < 1 && drop > 0 ? 1 : eased;
        }

        public static bool Argument(bool volatilePresent, int living, bool leaderPresent)
        {
            if (leaderPresent) return false;
            return living >= 2 && volatilePresent;
        }

        private static float Fill(float hunger, int amount)
        {
            float next = hunger + amount;
            if (next > 100f) return 100f;
            if (next < 0f) return 0f;
            return next;
        }
    }
}
