using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// Committed scatter settings per district, tuned in Tools > Outpost Zero > Debris Scatterer.
    /// A district with no row uses <see cref="Default"/> and its own id hash as the seed.
    /// </summary>
    [CreateAssetMenu(fileName = "DebrisProfile", menuName = "Outpost Zero/Debris Profile")]
    public class DebrisProfile : ScriptableObject
    {
        public const string ResourcePath = "DebrisProfile";

        [Serializable]
        public class Row
        {
            public string district;
            public int seed;
            [Tooltip("Minimum metres between two pieces.")] public float spacing = 0.85f;
            [Tooltip("Chance a piece survives in the open road, 0 to 1.")] public float baseDensity = 0.12f;
            [Tooltip("Chance a piece survives against a wall or curb, 0 to 1.")] public float edgeDensity = 0.9f;
            [Tooltip("Metres over which wall and curb density falls off.")] public float reach = 1.6f;
        }

        public List<Row> rows = new List<Row>();

        private static DebrisProfile loaded;
        private static bool tried;

        public static DebrisProfile Active
        {
            get
            {
                if (!tried)
                {
                    tried = true;
                    loaded = Resources.Load<DebrisProfile>(ResourcePath);
                }
                return loaded;
            }
        }

        public static Row Default(string district)
        {
            return new Row { district = district, seed = DressingPlan.SeedFor(district) };
        }

        public Row Find(string district)
        {
            if (rows == null) return null;
            for (int i = 0; i < rows.Count; i++)
                if (rows[i] != null && rows[i].district == district) return rows[i];
            return null;
        }

        public static Row For(string district)
        {
            var row = Active != null ? Active.Find(district) : null;
            return row ?? Default(district);
        }
    }
}
