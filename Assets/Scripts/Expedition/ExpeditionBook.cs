using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// Every district's expedition, loaded from Resources. Tools > Outpost Zero > Sync Expedition Book rebuilds it.
    /// </summary>
    public class ExpeditionBook : ScriptableObject
    {
        public const string ResourcePath = "ExpeditionBook";

        public ExpeditionDefinition[] expeditions = new ExpeditionDefinition[0];

        private static bool loaded;

        public static void Ensure()
        {
            if (loaded) return;
            loaded = true;
            Use(Resources.Load<ExpeditionBook>(ResourcePath));
        }

        public static void Use(ExpeditionBook book)
        {
            if (book == null)
            {
                ObjectivePlan.Clear();
                StreetTerms.Clear();
                return;
            }
            var list = new List<KeyValuePair<string, ObjectiveSpec[]>>();
            var terms = new List<KeyValuePair<string, StreetTerms.Terms>>();
            if (book.expeditions != null)
                foreach (var expedition in book.expeditions)
                {
                    if (expedition == null) continue;
                    list.Add(new KeyValuePair<string, ObjectiveSpec[]>(expedition.district, expedition.ToSpecs()));
                    terms.Add(new KeyValuePair<string, StreetTerms.Terms>(expedition.district, expedition.ToTerms()));
                }
            ObjectivePlan.Use(list);
            StreetTerms.Use(terms);
        }
    }
}
