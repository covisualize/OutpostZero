using UnityEngine;

namespace OutpostZero.Expedition
{
    /// <summary>One composable expedition objective, listed by an <see cref="ExpeditionDefinition"/>.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Objective", fileName = "Objective")]
    public class ObjectiveDefinition : ScriptableObject
    {
        [Tooltip("Stable id, unique within its expedition.")]
        public string id = "";

        public ObjectiveKind kind = ObjectiveKind.Collect;

        [Tooltip("Collect: an item category such as Medical. Retrieve: a key item id or \"poi\". Reach: a spot name. Rescue: a survivor id. ClearNest: \"nest\". Empty matches any.")]
        public string target = "";

        [Min(1)] public int count = 1;

        [Tooltip("Bonus objectives pay their reward but never hold up extraction.")]
        public bool bonus = true;

        [Tooltip("Scrap paid into camp stores when the leader extracts with this objective done.")]
        [Min(0)] public int reward;

        [Tooltip("String-table key for the objective line.")]
        public string titleKey = "";

        [Tooltip("English line used when the string table lacks the key.")]
        public string fallback = "";

        public ObjectiveSpec ToSpec()
        {
            return new ObjectiveSpec(id, kind, target, count, bonus, reward, titleKey, fallback);
        }

        public void CopyFrom(ObjectiveSpec spec)
        {
            id = spec.Id;
            kind = spec.Kind;
            target = spec.Target;
            count = spec.Count;
            bonus = spec.Bonus;
            reward = spec.Reward;
            titleKey = spec.Key;
            fallback = spec.Fallback;
        }
    }
}
