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
        [SerializeField] private bool verboseLogging = false;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public bool Shielded;
        public Vector3 LastHitDirection { get; private set; }
        public float LastHitTime { get; private set; }

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
            if (isDead || Shielded) return;
            if (hitDirection.sqrMagnitude > 0.0001f)
            {
                LastHitDirection = hitDirection;
                LastHitTime = Time.time;
            }

            // Apply armor mitigation (formula: net damage = amount * (100 / (100 + armor)))
            float netDamage = amount * (100f / (100f + Mathf.Max(0f, armorRating)));
            currentHealth = Mathf.Max(0f, currentHealth - netDamage);

#if OUTPOST_VERBOSE_COMBAT
            verboseLogging = true;
#endif
            if (verboseLogging)
            {
                Debug.Log($"[HealthSystem] {gameObject.name} took {netDamage:0.0} damage ({currentHealth:0.0} left).");
            }

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

            if (verboseLogging)
            {
                Debug.LogWarning($"[HealthSystem] {gameObject.name} died.");
            }

            OnDeath?.Invoke(hitPoint, hitDirection, attacker);
        }

        public void Configure(float max, float armor = 0f)
        {
            maxHealth = Mathf.Max(1f, max);
            currentHealth = maxHealth;
            armorRating = armor;
            isDead = false;
        }

        /// <summary>Puts a saved health value back on a living body. A save never holds a death, so zero or less is ignored.</summary>
        public void Restore(float value)
        {
            if (value <= 0f) return;
            isDead = false;
            currentHealth = Mathf.Min(maxHealth, value);
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void ResetHealth()
        {
            isDead = false;
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }
}
