using UnityEngine;

namespace OutpostZero.AI
{
    /// <summary>One difficulty: how the street's tension climbs, how hard the director spawns, which zombies come, and how scarce the loot is.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Difficulty", fileName = "Difficulty")]
    public class DifficultyDefinition : ScriptableObject
    {
        [Tooltip("1 Scavenger, 2 Survivor, 3 Nightmare. Saves store this number.")]
        [Range(1, 3)] public int level = 2;
        public string displayName = "";

        [Header("Tension")]
        [Tooltip("Opening tension added per threat tier above the first.")]
        public float tensionPerTier;
        [Tooltip("Opening tension added per campaign day.")]
        public float tensionPerDay;
        [Tooltip("Multiplies the district's spawn interval; below 1 spawns faster.")]
        [Min(0.1f)] public float intervalScale = 1f;

        [Header("Kill goal")]
        [Tooltip("Extra kills on the goal: base + per tier x threat tier, never below 0.")]
        public int extraKillBase;
        public int extraKillPerTier;

        [Header("Spawns")]
        [Tooltip("Threat tier from which runners are favoured; 0 never.")]
        [Min(0)] public int runnerTier;
        [Tooltip("Threat tier from which brutes are favoured; 0 never.")]
        [Min(0)] public int bruteTier;
        [Tooltip("Zombies per director spawn in Build-up, Peak and Relax.")]
        [Min(0)] public int buildUpBatch = 2;
        [Min(0)] public int peakBatch = 4;
        [Min(0)] public int relaxBatch = 1;
        [Tooltip("Share of the quality tier's crowd that may be alive at once.")]
        [Range(0.1f, 1f)] public float aliveScale = 1f;
        [Tooltip("Relative odds of a walker, runner and brute on an ordinary spawn.")]
        [Min(0)] public int walkerWeight = 1;
        [Min(0)] public int runnerWeight = 1;
        [Min(0)] public int bruteWeight = 1;
        [Tooltip("Multiplies the runner lunge and brute charge cooldown; below 1 they come back sooner.")]
        [Min(0.1f)] public float cooldownScale = 1f;

        [Header("Loot")]
        [Tooltip("Multiplies every container's rolled counts; the fraction is a seeded chance of one more.")]
        [Min(0f)] public float lootScale = 1f;

        public DifficultyTable.Row ToRow()
        {
            return new DifficultyTable.Row
            {
                Level = level,
                Name = displayName,
                TensionPerTier = tensionPerTier,
                TensionPerDay = tensionPerDay,
                IntervalScale = intervalScale,
                ExtraKillBase = extraKillBase,
                ExtraKillPerTier = extraKillPerTier,
                RunnerTier = runnerTier,
                BruteTier = bruteTier,
                BuildUpBatch = buildUpBatch,
                PeakBatch = peakBatch,
                RelaxBatch = relaxBatch,
                AliveScale = aliveScale,
                WalkerWeight = walkerWeight,
                RunnerWeight = runnerWeight,
                BruteWeight = bruteWeight,
                CooldownScale = cooldownScale,
                LootScale = lootScale
            };
        }

        public void CopyFrom(DifficultyTable.Row row)
        {
            level = row.Level;
            displayName = row.Name;
            tensionPerTier = row.TensionPerTier;
            tensionPerDay = row.TensionPerDay;
            intervalScale = row.IntervalScale;
            extraKillBase = row.ExtraKillBase;
            extraKillPerTier = row.ExtraKillPerTier;
            runnerTier = row.RunnerTier;
            bruteTier = row.BruteTier;
            buildUpBatch = row.BuildUpBatch;
            peakBatch = row.PeakBatch;
            relaxBatch = row.RelaxBatch;
            aliveScale = row.AliveScale;
            walkerWeight = row.WalkerWeight;
            runnerWeight = row.RunnerWeight;
            bruteWeight = row.BruteWeight;
            cooldownScale = row.CooldownScale;
            lootScale = row.LootScale;
        }
    }
}
