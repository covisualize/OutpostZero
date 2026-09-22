namespace OutpostZero.Colony
{
    /// <summary>
    /// A farm feeds the camp after three mornings. A purifier and a collector add water,
    /// and rain adds one more. A running generator draws a harder raid; barricades cut it back.
    /// </summary>
    public static class CampYield
    {
        public const int FarmWait = 3;
        public const int FarmFood = 2;
        public const int PurifierWater = 2;
        public const int RainBonus = 1;
        public const int CollectorWater = 1;

        public struct Plot
        {
            public string Kind;
            public int Age;
            public int Integrity;
        }

        public static void Advance(Plot[] plots)
        {
            if (plots == null) return;
            for (int i = 0; i < plots.Length; i++)
            {
                if (plots[i].Integrity <= 0) continue;
                if (plots[i].Kind == "Farm") plots[i].Age++;
            }
        }

        public static void Produce(Plot[] plots, bool rain, out int food, out int water)
        {
            food = 0;
            water = 0;
            if (plots == null) return;
            for (int i = 0; i < plots.Length; i++)
            {
                if (plots[i].Integrity <= 0) continue;
                if (plots[i].Kind == "Farm" && plots[i].Age >= FarmWait) food += FarmFood;
                if (plots[i].Kind == "Purifier") water += PurifierWater + (rain ? RainBonus : 0);
                if (plots[i].Kind == "Water") water += CollectorWater;
            }
        }

        public static int RaidPressure(int basePressure, bool generator, int barricades)
        {
            if (basePressure < 0) basePressure = 0;
            if (barricades < 0) barricades = 0;
            int walls = barricades > 3 ? 3 : barricades;
            int next = basePressure + (generator ? 2 : 0) - walls;
            if (next < 1) return 1;
            return next;
        }
    }
}
