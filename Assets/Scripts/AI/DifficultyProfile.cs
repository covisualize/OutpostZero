namespace OutpostZero.AI
{
    /// <summary>
    /// Scavenger, Survivor, and Nightmare. Threat tier and the day stretch the same curve.
    /// A stored 0 is Survivor, so an old save does not drop to the easy curve.
    /// </summary>
    public static class DifficultyProfile
    {
        public struct Curve
        {
            public float Tension;
            public float Interval;
            public int ExtraKills;
            public string Prefer;
        }

        /// <summary>Most zombies alive at once on the Low quality tier, whatever the difficulty.</summary>
        public const int LowTierAlive = 16;

        /// <summary>The spawner cap: the quality tier's crowd, held at <see cref="LowTierAlive"/> on Low so Nightmare cannot outgrow an integrated GPU.</summary>
        public static int AliveCap(int quality)
        {
            int tier = OutpostZero.Graphics.QualityProfile.For(quality).Zombies;
            if (quality <= 0) return tier < LowTierAlive ? tier : LowTierAlive;
            return tier;
        }

        public static int Resolve(int stored)
        {
            if (stored <= 0) return 2;
            if (stored >= 3) return 3;
            return stored;
        }

        public static string Name(int stored)
        {
            int level = Resolve(stored);
            if (level == 1) return "Scavenger";
            if (level == 3) return "Nightmare";
            return "Survivor";
        }

        public static Curve For(int tier, int day, int difficulty)
        {
            int level = Resolve(difficulty);
            if (tier < 1) tier = 1;
            if (day < 1) day = 1;
            float perTier = level == 1 ? 2f : level == 3 ? 8f : 4f;
            float tension = (tier - 1) * perTier;
            if (level == 3) tension += day * 0.5f;
            float interval = level == 1 ? 1.15f : level == 3 ? 0.75f : 1f;
            int extra = level == 1 ? 0 : level == 3 ? tier + 1 : tier - 1;
            if (extra < 0) extra = 0;
            string prefer = "";
            if (level >= 2 && tier >= 3) prefer = "Brute";
            else if (level >= 2 && tier >= 2) prefer = "Runner";
            return new Curve { Tension = tension, Interval = interval, ExtraKills = extra, Prefer = prefer };
        }

        public static int Batch(int state, int difficulty)
        {
            int level = Resolve(difficulty);
            if (state == 2) return level == 3 ? 6 : level == 1 ? 3 : 4;
            if (state == 1) return level == 3 ? 3 : level == 1 ? 1 : 2;
            if (state == 3) return 1;
            return 0;
        }
    }
}
