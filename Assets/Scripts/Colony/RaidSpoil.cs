namespace OutpostZero.Colony
{
    /// <summary>
    /// A breach spills the stores. A night that holds the boards keeps them.
    /// </summary>
    public static class RaidSpoil
    {
        public const int Floor = 4;
        public const int Cap = 12;
        public const int MealCap = 3;

        public static int Scrap(bool breached, int stored)
        {
            if (!breached || stored <= 0) return 0;
            int loss = stored / 4;
            if (loss < Floor) loss = stored < Floor ? stored : Floor;
            if (loss > Cap) return Cap;
            return loss;
        }

        public static int Meals(bool breached, int food)
        {
            if (!breached || food <= 0) return 0;
            int loss = food / 3;
            if (loss < 1) loss = 1;
            if (loss > MealCap) return MealCap;
            return loss;
        }
    }
}
