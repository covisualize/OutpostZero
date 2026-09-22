namespace OutpostZero.Colony
{
    /// <summary>
    /// A thin line loses. Walls, guards, and lights are what make a later night hold.
    /// A breach still loses, whatever was built.
    /// </summary>
    public static class RaidOutcome
    {
        public static int Need(int day)
        {
            if (day < 1) day = 1;
            int need = 1 + day / 3;
            if (need > 6) return 6;
            return need;
        }

        public static int Strength(int walls, int guards, int lights)
        {
            if (walls < 0) walls = 0;
            if (guards < 0) guards = 0;
            if (lights < 0) lights = 0;
            return walls + guards * 2 + lights;
        }

        public static bool Holds(int day, int walls, int guards, int lights, bool breached)
        {
            if (breached) return false;
            return Strength(walls, guards, lights) >= Need(day);
        }
    }
}
