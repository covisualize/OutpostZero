namespace OutpostZero.Colony
{
    /// <summary>
    /// A finished generator runs at tier 1 until it is ordered and a builder puts in three hours, like the bench.
    /// The radio tower only broadcasts off a tier-2 generator.
    /// </summary>
    public static class GeneratorTune
    {
        public const int Scrap = 14;
        public const int Chemicals = 2;
        public const int Tape = 2;

        public static bool Raises(string kind) => kind == "Workbench" || kind == "Generator";

        public static int Tier(int tier, int job) => tier >= 2 || job >= CraftGate.Done ? 2 : 1;
    }
}
