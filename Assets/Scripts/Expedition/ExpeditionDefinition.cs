using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Expedition
{
    /// <summary>One district's expedition: its objectives, how long the street lasts, its difficulty and where it extracts.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Expedition", fileName = "Expedition")]
    public class ExpeditionDefinition : ScriptableObject
    {
        [Tooltip("Campaign district id, such as ash_market.")]
        public string district = "";

        public ObjectiveDefinition[] objectives = new ObjectiveDefinition[0];

        [Tooltip("Seconds on the street before overtime, when tension holds at its peak. 0 has no limit.")]
        [Min(0f)] public float duration = StreetTerms.DefaultDuration;

        [Tooltip("1 Scavenger, 2 Survivor, 3 Nightmare for this expedition only. 0 uses the run's difficulty.")]
        [Range(0, 3)] public int difficulty;

        [Tooltip("Street positions (x, z) the extraction gate may stand on, one picked by the world seed. Empty keeps the seeded gate.")]
        public Vector2[] extractionPoints = new Vector2[0];

        public ObjectiveSpec[] ToSpecs()
        {
            var specs = new List<ObjectiveSpec>();
            if (objectives != null)
                foreach (var objective in objectives)
                    if (objective != null && !string.IsNullOrEmpty(objective.id)) specs.Add(objective.ToSpec());
            return specs.ToArray();
        }

        public StreetTerms.Terms ToTerms()
        {
            int count = extractionPoints != null ? extractionPoints.Length : 0;
            var terms = new StreetTerms.Terms
            {
                Duration = duration,
                Difficulty = difficulty,
                ExtractX = new float[count],
                ExtractZ = new float[count]
            };
            for (int i = 0; i < count; i++)
            {
                terms.ExtractX[i] = extractionPoints[i].x;
                terms.ExtractZ[i] = extractionPoints[i].y;
            }
            return terms;
        }
    }
}
