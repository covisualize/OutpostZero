using System;
using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The factions the stall trades with. Empty until <see cref="FactionBook.Ensure"/> fills it from
    /// Resources/FactionBook; until then, and for any faction the book lacks, the code tables in
    /// <see cref="CaravanBook"/> answer. A book row with a new id adds a faction after the built-in four
    /// (<see cref="Roster"/>); saves keep standing by id, so the order can grow without breaking one.
    /// </summary>
    public static class FactionTable
    {
        private static string[] roster = CaravanBook.BuiltInIds;

        /// <summary>Every faction id: the built-in four in their order, then the book's new ids in book order.</summary>
        public static string[] Roster => roster;

        /// <summary>A faction id packs into a save as <c>id=value,</c>, so it keeps to lower-case letters, digits and underscores.</summary>
        public static bool ValidId(string id)
        {
            if (string.IsNullOrEmpty(id) || id.Length > 32) return false;
            for (int i = 0; i < id.Length; i++)
            {
                char c = id[i];
                if (!(c >= 'a' && c <= 'z') && !(c >= '0' && c <= '9') && c != '_') return false;
            }
            return true;
        }

        public sealed class Row
        {
            public string Id;
            public string Label;
            public string[] Stock = Array.Empty<string>();
            public string Premium = "";
            public int RefuseBelow = CaravanBook.Never;
            public int Markup = 100;
        }

        private static readonly Dictionary<string, Row> rows = new Dictionary<string, Row>();

        public static bool FromAsset => rows.Count > 0;

        public static void Use(IList<Row> list)
        {
            Clear();
            if (list == null) return;
            var ids = new List<string>(CaravanBook.BuiltInIds);
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || !ValidId(row.Id) || rows.ContainsKey(row.Id)) continue;
                if (row.Stock == null) row.Stock = Array.Empty<string>();
                if (row.Premium == null) row.Premium = "";
                if (string.IsNullOrEmpty(row.Label)) row.Label = row.Id;
                row.Markup = Math.Max(CaravanBook.MarkupFloor, Math.Min(CaravanBook.MarkupCeiling, row.Markup));
                rows[row.Id] = row;
                if (!ids.Contains(row.Id)) ids.Add(row.Id);
            }
            roster = ids.ToArray();
        }

        public static void Clear()
        {
            rows.Clear();
            roster = CaravanBook.BuiltInIds;
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
            foreach (string id in CaravanBook.BuiltInIds)
            {
                list.Add(new Row
                {
                    Id = id,
                    Label = CaravanBook.CodeDisplay(id),
                    Stock = CaravanBook.CodeStock(id),
                    Premium = CaravanBook.CodePremium(id),
                    RefuseBelow = CaravanBook.CodeRefuseBelow(id),
                    Markup = CaravanBook.CodeMarkup(id)
                });
            }
            return list;
        }
    }
}
