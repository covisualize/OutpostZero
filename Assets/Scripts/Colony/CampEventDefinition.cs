using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>One random camp event's weight and conditions, listed in <see cref="CampEventBook"/>. Its effect is in code keyed by id.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Camp Event", fileName = "CampEvent")]
    public class CampEventDefinition : ScriptableObject
    {
        [Tooltip("stranger, argument, generator, sickness, rain_fill, merchant, sighting or cache.")]
        public string id = "";

        [Tooltip("Relative chance among the events whose conditions hold.")]
        [Min(0)] public int weight = 1;

        [Tooltip("First day the event can happen.")]
        [Min(1)] public int minDay = 2;

        [Tooltip("Module that must be working: Generator, Water, or empty for none.")]
        public string module = "";

        public bool needsRain;

        [Tooltip("The roster has room for one more.")]
        public bool needsRoom;

        [Tooltip("Two living colonists think poorly of each other.")]
        public bool needsFeud;

        [Tooltip("A colonist other than the leader is uninjured.")]
        public bool needsHealthy;

        [Tooltip("No trader is at the gate today.")]
        public bool needsMerchantAway;

        public string titleKey = "";

        [TextArea] public string fallback = "";

        public CampEventRow ToRow()
        {
            return CampEventTable.Row(id, weight, minDay, module, needsRain, needsRoom, needsFeud, needsHealthy, needsMerchantAway, titleKey, fallback);
        }

        public void CopyFrom(CampEventRow row)
        {
            id = row.Id;
            weight = row.Weight;
            minDay = row.MinDay;
            module = row.Module;
            needsRain = row.NeedsRain;
            needsRoom = row.NeedsRoom;
            needsFeud = row.NeedsFeud;
            needsHealthy = row.NeedsHealthy;
            needsMerchantAway = row.NeedsMerchantAway;
            titleKey = row.Key;
            fallback = row.Fallback;
        }
    }
}
