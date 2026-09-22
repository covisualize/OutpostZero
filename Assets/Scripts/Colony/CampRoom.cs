namespace OutpostZero.Colony
{
    /// <summary>
    /// Camp stores share one room. A storage crate raises it. Goods already over the
    /// limit stay put; new goods wait until something is spent or another crate is built.
    /// </summary>
    public static class CampRoom
    {
        public const int Base = 80;
        public const int PerCrate = 40;
        public const int Scrap = 1;
        public const int Food = 2;
        public const int Water = 2;
        public const int Cloth = 3;
        public const int Chemicals = 4;
        public const int Tape = 3;

        public static int Room(int crates)
        {
            if (crates < 0) crates = 0;
            return Base + crates * PerCrate;
        }

        public static int Bulk(int scrap, int food, int water, int cloth, int chemicals, int tape)
        {
            if (scrap < 0) scrap = 0;
            if (food < 0) food = 0;
            if (water < 0) water = 0;
            if (cloth < 0) cloth = 0;
            if (chemicals < 0) chemicals = 0;
            if (tape < 0) tape = 0;
            return scrap * Scrap + food * Food + water * Water + cloth * Cloth + chemicals * Chemicals + tape * Tape;
        }

        public static int Fit(int used, int unit, int amount, int room)
        {
            if (amount <= 0 || unit <= 0) return 0;
            if (used < 0) used = 0;
            if (room < 0) room = 0;
            int space = room - used;
            if (space <= 0) return 0;
            int max = space / unit;
            if (amount < max) return amount;
            return max;
        }
    }
}
