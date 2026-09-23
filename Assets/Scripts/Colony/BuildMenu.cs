namespace OutpostZero.Colony
{
    /// <summary>
    /// The camp build row as three tabs: walls and traps, the places people live, and the works that
    /// make things. Every module kind sits on exactly one tab.
    /// </summary>
    public static class BuildMenu
    {
        public enum Tab
        {
            Defence,
            Living,
            Works
        }

        public const int TabCount = 3;

        private static readonly ModuleKind[] Defence = { ModuleKind.Barricade, ModuleKind.Watchtower, ModuleKind.Turret, ModuleKind.Spikes, ModuleKind.Oil, ModuleKind.Lamp };
        private static readonly ModuleKind[] Living = { ModuleKind.Cot, ModuleKind.Campfire, ModuleKind.Crate, ModuleKind.TradingPost, ModuleKind.Memorial };
        private static readonly ModuleKind[] Works = { ModuleKind.Generator, ModuleKind.Water, ModuleKind.Purifier, ModuleKind.Farm, ModuleKind.Workbench };

        public static ModuleKind[] Kinds(Tab tab)
        {
            switch (tab)
            {
                case Tab.Living: return Living;
                case Tab.Works: return Works;
                default: return Defence;
            }
        }

        public static Tab TabOf(ModuleKind kind)
        {
            for (int t = 0; t < TabCount; t++)
                if (System.Array.IndexOf(Kinds((Tab)t), kind) >= 0) return (Tab)t;
            return Tab.Defence;
        }

        public static string TabKey(Tab tab) => "build.tab." + tab.ToString().ToLowerInvariant();

        public static string LabelKey(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.Watchtower: return "camp.tower";
                case ModuleKind.Workbench: return "camp.bench";
                case ModuleKind.TradingPost: return "camp.post";
                case ModuleKind.Campfire: return "camp.fire";
                default: return "camp." + kind.ToString().ToLowerInvariant();
            }
        }

        public static bool Affordable(int cost, int scrap) => scrap >= cost;
    }
}
