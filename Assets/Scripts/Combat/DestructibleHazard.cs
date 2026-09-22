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
        private string stamp = "";

        public string StampId => stamp ?? "";

        public void Configure(HazardKind hazardKind)
        {
            kind = hazardKind;
            if (kind == HazardKind.Toxic) damage = 18f;
            if (kind == HazardKind.Oil) damage = 8f;
        }

        public void Stamp(string mark)
        {
            stamp = mark ?? "";
        }

        public void Silence()
        {
            detonated = true;
            gameObject.SetActive(false);
        }

        public static void Sweep()
        {
            if (WorldMapService.Instance == null) return;
            var hazards = Object.FindObjectsByType<DestructibleHazard>(FindObjectsSortMode.None);
            for (int i = 0; i < hazards.Length; i++)
            {
                if (hazards[i] == null) continue;
                if (!WorldMapService.Instance.StreetTaken(hazards[i].StampId)) continue;
                hazards[i].Silence();
            }
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
            if (!string.IsNullOrEmpty(stamp)) WorldMapService.Instance?.NoteStreet(stamp);
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
            GameplayFeedback.Toast(FightSay.Hazard(kind, null));
            Chain(origin);
            if (kind == HazardKind.Toxic) GasField.Open(transform.position);
            if (kind == HazardKind.Oil) OilPatch.Leave(transform.position);
            if (kind == HazardKind.Explosive)
            {
                OilPatch.Blast(transform.position);
                PowderFire.Open(transform.position);
            }
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
