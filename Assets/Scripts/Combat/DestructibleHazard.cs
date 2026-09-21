using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Sensory;

namespace OutpostZero.Combat
{
    public enum HazardKind
    {
        Explosive,
        Toxic,
        Oil
    }

    public class DestructibleHazard : MonoBehaviour
    {
        [SerializeField] private HazardKind kind = HazardKind.Explosive;
        [SerializeField] private float health = 30f;
        [SerializeField] private float radius = 4.5f;
        [SerializeField] private float damage = 55f;
        private bool detonated;

        public void Configure(HazardKind hazardKind)
        {
            kind = hazardKind;
            if (kind == HazardKind.Toxic) damage = 18f;
            if (kind == HazardKind.Oil) damage = 8f;
        }

        public void TakeHit(float amount)
        {
            if (detonated) return;
            health -= amount;
            if (health <= 0f) Detonate();
        }

        private void Detonate()
        {
            detonated = true;
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (NoiseManager.Instance != null)
            {
                NoiseType noise = kind == HazardKind.Explosive ? NoiseType.Explosion : NoiseType.ObjectBroken;
                NoiseManager.Instance.EmitNoise(origin, kind == HazardKind.Explosive ? 28f : 10f, 1f, noise, gameObject);
            }

            Collider[] hits = Physics.OverlapSphere(origin, radius);
            foreach (var hit in hits)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead) continue;
                damageable.TakeDamage(damage, hit.bounds.center, (hit.transform.position - origin).normalized, gameObject);
                if (kind == HazardKind.Toxic)
                {
                    var effects = hit.GetComponentInParent<Player.StatusEffectController>();
                    if (effects != null) effects.ApplyPoison(6f);
                }
            }

            CombatEvents.RaiseHit(origin, Vector3.up, gameObject);
            GameplayFeedback.Toast(kind == HazardKind.Explosive ? "Barrel exploded" : kind == HazardKind.Toxic ? "Toxic cloud" : "Oil spill");
            Destroy(gameObject);
        }
    }
}
