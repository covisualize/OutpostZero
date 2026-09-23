using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Combat
{
    /// <summary>The mod list guns load from Resources. Tools > Outpost Zero > Sync Weapon Mod Book rebuilds it.</summary>
    public class WeaponModBook : ScriptableObject
    {
        public const string ResourcePath = "WeaponModBook";

        public WeaponModDefinition[] mods = new WeaponModDefinition[0];

        private static bool loaded;

        public static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Use(Resources.Load<WeaponModBook>(ResourcePath));
        }

        public static void Use(WeaponModBook book)
        {
            var rows = new List<WeaponModTable.Row>();
            if (book != null && book.mods != null)
            {
                foreach (var mod in book.mods)
                    if (mod != null) rows.Add(mod.ToRow());
            }
            WeaponModTable.Use(rows);
        }
    }
}
