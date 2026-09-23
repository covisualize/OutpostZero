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
        private int volley;

        public void Setup(Vector3 dir, float dmg, GameObject attacker, LayerMask targetLayers, Core.WeaponType type = Core.WeaponType.Pistol, float range = 16f, int shot = 0)
        {
            direction = dir.normalized;
            damage = dmg;
            reach = range;
            shooter = attacker;
            hitLayers = targetLayers;
            weapon = type;
            volley = shot;
            origin = transform.position;
            spawnTime = Time.time;

            transform.forward = direction;
        }

        private void Update()
        {
            float step = speed * Time.deltaTime;
            Vector3 startPos = transform.position;
            Vector3 nextPos = startPos + direction * step;

            bool solid = Physics.Raycast(startPos, direction, out RaycastHit hit, step, hitLayers);
            if (solid && hit.collider != null && hit.collider.gameObject == shooter) solid = false;
            bool doorCast = Physics.Raycast(startPos, direction, out RaycastHit slab, step, OutpostZero.Core.GameLayers.InteractableMask);
            var door = doorCast && slab.collider != null ? slab.collider.GetComponent<OutpostZero.Expedition.StreetDoor>() : null;
            if (door != null && door.Barred && (!solid || slab.distance < hit.distance))
            {
                door.Shoot(shooter, weapon, volley);
                CombatVfx.Tracer(transform.position, slab.point);
                OilPatch.Shot(origin, slab.point);
                OutpostZero.AI.ZombieAI.WhiffNear(origin.x, origin.z, slab.point.x, slab.point.z, null);
                Destroy(gameObject);
                return;
            }

            if (solid)
            {
                OnHit(hit);
                return;
            }

            transform.position = nextPos;

            if (Time.time - spawnTime >= maxLifetime)
            {
                OilPatch.Shot(origin, transform.position);
                OutpostZero.AI.ZombieAI.WhiffNear(origin.x, origin.z, transform.position.x, transform.position.z, null);
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
            var struck = hit.collider != null ? hit.collider.GetComponentInParent<OutpostZero.AI.ZombieAI>() : null;
            OutpostZero.AI.ZombieAI.WhiffNear(origin.x, origin.z, hit.point.x, hit.point.z, struck);
            Destroy(gameObject);
        }
    }
}
