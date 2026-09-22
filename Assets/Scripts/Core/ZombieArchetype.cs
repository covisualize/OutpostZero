using UnityEngine;

namespace OutpostZero.Core
{
    /// <summary>
    /// Tuning for one zombie type: movement, health, attack, senses, and special ability.
    /// </summary>
    [CreateAssetMenu(fileName = "ZombieArchetype", menuName = "Outpost Zero/Zombie Archetype")]
    public class ZombieArchetype : ScriptableObject
    {
        /// <summary>Stable id used by the codex, kill feed and spawn tables.</summary>
        [Tooltip("Stable id used by the codex, kill feed and spawn tables.")]
        public string id = "zombie";
        /// <summary>Name shown in the codex and kill feed.</summary>
        [Tooltip("Name shown in the codex and kill feed.")]
        public string displayName = "Zombie";
        /// <summary>FBX under Assets/Models used for the body.</summary>
        [Tooltip("FBX under Assets/Models used for the body.")]
        public string modelPath;
        /// <summary>Metres per second while idle or wandering.</summary>
        [Tooltip("Metres per second while idle or wandering.")]
        public float wanderSpeed = 1.8f;
        /// <summary>Metres per second while chasing a target.</summary>
        [Tooltip("Metres per second while chasing a target.")]
        public float chaseSpeed = 3.8f;
        /// <summary>Hit points at spawn.</summary>
        [Tooltip("Hit points at spawn.")]
        public float maxHealth = 65f;
        /// <summary>Armor rating; damage is scaled by 100 / (100 + armor).</summary>
        [Tooltip("Armor rating; damage is scaled by 100 / (100 + armor).")]
        public float armor;
        /// <summary>Damage per claw or bite.</summary>
        [Tooltip("Damage per claw or bite.")]
        public float attackDamage = 18f;
        /// <summary>Seconds between attacks.</summary>
        [Tooltip("Seconds between attacks.")]
        public float attackCooldown = 1.4f;
        /// <summary>Metres from the target at which an attack can land.</summary>
        [Tooltip("Metres from the target at which an attack can land.")]
        public float attackRange = 1.6f;
        /// <summary>Metres the vision cone reaches.</summary>
        [Tooltip("Metres the vision cone reaches.")]
        public float sightRange = 14f;
        /// <summary>Full vision cone angle in degrees.</summary>
        [Tooltip("Full vision cone angle in degrees.")]
        public float sightAngle = 110f;
        /// <summary>Multiplier on how far noises reach this zombie.</summary>
        [Tooltip("Multiplier on how far noises reach this zombie.")]
        public float hearingSensitivity = 1f;
        /// <summary>Metres within which its alert call wakes other zombies.</summary>
        [Tooltip("Metres within which its alert call wakes other zombies.")]
        public float hordeAlertRadius = 10f;
        /// <summary>Runner lunge or brute charge, telegraphed by a wind-up.</summary>
        [Tooltip("Runner lunge or brute charge, telegraphed by a wind-up.")]
        public ZombieSpecialAbility specialAbility = ZombieSpecialAbility.None;
    }
}
