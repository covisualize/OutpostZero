namespace OutpostZero.Colony
{
    /// <summary>
    /// A campfire turns raw food into meals. Without a fire, or without anything raw, the pot stays thin.
    /// A cook also stews a batch into the food store before the day is served.
    /// The pot stops when the food store is full. No cook leaves the raw pile alone.
    /// </summary>
    public static class CookPot
    {
        public const int MealFood = 3;
        public const int ThinFood = 1;
        public const int MealMorale = 8;
        public const int ThinMorale = 2;
        public const int Batch = 2;

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

        public static int Stew(int raw, int food, int cap, bool cook)
        {
            if (!cook || raw <= 0 || cap <= 0) return 0;
            if (food < 0) food = 0;
            int room = cap - food;
            if (room <= 0) return 0;
            int batch = raw < Batch ? raw : Batch;
            if (batch > room) return room;
            return batch;
        }
    }
}
