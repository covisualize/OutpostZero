using UnityEngine;

namespace OutpostZero.Combat
{
    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }

        void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, GameObject attacker);
        void Heal(float amount);
    }
}
