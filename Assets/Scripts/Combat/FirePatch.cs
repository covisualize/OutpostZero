using UnityEngine;
using OutpostZero.Shell;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A molotov leaves a patch after the burst. The burst is still 28 inside 3.2 meters.
    /// The patch burns for 4 seconds and ticks 6 damage every half second inside 2.4 meters.
    /// An oil trench keeps its own eight-second burn.
    /// </summary>
    public static class FirePatch
    {
        public const float Burst = 28f;
        public const float BurstRadius = 3.2f;
        public const float Life = 4f;
        public const float Gap = 0.5f;
        public const float Radius = 2.4f;
        public const float Damage = 6f;

        public static bool Hot(float age)
        {
            return age >= 0f && age < Life;
        }

        public static bool Inside(float distance)
        {
            if (distance < 0f) return false;
            return distance <= Radius;
        }

        public static bool TickDue(float previous, float now)
        {
            if (now < 0f || now < previous) return false;
            if (!Hot(now)) return false;
            if (previous < 0f) return true;
            int before = (int)(previous / Gap);
            int after = (int)(now / Gap);
            return after > before;
        }
    }

    /// <summary>The flame left on the street after a molotov breaks.</summary>
    public class GroundFire : MonoBehaviour
    {
        private float age = -1f;

        private void Start()
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "FireSheet";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            body.transform.localScale = new Vector3(FirePatch.Radius, 0.04f, FirePatch.Radius);
            var solid = body.GetComponent<Collider>();
            if (solid != null) Destroy(solid);
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(1f, 0.42f, 0.08f);
            var glow = gameObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = 6f;
            glow.intensity = 1.8f;
            glow.color = new Color(1f, 0.45f, 0.12f);
            AudioManager.Instance?.PlayAt("burn", transform.position, 0.8f);
        }

        private void Update()
        {
            float next = age < 0f ? 0f : age + Time.deltaTime;
            if (FirePatch.TickDue(age, next)) Scorch();
            age = next;
            if (!FirePatch.Hot(age)) Destroy(gameObject);
        }

        private void Scorch()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, FirePatch.Radius);
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null) continue;
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead) continue;
                if (!FirePatch.Inside(Vector3.Distance(transform.position, hit.bounds.center))) continue;
                damageable.TakeDamage(FirePatch.Damage, hit.bounds.center, (hit.transform.position - transform.position).normalized, gameObject);
                hit.GetComponentInParent<OutpostZero.AI.ZombieAI>()?.Ignite();
            }
        }
    }
}
