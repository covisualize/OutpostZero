using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The random camp events, loaded from Resources. Tools > Outpost Zero > Sync Camp Event Book rebuilds it.
    /// </summary>
    public class CampEventBook : ScriptableObject
    {
        public const string ResourcePath = "CampEventBook";

        public CampEventDefinition[] events = new CampEventDefinition[0];

        private static bool loaded;

        public static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Use(Resources.Load<CampEventBook>(ResourcePath));
        }

        public static void Use(CampEventBook book)
        {
            if (book == null)
            {
                CampEventTable.Clear();
                return;
            }
            var rows = new List<CampEventRow>();
            if (book.events != null)
                foreach (var definition in book.events)
                    if (definition != null) rows.Add(definition.ToRow());
            CampEventTable.Use(rows);
        }
    }
}
