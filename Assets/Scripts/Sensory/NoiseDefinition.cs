using UnityEngine;

namespace OutpostZero.Sensory
{
    /// <summary>One world noise's reach and loudness, keyed by the source id the code emits it under.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Noise", fileName = "Noise")]
    public class NoiseDefinition : ScriptableObject
    {
        [Tooltip("Source id the code emits this noise under (NoiseTable ids), also the asset name.")]
        public string id = "";
        [Tooltip("Metres the noise carries.")]
        [Min(0f)] public float radius;
        [Tooltip("0 to 1: how strongly a zombie inside the reach reacts.")]
        [Range(0f, 1f)] public float loudness;

        public NoiseTable.Row ToRow()
        {
            return new NoiseTable.Row { Id = id, Radius = radius, Loud = loudness };
        }

        public void CopyFrom(NoiseTable.Row row)
        {
            id = row.Id;
            radius = row.Radius;
            loudness = row.Loud;
        }
    }
}
