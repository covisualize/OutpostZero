using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Graphics;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The build modules the yard raises. Empty until <see cref="ModuleBook.Ensure"/> fills it from
    /// Resources/ModuleBook; until then, and for any kind the book lacks, the code tables in
    /// <see cref="GridBuilder"/> and <see cref="BuildSite"/> answer.
    /// </summary>
    public static class ModuleTable
    {
        public sealed class Row
        {
            public string Id;
            public int Scrap;
            public int Cloth;
            public int Chemicals;
            public int Tape;
            public int Hours = 1;
            public Vector3 Size = Vector3.one;
            public bool Wears;
            public SurfaceFamily Family;
            public Color Tint = Color.grey;
            public int Power;
        }

        private static readonly Dictionary<string, Row> rows = new Dictionary<string, Row>();

        public static bool FromAsset => rows.Count > 0;

        public static void Use(IList<Row> list)
        {
            Clear();
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || string.IsNullOrEmpty(row.Id) || rows.ContainsKey(row.Id)) continue;
                rows[row.Id] = row;
            }
        }

        public static void Clear()
        {
            rows.Clear();
        }

        public static bool TryRow(string id, out Row row)
        {
            row = null;
            return !string.IsNullOrEmpty(id) && rows.TryGetValue(id, out row);
        }

        /// <summary>The code tables as rows: what Sync Module Book writes and what the committed assets must match.</summary>
        public static List<Row> BuiltInRows()
        {
            var list = new List<Row>();
            foreach (ModuleKind kind in System.Enum.GetValues(typeof(ModuleKind)))
            {
                string id = kind.ToString();
                GridBuilder.CodeSupplies(kind, out int cloth, out int chemicals, out int tape);
                list.Add(new Row
                {
                    Id = id,
                    Scrap = GridBuilder.CodeCost(kind),
                    Cloth = cloth,
                    Chemicals = chemicals,
                    Tape = tape,
                    Hours = BuildSite.CodeNeed(id),
                    Size = GridBuilder.CodeSize(id),
                    Wears = GridBuilder.CodeWears(id),
                    Family = GridBuilder.CodeFamily(id),
                    Tint = GridBuilder.CodeColor(id),
                    Power = PowerGrid.CodePower(id)
                });
            }
            return list;
        }
    }
}
