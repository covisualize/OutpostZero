using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Combat
{
    public static class DamageResolver
    {
        public const float HeadshotHeight = 1.45f;
        public const float HeadshotMultiplier = 2f;

        public static void Resolve(RaycastHit hit, float damage, GameObject attacker, bool allowHeadshot, WeaponType weapon = WeaponType.Pistol)
        {
            var hazard = hit.collider.GetComponentInParent<DestructibleHazard>();
            if (hazard != null)
            {
                hazard.TakeHit(damage);
                CombatEvents.RaiseHit(hit.point, hit.normal, hazard.gameObject);
                return;
            }

            var target = hit.collider.GetComponentInParent<IDamageable>();
            var body = target as Component;
            if (target == null || body == null) return;

            bool crit = false;
            float amount = damage;
            if (allowHeadshot && hit.point.y - body.transform.position.y >= HeadshotHeight)
            {
                amount *= HeadshotMultiplier;
                crit = true;
            }

            target.TakeDamage(amount, hit.point, hit.normal, attacker);
            CombatEvents.RaiseHit(hit.point, hit.normal, body.gameObject, weapon);
            if (HitFeedback.Instance != null)
            {
                HitFeedback.Instance.AddNumber(hit.point, amount, crit);
            }
            if (target.IsDead)
            {
                CombatEvents.RaiseKill(body.gameObject, attacker);
            }
        }

        public static void ResolveBody(Collider col, Vector3 point, Vector3 direction, float damage, GameObject attacker, bool allowHeadshot, WeaponType weapon = WeaponType.Pistol)
        {
            if (col == null) return;
            var hazard = col.GetComponentInParent<DestructibleHazard>();
            if (hazard != null)
            {
                hazard.TakeHit(damage);
                CombatEvents.RaiseHit(point, direction, hazard.gameObject);
                return;
            }

            var target = col.GetComponentInParent<IDamageable>();
            var body = target as Component;
            if (target == null || body == null || target.IsDead) return;

            bool crit = allowHeadshot && point.y - body.transform.position.y >= HeadshotHeight;
            float amount = crit ? damage * HeadshotMultiplier : damage;
            target.TakeDamage(amount, point, direction, attacker);
            CombatEvents.RaiseHit(point, direction, body.gameObject, weapon);
            if (HitFeedback.Instance != null)
            {
                HitFeedback.Instance.AddNumber(point, amount, crit);
            }
            if (target.IsDead)
            {
                CombatEvents.RaiseKill(body.gameObject, attacker);
            }
        }
    }
}
