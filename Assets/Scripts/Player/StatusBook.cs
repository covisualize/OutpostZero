using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Player
{
    /// <summary>The condition list the status controller loads from Resources. Tools > Outpost Zero > Sync Status Book rebuilds it.</summary>
    public class StatusBook : ScriptableObject
    {
        public const string ResourcePath = "StatusBook";

        public StatusEffectDefinition[] effects = new StatusEffectDefinition[0];

        private static bool loaded;

        public static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Use(Resources.Load<StatusBook>(ResourcePath));
        }

        public static void Use(StatusBook book)
        {
            var rows = new List<StatusTable.Row>();
            if (book != null && book.effects != null)
            {
                foreach (var effect in book.effects)
                    if (effect != null) rows.Add(effect.ToRow());
            }
            StatusTable.Use(rows);
        }
    }
}
