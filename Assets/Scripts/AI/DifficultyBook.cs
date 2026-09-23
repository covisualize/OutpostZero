using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.AI
{
    /// <summary>The difficulty list the director loads from Resources. Tools > Outpost Zero > Sync Difficulty Book rebuilds it.</summary>
    public class DifficultyBook : ScriptableObject
    {
        public const string ResourcePath = "DifficultyBook";

        public DifficultyDefinition[] levels = new DifficultyDefinition[0];

        private static bool loaded;

        public static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Use(Resources.Load<DifficultyBook>(ResourcePath));
        }

        public static void Use(DifficultyBook book)
        {
            var rows = new List<DifficultyTable.Row>();
            if (book != null && book.levels != null)
            {
                foreach (var level in book.levels)
                    if (level != null) rows.Add(level.ToRow());
            }
            DifficultyTable.Use(rows);
        }
    }
}
