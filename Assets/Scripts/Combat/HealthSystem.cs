using System;
using UnityEngine;

namespace OutpostZero.Combat
{
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private float armorRating = 0f; // Flat or percentage mitigation

        [Header("State")]
        [SerializeField] private bool isDead = false;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;

        public event Action<float, float> OnHealthChanged; // current, max
        public event Action<Vector3, Vector3, GameObject> OnDeath; // hitPoint, hitDir, attacker
        public event Action<float, Vector3> OnDamaged; // amount, hitPoint

        private void Awake()
        {
            if (currentHealth <= 0.001f && !isDead)
            {
                currentHealth = maxHealth;
            }
        }

        public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, GameObject attacker)
        {
            if (isDead) return;

            // Apply armor mitigation (formula: net damage = amount * (100 / (100 + armor)))
            float netDamage = amount * (100f / (100f + Mathf.Max(0f, armorRating)));
            currentHealth = Mathf.Max(0f, currentHealth - netDamage);

            Debug.Log($"[HealthSystem] {gameObject.name} TakeDamage by {(attacker != null ? attacker.name : "null")}: amount={amount}, netDamage={netDamage}, currentHealth={currentHealth}\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");

            OnDamaged?.Invoke(netDamage, hitPoint);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);

            if (currentHealth <= 0.001f)
            {
                Die(hitPoint, hitDirection, attacker);
            }
        }

        public void Heal(float amount)
        {
            if (isDead) return;

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die(Vector3 hitPoint, Vector3 hitDirection, GameObject attacker)
        {
            if (isDead) return;
            isDead = true;

            Debug.LogWarning($"[HealthSystem] {gameObject.name} Died! HitPoint: {hitPoint}, Attacker: {(attacker != null ? attacker.name : "null")}\n{UnityEngine.StackTraceUtility.ExtractStackTrace()}");

            OnDeath?.Invoke(hitPoint, hitDirection, attacker);
        }

        public void ResetHealth()
        {
            isDead = false;
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
