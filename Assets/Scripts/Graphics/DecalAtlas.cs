namespace OutpostZero.Graphics
{
    /// <summary>
    /// Cell layout of Textures/Decals/DecalAtlas.png, written by BlenderScripts/decal_atlas.py.
    /// Row 0 is the top of the PNG; UVs count from the bottom, so a cell's bias flips its row.
    /// </summary>
    public static class DecalAtlas
    {
        public const int Columns = 8;
        public const int Rows = 4;
        public const string MaterialPath = "Decals/DecalAtlas";

        public const string Blood = "blood";
        public const string Drip = "drip";
        public const string HoleConcrete = "hole_concrete";
        public const string HoleMetal = "hole_metal";
        public const string HoleWood = "hole_wood";
        public const string Scorch = "scorch";
        public const string Oil = "oil";
        public const string Footprint = "footprint";

        private static readonly string[] Kinds = { Blood, Drip, HoleConcrete, HoleMetal, HoleWood, Scorch, Oil, Footprint };
        private static readonly int[] Counts = { 8, 4, 3, 3, 3, 2, 2, 2 };

        public static int Total
        {
            get
            {
                int total = 0;
                for (int i = 0; i < Counts.Length; i++) total += Counts[i];
                return total;
            }
        }

        public static int Count(string kind)
        {
            for (int i = 0; i < Kinds.Length; i++)
                if (Kinds[i] == kind) return Counts[i];
            return 0;
        }

        public static int First(string kind)
        {
            int index = 0;
            for (int i = 0; i < Kinds.Length; i++)
            {
                if (Kinds[i] == kind) return index;
                index += Counts[i];
            }
            return -1;
        }

        /// <summary>A cell of <paramref name="kind"/> picked by <paramref name="seed"/>; -1 for an unknown kind.</summary>
        public static int Cell(string kind, int seed)
        {
            int count = Count(kind);
            if (count <= 0) return -1;
            int pick = seed % count;
            if (pick < 0) pick += count;
            return First(kind) + pick;
        }

        public static float ScaleU => 1f / Columns;
        public static float ScaleV => 1f / Rows;

        public static float BiasU(int cell)
        {
            return (cell % Columns) / (float)Columns;
        }

        public static float BiasV(int cell)
        {
            int row = cell / Columns;
            return (Rows - 1 - row) / (float)Rows;
        }

        /// <summary>Bullet hole for a StrikeFace surface: metal rings, wood splinters, concrete chips.</summary>
        public static string HoleFor(string face)
        {
            if (face == "metal") return HoleMetal;
            if (face == "wood") return HoleWood;
            return HoleConcrete;
        }
    }
}
