using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>The faction list the stall loads from Resources. Tools > Outpost Zero > Sync Faction Book rebuilds it.</summary>
    public class FactionBook : ScriptableObject
    {
        public const string ResourcePath = "FactionBook";

        public FactionDefinition[] factions = new FactionDefinition[0];

        private static bool loaded;

        public static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Use(Resources.Load<FactionBook>(ResourcePath));
        }

        public static void Use(FactionBook book)
        {
            var rows = new List<FactionTable.Row>();
            if (book != null && book.factions != null)
            {
                foreach (var faction in book.factions)
                    if (faction != null && !string.IsNullOrEmpty(faction.id)) rows.Add(faction.ToRow());
            }
            FactionTable.Use(rows);
        }
    }
}
