using System.Collections.Generic;

namespace OutpostZero.Combat
{
    /// <summary>
    /// Each weapon mod's numbers as a row. Empty until <see cref="WeaponModBook.Ensure"/> fills it from
    /// Resources/WeaponModBook; until then, and for any id the book lacks, the built-in rows answer.
    /// Mods on one gun multiply their damage, noise and spread and add their magazine bonus.
    /// </summary>
    public static class WeaponModTable
    {
        public sealed class Row
        {
            public string Id = "";
            /// <summary>Damage multiplier.</summary>
            public float Damage = 1f;
            /// <summary>Multiplier on the gun's noise radius.</summary>
            public float Noise = 1f;
            /// <summary>Multiplier on the gun's spread.</summary>
            public float Spread = 1f;
            /// <summary>Rounds added to the magazine.</summary>
            public int MagazineBonus;
            /// <summary>Turns a loud report into a quiet one, so the spawner's loud-shot call never hears it.</summary>
            public bool Quiet;
        }

        private static readonly Dictionary<string, Row> rows = new Dictionary<string, Row>();
        private static List<Row> builtIn;
        private static readonly Row none = new Row { Id = "none" };

        public static bool FromAsset => rows.Count > 0;

        public static void Use(IList<Row> list)
        {
            rows.Clear();
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

        /// <summary>The row for a mod id; an unknown id changes nothing.</summary>
        public static Row Of(string id)
        {
            if (string.IsNullOrEmpty(id)) return none;
            if (rows.TryGetValue(id, out var row)) return row;
            var list = BuiltInRows();
            for (int i = 0; i < list.Count; i++) if (list[i].Id == id) return list[i];
            return none;
        }

        /// <summary>The code values as rows: what Sync Weapon Mod Book writes and what the committed assets must match.</summary>
        public static List<Row> BuiltInRows()
        {
            if (builtIn != null) return builtIn;
            builtIn = new List<Row>
            {
                new Row { Id = "suppressor", Damage = 0.9f, Noise = 0.35f, Spread = 0.85f, MagazineBonus = 0, Quiet = true },
                new Row { Id = "optic", Damage = 1f, Noise = 1f, Spread = 0.55f, MagazineBonus = 0 },
                new Row { Id = "extended_mag", Damage = 1f, Noise = 1f, Spread = 1f, MagazineBonus = 10 },
                new Row { Id = "rail", Damage = 1f, Noise = 1f, Spread = 1f, MagazineBonus = 0 }
            };
            return builtIn;
        }
    }
}
