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
        [SerializeField] private float painRemaining;
        [SerializeField] private float adrenaline;

        private HealthSystem health;
        private float tick;
        private float lastDrip;

        public bool IsPoisoned => poisonRemaining > 0f;
        public bool IsBleeding => bleedRemaining != 0f;
        public bool IsInfected => InfectionStage >= 1;
        public float Infection => infection;
        public int InfectionStage => Affliction.Stage(infection);
        public bool IsKnockedDown => knockdownRemaining > 0f;
        public float SprintBonus => adrenaline > 0f ? Affliction.AdrenalineSprint : 1f;
        public float SlowMultiplier
        {
            get
            {
                float slow = slowRemaining > 0f ? 0.55f : 1f;
                if (InfectionStage >= 2) slow *= 0.85f;
                return slow;
            }
        }

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
            bleedRemaining = -1f;
        }

        public void StopBleed()
        {
            bleedRemaining = 0f;
            lastDrip = 0f;
        }

        public void ApplyInfection(float amount)
        {
            infection = Affliction.Bite(infection);
            GameplayFeedback.Toast(Affliction.Label(InfectionStage));
        }

        public bool CureInfection()
        {
            if (!Affliction.AntibioticsWork(InfectionStage)) return false;
            infection = 0f;
            return true;
        }

        public void ApplyPainkiller()
        {
            painRemaining = Affliction.PainSpan;
        }

        public void ApplyAdrenaline(float seconds)
        {
            if (seconds <= 0f) return;
            adrenaline = Mathf.Max(adrenaline, seconds);
        }

        public void ApplySlow(float seconds)
        {
            slowRemaining = Mathf.Max(slowRemaining, seconds);
        }

        public void Knockdown(float seconds)
        {
            knockdownRemaining = Mathf.Max(knockdownRemaining, seconds);
        }

        public void RestoreCondition(bool bleeding, float infectionSeconds)
        {
            bleedRemaining = bleeding ? -1f : 0f;
            infection = infectionSeconds < 0.05f ? 0f : infectionSeconds;
        }

        public void ClearInjury()
        {
            poisonRemaining = 0f;
            bleedRemaining = 0f;
            slowRemaining = 0f;
            knockdownRemaining = 0f;
            painRemaining = 0f;
            adrenaline = 0f;
            infection = 0f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (poisonRemaining > 0f) poisonRemaining -= dt;
            if (bleedRemaining > 0f) bleedRemaining -= dt;
            if (slowRemaining > 0f) slowRemaining -= dt;
            if (knockdownRemaining > 0f) knockdownRemaining -= dt;
            if (adrenaline > 0f) adrenaline -= dt;
            if (infection > 0.01f) infection = Affliction.Advance(infection, dt);
            if (painRemaining > 0f && health != null && !health.IsDead)
            {
                float step = dt < painRemaining ? dt : painRemaining;
                painRemaining -= step;
                health.Heal(Affliction.PainHeal(step));
            }

            int gore = SettingsService.Instance != null ? SettingsService.Instance.Gore : 1;
            if (health != null && !health.IsDead && IsBleeding && WoundShow.Bleeds(gore) && WoundShow.DripDue(Time.time, lastDrip))
            {
                lastDrip = Time.time;
                CombatVfx.Drip(transform.position);
            }
            else if (!IsBleeding)
            {
                lastDrip = 0f;
            }

            tick -= dt;
            if (tick > 0f || health == null || health.IsDead) return;
            tick = 1f;
            if (poisonRemaining > 0f) health.TakeDamage(4f, transform.position, Vector3.zero, gameObject);
            if (IsBleeding) health.TakeDamage(Affliction.BleedPerSecond, transform.position, Vector3.zero, gameObject);
            if (Affliction.Fatal(infection)) health.TakeDamage(health.CurrentHealth + 5f, transform.position, Vector3.zero, gameObject);
        }
    }
}
