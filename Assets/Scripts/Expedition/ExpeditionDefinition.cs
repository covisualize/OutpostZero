using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Expedition
{
    /// <summary>The objectives an expedition to one district carries.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Expedition", fileName = "Expedition")]
    public class ExpeditionDefinition : ScriptableObject
    {
        [Tooltip("Campaign district id, such as ash_market.")]
        public string district = "";

        public ObjectiveDefinition[] objectives = new ObjectiveDefinition[0];

        public ObjectiveSpec[] ToSpecs()
        {
            var specs = new List<ObjectiveSpec>();
            if (objectives != null)
                foreach (var objective in objectives)
                    if (objective != null && !string.IsNullOrEmpty(objective.id)) specs.Add(objective.ToSpec());
            return specs.ToArray();
        }
    }
}
