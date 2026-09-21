using UnityEngine;

namespace OutpostZero.Combat
{
    public class BulletProjectile : MonoBehaviour
    {
        [Header("Ballistics")]
        [SerializeField] private float speed = 55f;
        [SerializeField] private float maxLifetime = 3f;
        [SerializeField] private LayerMask hitLayers;

        private float damage;
        private Vector3 direction;
        private GameObject shooter;
        private float spawnTime;

        public void Setup(Vector3 dir, float dmg, GameObject attacker, LayerMask targetLayers)
        {
            direction = dir.normalized;
            damage = dmg;
            shooter = attacker;
            hitLayers = targetLayers;
            spawnTime = Time.time;

            transform.forward = direction;
        }

        private void Update()
        {
            float step = speed * Time.deltaTime;
            Vector3 startPos = transform.position;
            Vector3 nextPos = startPos + direction * step;

            if (Physics.Raycast(startPos, direction, out RaycastHit hit, step, hitLayers))
            {
                // Don't hit the shooter
                if (hit.collider.gameObject != shooter)
                {
                    OnHit(hit);
                    return;
                }
            }

            transform.position = nextPos;

            if (Time.time - spawnTime >= maxLifetime)
            {
                Destroy(gameObject);
            }
        }

        private void OnHit(RaycastHit hit)
        {
            var damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage, hit.point, direction, shooter);
            }

            // Spawn simple impact particle or decal here if available
            Destroy(gameObject);
        }
    }
}
