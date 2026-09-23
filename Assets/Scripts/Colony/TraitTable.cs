using System;
using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The survivor traits: their name key, starting skills, clashes and the numbers <see cref="TraitHook"/> reads.
    /// <see cref="TraitBook.Ensure"/> fills it from Resources/TraitBook. Until then, and for any trait the book lacks,
    /// the built-in rows answer. What a trait does (who it feuds with, who it keeps apart from) stays in code by id.
    /// Book order is draw order, so a seed opens the same camp only while the order holds.
    /// </summary>
    public static class TraitTable
    {
        public sealed class Row
        {
            public string Id;
            public string Key = "";
            public int Combat;
            public int Medicine;
            public int Engineering;
            public int Cooking;
            public int Scavenge;
            public string[] Clashes = Array.Empty<string>();
            public float Hunger = TraitHook.PlainHunger;
            public float Aim = 1f;
            public int WatchCost = TraitHook.PlainWatch;
            public bool WatchPays = true;
            public int RestCut;
            public int CookPlate;
            public float Warn;
            public int Haul;
        }

        private static readonly List<Row> ordered = new List<Row>();
        private static readonly Dictionary<string, Row> rows = new Dictionary<string, Row>();
        private static Dictionary<string, Row> builtIn;

        public static bool FromAsset => rows.Count > 0;

        public static void Use(IList<Row> list)
        {
            Clear();
            if (list == null) return;
            for (int i = 0; i < list.Count; i++)
            {
                var row = list[i];
                if (row == null || string.IsNullOrEmpty(row.Id) || rows.ContainsKey(row.Id)) continue;
                if (row.Clashes == null) row.Clashes = Array.Empty<string>();
                if (row.Key == null) row.Key = "";
                rows[row.Id] = row;
                ordered.Add(row);
            }
        }

        public static void Clear()
        {
            rows.Clear();
            ordered.Clear();
        }

        /// <summary>The book's row for a trait, the built-in row when the book lacks it, or null for no trait.</summary>
        public static Row For(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (rows.TryGetValue(id, out var row)) return row;
            if (builtIn == null)
            {
                builtIn = new Dictionary<string, Row>();
                foreach (var code in BuiltInRows()) builtIn[code.Id] = code;
            }
            return builtIn.TryGetValue(id, out row) ? row : null;
        }

        /// <summary>Trait ids in draw order: the book's when loaded, else the built-in order.</summary>
        public static string[] Ids()
        {
            var source = FromAsset ? ordered : BuiltInRows();
            var ids = new string[source.Count];
            for (int i = 0; i < ids.Length; i++) ids[i] = source[i].Id;
            return ids;
        }

        public static int Skill(string id, string kind)
        {
            var row = For(id);
            if (row == null) return 0;
            switch (kind)
            {
                case "combat": return row.Combat;
                case "medicine": return row.Medicine;
                case "engineering": return row.Engineering;
                case "cooking": return row.Cooking;
                case "scavenge": return row.Scavenge;
                default: return 0;
            }
        }

        /// <summary>Two traits clash when they are the same or when either one lists the other.</summary>
        public static bool Clashes(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
            if (a == b) return true;
            return Lists(For(a), b) || Lists(For(b), a);
        }

        private static bool Lists(Row row, string other)
        {
            if (row == null || row.Clashes == null) return false;
            return Array.IndexOf(row.Clashes, other) >= 0;
        }

        /// <summary>The code table as rows: what Sync Trait Book writes and what the committed assets must match.</summary>
        public static List<Row> BuiltInRows()
        {
            return new List<Row>
            {
                new Row { Id = "Steady Hands", Key = "trait.steady", Combat = 3, Engineering = 2 },
                new Row { Id = "Light Sleeper", Key = "trait.sleeper", Cooking = 1, Warn = TraitHook.Sleeper },
                new Row { Id = "Field Medic", Key = "trait.medic", Medicine = 4 },
                new Row { Id = "Scrounger", Key = "trait.scrounger", Scavenge = 3, Haul = 3 },
                new Row { Id = "Watchful", Key = "trait.watchful", Combat = 3, Warn = TraitHook.Watch },
                new Row { Id = "Volatile", Key = "trait.volatile" },
                new Row { Id = "Glutton", Key = "trait.glutton", Hunger = TraitHook.GluttonHunger },
                new Row { Id = "Engineer", Key = "trait.engineer", Engineering = 4 },
                new Row { Id = "Cook", Key = "trait.cook", Cooking = 4, CookPlate = 4 },
                new Row { Id = "Sharpshooter", Key = "trait.sharp", Combat = 4, Aim = 0.8f },
                new Row { Id = "Brave", Key = "trait.brave", Combat = 2, WatchCost = 0, Clashes = new[] { "Cowardly" } },
                new Row { Id = "Cowardly", Key = "trait.coward", WatchCost = 6, WatchPays = false },
                new Row { Id = "Insomniac", Key = "trait.insomniac", RestCut = 3, Clashes = new[] { "Light Sleeper" } },
                new Row { Id = "Optimist", Key = "trait.optimist", Clashes = new[] { "Volatile" } },
                new Row { Id = "Loner", Key = "trait.loner", Scavenge = 2 },
                new Row { Id = "Night Owl", Key = "trait.owl", Warn = TraitHook.Owl }
            };
        }
    }
}
