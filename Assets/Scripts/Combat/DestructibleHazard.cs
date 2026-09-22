using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Sensory;
using OutpostZero.Shell;

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
        private float fuseAt;
        private float nextHiss;

        public void Configure(HazardKind hazardKind)
        {
            kind = hazardKind;
            if (kind == HazardKind.Toxic) damage = 18f;
            if (kind == HazardKind.Oil) damage = 8f;
        }

        public void TakeHit(float amount)
        {
            if (detonated) return;
            if (amount < 0f) amount = 0f;
            health -= amount;
            if (health <= 0f)
            {
                Detonate();
                return;
            }
            if (!BarrelFuse.Arms(kind) || fuseAt > 0f) return;
            fuseAt = Time.time;
            nextHiss = 0f;
            GameplayFeedback.Toast(Loc.T("barrel.hiss"));
        }

        private void Update()
        {
            if (detonated || fuseAt <= 0f) return;
            float now = Time.time;
            if (BarrelFuse.Due(fuseAt, now, BarrelFuse.Length(kind)))
            {
                Detonate();
                return;
            }
            if (!BarrelFuse.HissDue(nextHiss, now)) return;
            nextHiss = now;
            AudioManager.Instance?.PlayAt("hiss", transform.position, 0.45f);
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

            CombatVfx.Burst(origin, kind);
            CombatEvents.RaiseHit(origin, Vector3.up, gameObject);
            GameplayFeedback.Toast(kind == HazardKind.Explosive ? "Barrel exploded" : kind == HazardKind.Toxic ? "Toxic cloud" : "Oil spill");
            Chain(origin);
            Destroy(gameObject);
        }

        private void Chain(Vector3 origin)
        {
            Collider[] hits = Physics.OverlapSphere(origin, radius);
            foreach (var hit in hits)
            {
                var hazard = hit.GetComponentInParent<DestructibleHazard>();
                if (hazard == null || hazard == this) continue;
                hazard.TakeHit(damage);
            }
        }
    }
}
