using System;

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
            var row = DifficultyTable.Of(difficulty);
            if (tier < 1) tier = 1;
            if (day < 1) day = 1;
            float tension = (tier - 1) * row.TensionPerTier + day * row.TensionPerDay;
            int extra = row.ExtraKillBase + row.ExtraKillPerTier * tier;
            if (extra < 0) extra = 0;
            string prefer = "";
            if (row.BruteTier > 0 && tier >= row.BruteTier) prefer = "Brute";
            else if (row.RunnerTier > 0 && tier >= row.RunnerTier) prefer = "Runner";
            return new Curve { Tension = tension, Interval = row.IntervalScale, ExtraKills = extra, Prefer = prefer };
        }

        public static int Batch(int state, int difficulty)
        {
            var row = DifficultyTable.Of(difficulty);
            if (state == 2) return row.PeakBatch;
            if (state == 1) return row.BuildUpBatch;
            if (state == 3) return row.RelaxBatch;
            return 0;
        }

        /// <summary>The difficulty of the run in play, set when an expedition opens and when a save loads.</summary>
        public static int Active { get; set; } = 2;

        /// <summary>The quality tier's crowd scaled by the difficulty's alive share, never under 4.</summary>
        public static int AliveCap(int quality, int difficulty)
        {
            int cap = AliveCap(quality);
            float scaled = cap * DifficultyTable.Of(difficulty).AliveScale;
            int result = (int)Math.Round(scaled, MidpointRounding.AwayFromZero);
            return result < 4 ? 4 : result > cap ? cap : result;
        }

        public static float Cooldown(float seconds, int difficulty)
        {
            float scale = DifficultyTable.Of(difficulty).CooldownScale;
            return seconds * (scale <= 0f ? 1f : scale);
        }
    }
}
