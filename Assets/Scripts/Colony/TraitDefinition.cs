using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>One survivor trait: its name, the skills it starts with, what it can't share a survivor with, and its numbers.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Trait", fileName = "Trait")]
    public class TraitDefinition : ScriptableObject
    {
        [Tooltip("Trait id kept in saves and read by the trait hooks, such as Brave or Night Owl.")]
        public string id = "";
        [Tooltip("String table key for the trait's name, such as trait.brave.")]
        public string nameKey = "";

        [Header("Starting skills (0 to 10)")]
        [Range(0, 10)] public int combat;
        [Range(0, 10)] public int medicine;
        [Range(0, 10)] public int engineering;
        [Range(0, 10)] public int cooking;
        [Range(0, 10)] public int scavenge;

        [Header("Draw")]
        [Tooltip("Trait ids a survivor with this trait never also draws. Either side listing the other is enough.")]
        public string[] clashes = new string[0];

        [Header("Numbers")]
        [Tooltip("Hunger lost per day, 0 to 100 scale. The plain rate is 18.")]
        public float hunger = TraitHook.PlainHunger;
        [Tooltip("Multiplier on the leader's shot spread. Below 1 is tighter.")]
        [Range(0.5f, 1.5f)] public float aim = 1f;
        [Tooltip("Morale a guard shift costs. The plain cost is 2.")]
        [Range(0, 20)] public int watchCost = TraitHook.PlainWatch;
        [Tooltip("Whether a guard shift adds camp security.")]
        public bool watchPays = true;
        [Tooltip("Morale taken off a night's rest, never below 1 left.")]
        [Range(0, 10)] public int restCut;
        [Tooltip("Extra morale when this survivor cooks a real meal.")]
        [Range(0, 10)] public int cookPlate;
        [Tooltip("Seconds each survivor with this trait adds to a raid warning a watchtower already gives.")]
        [Range(0f, 10f)] public float warn;
        [Tooltip("Extra scrap a scavenging shift brings.")]
        [Range(0, 10)] public int haul;

        public TraitTable.Row ToRow()
        {
            return new TraitTable.Row
            {
                Id = id,
                Key = nameKey ?? "",
                Combat = combat,
                Medicine = medicine,
                Engineering = engineering,
                Cooking = cooking,
                Scavenge = scavenge,
                Clashes = clashes != null ? (string[])clashes.Clone() : new string[0],
                Hunger = hunger,
                Aim = aim,
                WatchCost = watchCost,
                WatchPays = watchPays,
                RestCut = restCut,
                CookPlate = cookPlate,
                Warn = warn,
                Haul = haul
            };
        }

        public void CopyFrom(TraitTable.Row row)
        {
            id = row.Id;
            nameKey = row.Key ?? "";
            combat = row.Combat;
            medicine = row.Medicine;
            engineering = row.Engineering;
            cooking = row.Cooking;
            scavenge = row.Scavenge;
            clashes = row.Clashes != null ? (string[])row.Clashes.Clone() : new string[0];
            hunger = row.Hunger;
            aim = row.Aim;
            watchCost = row.WatchCost;
            watchPays = row.WatchPays;
            restCut = row.RestCut;
            cookPlate = row.CookPlate;
            warn = row.Warn;
            haul = row.Haul;
        }
    }
}
