using UnityEngine;

namespace OutpostZero.Core
{
    [CreateAssetMenu(fileName = "ZombieArchetype", menuName = "Outpost Zero/Zombie Archetype")]
    public class ZombieArchetype : ScriptableObject
    {
        public string id = "zombie";
        public string displayName = "Zombie";
        public string modelPath;
        public float wanderSpeed = 1.8f;
        public float chaseSpeed = 3.8f;
        public float maxHealth = 65f;
        public float armor;
        public float attackDamage = 18f;
        public float attackCooldown = 1.4f;
        public float attackRange = 1.6f;
        public float sightRange = 14f;
        public float sightAngle = 110f;
        public float hearingSensitivity = 1f;
        public float hordeAlertRadius = 10f;
        public ZombieSpecialAbility specialAbility = ZombieSpecialAbility.None;
    }
}
