using UnityEngine;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A powder barrel leaves a fire after the flash. It burns for ten seconds
    /// and deals eight damage once a second inside the blast ring.
    /// The molotov patch stays at four seconds and six damage.
    /// </summary>
    public static class PowderBed
    {
        public const float Life = 10f;
        public const float Gap = 1f;
        public const float Radius = 3.2f;
        public const float Damage = 8f;

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

    /// <summary>The crater fire a red barrel leaves behind.</summary>
    public class PowderFire : MonoBehaviour
    {
        private float age = -1f;

        public static void Open(Vector3 at)
        {
            var go = new GameObject("PowderFire");
            go.transform.position = at;
            go.AddComponent<PowderFire>();
        }

        private void Start()
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            body.name = "PowderSheet";
            body.transform.SetParent(transform, false);
            body.transform.localPosition = new Vector3(0f, 0.06f, 0f);
            float width = PowderBed.Radius * 2f;
            body.transform.localScale = new Vector3(width, 0.05f, width);
            var solid = body.GetComponent<Collider>();
            if (solid != null) Destroy(solid);
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.95f, 0.28f, 0.05f);
            var glow = gameObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = 7f;
            glow.intensity = 2.2f;
            glow.color = new Color(1f, 0.35f, 0.08f);
        }

        private void Update()
        {
            float next = age < 0f ? 0f : age + Time.deltaTime;
            if (PowderBed.TickDue(age, next)) Scorch();
            age = next;
            if (!PowderBed.Hot(age)) Destroy(gameObject);
        }

        private void Scorch()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, PowderBed.Radius);
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null) continue;
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable.IsDead) continue;
                if (!PowderBed.Inside(Vector3.Distance(transform.position, hit.bounds.center))) continue;
                damageable.TakeDamage(PowderBed.Damage, hit.bounds.center, (hit.transform.position - transform.position).normalized, gameObject);
                hit.GetComponentInParent<OutpostZero.AI.ZombieAI>()?.Ignite();
            }
        }
    }
}
