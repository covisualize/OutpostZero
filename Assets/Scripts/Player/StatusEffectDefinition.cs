using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Player
{
    /// <summary>One condition's numbers: the HUD label, damage over time, default length and movement scale.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Status Effect", fileName = "StatusEffect")]
    public class StatusEffectDefinition : ScriptableObject
    {
        [Tooltip("Which condition this row tunes. Only one asset per kind is read.")]
        public StatusKind kind = StatusKind.Bleeding;
        [Tooltip("Stable id, also the asset name.")]
        public string id = "";
        [Tooltip("String table key for the HUD pill.")]
        public string labelKey = "";
        [Tooltip("Health lost each second while it lasts.")]
        [Min(0f)] public float damagePerSecond;
        [Tooltip("Seconds a fresh case lasts when its source names none. Below 0 lasts until treated.")]
        public float seconds;
        [Tooltip("Movement multiplier while it lasts (the sprint bonus for adrenaline). 1 leaves speed alone.")]
        [Min(0f)] public float scale = 1f;

        public StatusTable.Row ToRow()
        {
            return new StatusTable.Row
            {
                Kind = kind,
                Id = id,
                LabelKey = labelKey,
                DamagePerSecond = damagePerSecond,
                Seconds = seconds,
                Scale = scale
            };
        }

        public void CopyFrom(StatusTable.Row row)
        {
            kind = row.Kind;
            id = row.Id;
            labelKey = row.LabelKey;
            damagePerSecond = row.DamagePerSecond;
            seconds = row.Seconds;
            scale = row.Scale;
        }
    }
}
