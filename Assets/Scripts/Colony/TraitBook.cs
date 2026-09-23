using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>The trait list the roster loads from Resources, in draw order. Tools > Outpost Zero > Sync Trait Book rebuilds it.</summary>
    public class TraitBook : ScriptableObject
    {
        public const string ResourcePath = "TraitBook";

        public TraitDefinition[] traits = new TraitDefinition[0];

        private static bool loaded;

        public static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Use(Resources.Load<TraitBook>(ResourcePath));
        }

        public static void Use(TraitBook book)
        {
            var rows = new List<TraitTable.Row>();
            if (book != null && book.traits != null)
            {
                foreach (var trait in book.traits)
                    if (trait != null && !string.IsNullOrEmpty(trait.id)) rows.Add(trait.ToRow());
            }
            TraitTable.Use(rows);
        }
    }
}
