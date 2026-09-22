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

        private void Reset()
        {
            hitMask = GameLayers.WeaponHitMask;
            weaponType = WeaponType.Melee;
            noiseType = NoiseType.MeleeSwing;
        }

        private void OnValidate()
        {
            hitMask = GameLayers.Resolve(hitMask, GameLayers.WeaponHitMask);
        }

        private void Awake()
        {
            hitMask = GameLayers.Resolve(hitMask, GameLayers.WeaponHitMask);
            weaponType = WeaponType.Melee;
            noiseType = NoiseType.MeleeSwing;
            if (noiseRadius <= 0f) noiseRadius = 3f;
        }

        public override void Configure(WeaponDefinition definition)
        {
            base.Configure(definition);
            weaponType = WeaponType.Melee;
            noiseType = NoiseType.MeleeSwing;
            hitMask = GameLayers.WeaponHitMask;
        }

        public override bool TryAttack(Vector3 targetDirection)
        {
            if (!CanAttack()) return false;

            nextAttackTime = Time.time + (1f / attackRate);

            if (audioSource != null && swingSound != null)
            {
                audioSource.PlayOneShot(swingSound);
            }
            string whoosh = SwingCue.Sound(weaponType);
            if (whoosh.Length > 0)
                OutpostZero.Shell.AudioManager.Instance?.PlayAt(whoosh, transform.position, SwingCue.Volume);

            Vector3 origin = ownerTransform != null ? ownerTransform.position : transform.position;
            Vector3 forward = targetDirection.normalized;

            // Detect targets in front arc
            Collider[] colliders = Physics.OverlapSphere(origin, range, hitMask);
            int hitCount = 0;
            bool wall = false;

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
                    var pane = col.GetComponent<OutpostZero.Expedition.GlassPane>();
                    if (pane != null)
                    {
                        pane.TakeDamage(BladePane.Hit(baseDamage), col.bounds.center, dirToTarget, ownerGameObject);
                        hitCount++;
                    }
                    else
                    {
                        var damageable = col.GetComponentInParent<IDamageable>();
                        var hazard = col.GetComponentInParent<DestructibleHazard>();
                        if ((damageable != null && !damageable.IsDead) || hazard != null)
                        {
                            CombatEvents.NoteDir(dirToTarget);
                            DamageResolver.ResolveBody(col, col.bounds.center, dirToTarget, baseDamage * (GetComponent<WeaponMod>() != null ? GetComponent<WeaponMod>().damageMultiplier : 1f), ownerGameObject, false, WeaponType.Melee);
                            hitCount++;

                            var rb = col.GetComponentInParent<Rigidbody>();
                            if (rb != null && !rb.isKinematic)
                            {
                                rb.AddForce(dirToTarget * knockbackForce, ForceMode.Impulse);
                            }
                        }
                        else if (ContactCue.Wall(col.gameObject.layer)) wall = true;
                    }
                }
            }

            var bars = Physics.OverlapSphere(origin, range, GameLayers.InteractableMask);
            for (int i = 0; i < bars.Length; i++)
            {
                var col = bars[i];
                if (col == null) continue;
                var door = col.GetComponent<OutpostZero.Expedition.StreetDoor>();
                if (door == null || !door.Barred) continue;
                Vector3 toDoor = col.bounds.center - origin;
                toDoor.y = 0f;
                if (toDoor.sqrMagnitude < 0.0001f) continue;
                if (Vector3.Angle(forward, toDoor.normalized) > swingArcAngle * 0.5f) continue;
                door.Strike(ownerGameObject);
                hitCount++;
            }

            if (hitCount == 0 && wall)
            {
                OutpostZero.Shell.AudioManager.Instance?.PlayAt("clang", origin, 0.42f, 0.85f);
                var body = ownerGameObject != null ? ownerGameObject.GetComponent<OutpostZero.Player.PlayerController>() : null;
                float radius = BladeClang.Radius(body != null && body.IsCrouching);
                if (OutpostZero.Sensory.NoiseManager.Instance != null && radius > 0f)
                    OutpostZero.Sensory.NoiseManager.Instance.EmitNoise(origin, radius, 0.9f, NoiseType.MeleeSwing, ownerGameObject);
            }
            else
            {
                var body = ownerGameObject != null ? ownerGameObject.GetComponent<OutpostZero.Player.PlayerController>() : null;
                EmitWeaponNoise(QuietSwing.Scale(body != null && body.IsCrouching, weaponType));
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
