using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Player
{
    /// <summary>The throwable list the leader's hands load from Resources. Tools > Outpost Zero > Sync Throwable Book rebuilds it.</summary>
    public class ThrowableBook : ScriptableObject
    {
        public const string ResourcePath = "ThrowableBook";

        public ThrowableDefinition[] throwables = new ThrowableDefinition[0];

        private static bool loaded;

        public static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Use(Resources.Load<ThrowableBook>(ResourcePath));
        }

        public static void Use(ThrowableBook book)
        {
            var rows = new List<ThrowableTable.Row>();
            if (book != null && book.throwables != null)
            {
                foreach (var throwable in book.throwables)
                    if (throwable != null) rows.Add(throwable.ToRow());
            }
            ThrowableTable.Use(rows);
        }
    }
}
