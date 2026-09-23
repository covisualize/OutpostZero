using System;
using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The factions the stall trades with. Empty until <see cref="FactionBook.Ensure"/> fills it from
    /// Resources/FactionBook; until then, and for any faction the book lacks, the code tables in
    /// <see cref="CaravanBook"/> answer. Only ids in <see cref="CaravanBook.Ids"/> are taken, since saves
    /// keep standing by that order.
    /// </summary>
    public static class FactionTable
    {
        public sealed class Row
        {
            public string Id;
            public string Label;
            public string[] Stock = Array.Empty<string>();
            public string Premium = "";
            public int RefuseBelow = CaravanBook.Never;
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
                if (row == null || CaravanBook.IndexOf(row.Id) < 0 || rows.ContainsKey(row.Id)) continue;
                if (row.Stock == null) row.Stock = Array.Empty<string>();
                if (row.Premium == null) row.Premium = "";
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

        /// <summary>The code tables as rows: what Sync Faction Book writes and what the committed assets must match.</summary>
        public static List<Row> BuiltInRows()
        {
            var list = new List<Row>();
            foreach (string id in CaravanBook.Ids)
            {
                list.Add(new Row
                {
                    Id = id,
                    Label = CaravanBook.CodeDisplay(id),
                    Stock = CaravanBook.CodeStock(id),
                    Premium = CaravanBook.CodePremium(id),
                    RefuseBelow = CaravanBook.CodeRefuseBelow(id)
                });
            }
            return list;
        }
    }
}
