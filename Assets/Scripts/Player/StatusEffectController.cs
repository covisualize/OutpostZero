using UnityEngine;
using OutpostZero.Combat;
using OutpostZero.Core;

namespace OutpostZero.Player
{
    public class StatusEffectController : MonoBehaviour
    {
        [SerializeField] private float poisonRemaining;
        [SerializeField] private float bleedRemaining;
        [SerializeField] private float infection;
        [SerializeField] private float slowRemaining;
        [SerializeField] private float knockdownRemaining;

        private HealthSystem health;
        private float tick;

        public bool IsPoisoned => poisonRemaining > 0f;
        public bool IsBleeding => bleedRemaining > 0f;
        public bool IsInfected => infection >= 25f;
        public float Infection => infection;
        public bool IsKnockedDown => knockdownRemaining > 0f;
        public float SlowMultiplier => slowRemaining > 0f ? 0.55f : 1f;

        private void Awake()
        {
            health = GetComponent<HealthSystem>();
        }

        public void ApplyPoison(float seconds)
        {
            poisonRemaining = Mathf.Max(poisonRemaining, seconds);
            GameplayFeedback.Toast("Poisoned");
        }

        public void ApplyBleed(float seconds)
        {
            bleedRemaining = Mathf.Max(bleedRemaining, seconds);
        }

        public void ApplyInfection(float amount)
        {
            infection = Mathf.Clamp(infection + amount, 0f, 100f);
        }

        public void ApplySlow(float seconds)
        {
            slowRemaining = Mathf.Max(slowRemaining, seconds);
        }

        public void Knockdown(float seconds)
        {
            knockdownRemaining = Mathf.Max(knockdownRemaining, seconds);
        }

        public void ClearInjury()
        {
            poisonRemaining = 0f;
            bleedRemaining = 0f;
            slowRemaining = 0f;
            knockdownRemaining = 0f;
            infection = Mathf.Max(0f, infection - 40f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (poisonRemaining > 0f) poisonRemaining -= dt;
            if (bleedRemaining > 0f) bleedRemaining -= dt;
            if (slowRemaining > 0f) slowRemaining -= dt;
            if (knockdownRemaining > 0f) knockdownRemaining -= dt;

            tick -= dt;
            if (tick > 0f || health == null || health.IsDead) return;
            tick = 1f;
            if (poisonRemaining > 0f) health.TakeDamage(4f, transform.position, Vector3.up, gameObject);
            if (bleedRemaining > 0f) health.TakeDamage(3f, transform.position, Vector3.up, gameObject);
            if (infection >= 80f) health.TakeDamage(2f, transform.position, Vector3.up, gameObject);
        }
    }
}
