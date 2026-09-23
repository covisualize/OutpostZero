using System;
using System.Collections.Generic;

namespace OutpostZero.AI
{
    /// <summary>
    /// The three difficulties as rows. Empty until <see cref="DifficultyBook.Ensure"/> fills it from
    /// Resources/DifficultyBook; until then, and for any level the book lacks, the built-in rows answer.
    /// </summary>
    public static class DifficultyTable
    {
        public sealed class Row
        {
            public int Level;
            public string Name = "";
            public float TensionPerTier;
            public float TensionPerDay;
            public float IntervalScale = 1f;
            public int ExtraKillBase;
            public int ExtraKillPerTier;
            /// <summary>Threat tier from which the director favours runners; 0 never.</summary>
            public int RunnerTier;
            /// <summary>Threat tier from which the director favours brutes; 0 never.</summary>
            public int BruteTier;
            public int BuildUpBatch;
            public int PeakBatch;
            public int RelaxBatch;
            /// <summary>Share of the quality tier's crowd that may be alive at once.</summary>
            public float AliveScale = 1f;
            public int WalkerWeight = 1;
            public int RunnerWeight = 1;
            public int BruteWeight = 1;
            /// <summary>Scales the runner lunge and brute charge cooldown.</summary>
            public float CooldownScale = 1f;
            /// <summary>Scales every container's rolled counts; below 1 is scarcer.</summary>
            public float LootScale = 1f;
        }

        private static readonly Dictionary<int, Row> rows = new Dictionary<int, Row>();
        private static List<Row> builtIn;

        public static bool FromAsset => rows.Count > 0;

        public static void Use(IList<Row> list)
        {
            rows.Clear();
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || row.Level < 1 || row.Level > 3 || rows.ContainsKey(row.Level)) continue;
                rows[row.Level] = row;
            }
        }

        public static void Clear()
        {
            rows.Clear();
        }

        public static Row Of(int stored)
        {
            int level = DifficultyProfile.Resolve(stored);
            if (rows.TryGetValue(level, out var row)) return row;
            return BuiltInRows()[level - 1];
        }

        /// <summary>The code tables as rows, Scavenger to Nightmare: what Sync Difficulty Book writes and what the committed assets must match.</summary>
        public static List<Row> BuiltInRows()
        {
            if (builtIn != null) return builtIn;
            builtIn = new List<Row>
            {
                new Row
                {
                    Level = 1, Name = "Scavenger", TensionPerTier = 2f, TensionPerDay = 0f, IntervalScale = 1.15f,
                    ExtraKillBase = 0, ExtraKillPerTier = 0, RunnerTier = 0, BruteTier = 0,
                    BuildUpBatch = 1, PeakBatch = 3, RelaxBatch = 1, AliveScale = 0.75f,
                    WalkerWeight = 50, RunnerWeight = 35, BruteWeight = 15, CooldownScale = 1.25f, LootScale = 1.25f
                },
                new Row
                {
                    Level = 2, Name = "Survivor", TensionPerTier = 4f, TensionPerDay = 0f, IntervalScale = 1f,
                    ExtraKillBase = -1, ExtraKillPerTier = 1, RunnerTier = 2, BruteTier = 3,
                    BuildUpBatch = 2, PeakBatch = 4, RelaxBatch = 1, AliveScale = 1f,
                    WalkerWeight = 34, RunnerWeight = 33, BruteWeight = 33, CooldownScale = 1f, LootScale = 1f
                },
                new Row
                {
                    Level = 3, Name = "Nightmare", TensionPerTier = 8f, TensionPerDay = 0.5f, IntervalScale = 0.75f,
                    ExtraKillBase = 1, ExtraKillPerTier = 1, RunnerTier = 2, BruteTier = 3,
                    BuildUpBatch = 3, PeakBatch = 6, RelaxBatch = 1, AliveScale = 1f,
                    WalkerWeight = 25, RunnerWeight = 40, BruteWeight = 35, CooldownScale = 0.8f, LootScale = 0.75f
                }
            };
            return builtIn;
        }

        public static int WeightOf(Row row, string variant)
        {
            if (row == null) return 1;
            if (variant != null && variant.IndexOf("Brute", StringComparison.Ordinal) >= 0) return Math.Max(0, row.BruteWeight);
            if (variant != null && variant.IndexOf("Runner", StringComparison.Ordinal) >= 0) return Math.Max(0, row.RunnerWeight);
            return Math.Max(0, row.WalkerWeight);
        }

        /// <summary>Index of the variant a roll in [0, 1) lands on, by weight; -1 when nothing weighs anything.</summary>
        public static int Pick(string[] variants, Row row, float roll)
        {
            if (variants == null || variants.Length == 0) return -1;
            int total = 0;
            for (int i = 0; i < variants.Length; i++) total += WeightOf(row, variants[i]);
            if (total <= 0) return -1;
            if (roll < 0f) roll = 0f;
            float mark = roll * total;
            int run = 0;
            for (int i = 0; i < variants.Length; i++)
            {
                run += WeightOf(row, variants[i]);
                if (mark < run) return i;
            }
            return variants.Length - 1;
        }

        /// <summary>
        /// A rolled count scaled by scarcity. The whole part is kept and the fraction is a
        /// seeded chance of one more, so a single bandage can vanish on Nightmare.
        /// </summary>
        public static int Scarce(int count, float scale, int salt)
        {
            if (count <= 0) return 0;
            if (scale < 0f) scale = 0f;
            double scaled = count * (double)scale;
            int whole = (int)Math.Floor(scaled);
            double part = scaled - whole;
            if (part <= 0.0001) return whole;
            uint hash = 2166136261u;
            unchecked
            {
                hash ^= (uint)salt;
                hash *= 16777619u;
                hash ^= hash >> 13;
                hash *= 0x5bd1e995u;
                hash ^= hash >> 15;
            }
            double chance = (hash % 10000u) / 10000.0;
            return chance < part ? whole + 1 : whole;
        }
    }
}
