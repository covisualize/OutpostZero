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
        private float reach = 16f;
        private Vector3 direction;
        private Vector3 origin;
        private GameObject shooter;
        private float spawnTime;
        private Core.WeaponType weapon = Core.WeaponType.Pistol;

        public void Setup(Vector3 dir, float dmg, GameObject attacker, LayerMask targetLayers, Core.WeaponType type = Core.WeaponType.Pistol, float range = 16f)
        {
            direction = dir.normalized;
            damage = dmg;
            reach = range;
            shooter = attacker;
            hitLayers = targetLayers;
            weapon = type;
            origin = transform.position;
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
                OilPatch.Shot(origin, transform.position);
                Destroy(gameObject);
            }
        }

        private void OnHit(RaycastHit hit)
        {
            CombatEvents.NoteDir(direction);
            float flown = Vector3.Distance(origin, hit.point);
            float amount = PelletDrop.Damage(damage, flown, reach, weapon);
            DamageResolver.Resolve(hit, amount, shooter, true, weapon);
            CombatVfx.Tracer(transform.position, hit.point);
            OilPatch.Shot(origin, hit.point);
            Destroy(gameObject);
        }
    }
}
