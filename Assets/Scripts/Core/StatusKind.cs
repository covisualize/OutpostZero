using System;
using UnityEngine;

namespace OutpostZero.Core
{
    /// <summary>The timed conditions a body can carry. Values are stored in assets, so new kinds go on the end.</summary>
    public enum StatusKind
    {
        None,
        Bleeding,
        Infected,
        Poisoned,
        Adrenaline,
        KnockedDown,
        Slowed
    }

    /// <summary>One condition a zombie's hit can leave: the kind, the odds per landed hit, and how long it lasts.</summary>
    [Serializable]
    public struct HitEffect
    {
        [Tooltip("Condition the hit leaves.")]
        public StatusKind kind;
        [Tooltip("Odds per landed hit, 0 to 1.")]
        [Range(0f, 1f)] public float chance;
        [Tooltip("Seconds it lasts. Bleeding runs until treated and infection runs on its own stage clock, so both ignore this.")]
        public float seconds;

        public HitEffect(StatusKind kind, float chance, float seconds)
        {
            this.kind = kind;
            this.chance = chance;
            this.seconds = seconds;
        }

        /// <summary>A roll in [0, 1) below the chance lands.</summary>
        public bool Lands(float roll)
        {
            if (kind == StatusKind.None) return false;
            if (roll < 0f) roll = 0f;
            return roll < chance;
        }
    }
}
