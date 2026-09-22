namespace OutpostZero.Colony
{
    /// <summary>
    /// A campfire turns raw food into meals. Without a fire, or without anything raw, the pot stays thin.
    /// </summary>
    public static class CookPot
    {
        public const int MealFood = 3;
        public const int ThinFood = 1;
        public const int MealMorale = 8;
        public const int ThinMorale = 2;

        public static void Serve(bool fire, int raw, int hands, out int spent, out int food, out int morale)
        {
            spent = 0;
            food = 0;
            morale = 0;
            if (hands <= 0) return;
            if (raw < 0) raw = 0;
            if (!fire || raw <= 0)
            {
                food = ThinFood;
                morale = ThinMorale;
                return;
            }
            int batch = hands < raw ? hands : raw;
            spent = batch;
            food = batch * MealFood;
            morale = MealMorale;
        }
    }
}
