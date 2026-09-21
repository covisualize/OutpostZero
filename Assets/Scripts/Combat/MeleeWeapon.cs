using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Combat
{
    public class MeleeWeapon : WeaponBase
    {
        [Header("Melee Swing Properties")]
        [SerializeField] private float swingRadius = 1.6f;
        [SerializeField] private float swingArcAngle = 100f;
        [SerializeField] private float knockbackForce = 6f;
        [SerializeField] private LayerMask hitMask;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip swingSound;
        [SerializeField] private AudioClip hitFleshSound;

        private void Awake()
        {
            weaponType = WeaponType.Melee;
            noiseType = NoiseType.MeleeSwing;
            if (noiseRadius <= 0f) noiseRadius = 3f; // Very quiet
        }

        public override bool TryAttack(Vector3 targetDirection)
        {
            if (!CanAttack()) return false;

            nextAttackTime = Time.time + (1f / attackRate);

            if (audioSource != null && swingSound != null)
            {
                audioSource.PlayOneShot(swingSound);
            }

            // Emit faint noise (whiff / grunt)
            EmitWeaponNoise();

            Vector3 origin = ownerTransform != null ? ownerTransform.position : transform.position;
            Vector3 forward = targetDirection.normalized;

            // Detect targets in front arc
            Collider[] colliders = Physics.OverlapSphere(origin, range, hitMask);
            int hitCount = 0;

            foreach (var col in colliders)
            {
                if (ownerGameObject != null && (col.gameObject == ownerGameObject || col.transform.IsChildOf(ownerTransform)))
                {
                    continue;
                }

                Vector3 dirToTarget = (col.bounds.center - origin).normalized;
                float angle = Vector3.Angle(forward, dirToTarget);

                if (angle <= swingArcAngle * 0.5f)
                {
                    var damageable = col.GetComponentInParent<IDamageable>();
                    if (damageable != null && !damageable.IsDead)
                    {
                        damageable.TakeDamage(baseDamage, col.bounds.center, dirToTarget, ownerGameObject);
                        hitCount++;

                        // Apply knockback if rigidbody present
                        var rb = col.GetComponentInParent<Rigidbody>();
                        if (rb != null && !rb.isKinematic)
                        {
                            rb.AddForce(dirToTarget * knockbackForce, ForceMode.Impulse);
                        }
                    }
                }
            }

            if (hitCount > 0 && audioSource != null && hitFleshSound != null)
            {
                audioSource.PlayOneShot(hitFleshSound);
            }

            TriggerAttackEvent();
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, range);
        }
    }
}
