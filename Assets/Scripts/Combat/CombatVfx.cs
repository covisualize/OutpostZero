using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// Muzzle flash, bullet tracers, ejected shells, and barrel bursts.
    /// Built at runtime so the prototype weapons do not need authored particle assets.
    /// </summary>
    public static class CombatVfx
    {
        private static Material spriteMaterial;

        public static void Shot(Vector3 muzzle, Vector3 direction, Vector3 end, Vector3 eject)
        {
            Shot(muzzle, direction, end, eject, true, WeaponType.Pistol);
        }

        public static void Shot(Vector3 muzzle, Vector3 direction, Vector3 end, Vector3 eject, bool tracer, WeaponType type)
        {
            direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            if (FlashCap.Take(Time.time, Quiet())) Muzzle(muzzle, direction);
            if (tracer) Tracer(muzzle, end);
            Shell(muzzle, eject, type);
        }

        public static bool Bolt(Vector3 position)
        {
            if (!FlashCap.Take(Time.time, Quiet())) return false;
            var flash = new GameObject("Lightning");
            flash.transform.position = position;
            var light = flash.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 40f;
            light.intensity = 2.2f;
            light.color = new Color(0.75f, 0.82f, 1f);
            flash.AddComponent<BurstFade>().Arm(1f, 0.08f);
            return true;
        }

        private static bool Quiet()
        {
            return Core.SettingsService.Instance != null && Core.SettingsService.Instance.QuietFlash;
        }

        public static void Burst(Vector3 origin, HazardKind kind)
        {
            Color color = kind == HazardKind.Toxic ? new Color(0.45f, 0.85f, 0.3f, 0.55f)
                : kind == HazardKind.Oil ? new Color(0.15f, 0.12f, 0.08f, 0.7f)
                : new Color(1f, 0.45f, 0.12f, 0.65f);
            float radius = kind == HazardKind.Explosive ? 4.2f : 2.4f;
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Burst_" + kind;
            Object.Destroy(sphere.GetComponent<Collider>());
            sphere.transform.position = origin;
            sphere.transform.localScale = Vector3.one * 0.4f;
            var renderer = sphere.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = SpriteMaterial();
                renderer.material.color = color;
            }
            var lightObject = new GameObject("BurstLight");
            lightObject.transform.SetParent(sphere.transform, false);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = radius * 2f;
            light.intensity = kind == HazardKind.Explosive ? 6f : 2f;
            light.color = color;
            sphere.AddComponent<BurstFade>().Arm(radius, 0.35f);
        }

        private static void Muzzle(Vector3 position, Vector3 direction)
        {
            var flash = new GameObject("MuzzleFlash");
            flash.transform.position = position + direction * 0.15f;
            var light = flash.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4.5f;
            light.intensity = 3.5f;
            light.color = new Color(1f, 0.78f, 0.45f);
            var spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(spark.GetComponent<Collider>());
            spark.transform.SetParent(flash.transform, false);
            spark.transform.localScale = Vector3.one * 0.12f;
            var renderer = spark.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = SpriteMaterial();
                renderer.material.color = new Color(1f, 0.9f, 0.55f, 0.9f);
            }
            flash.AddComponent<BurstFade>().Arm(0.45f, 0.05f);
        }

        public static void Tracer(Vector3 from, Vector3 to)
        {
            var tracer = new GameObject("Tracer");
            var line = tracer.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.SetPosition(0, from);
            line.SetPosition(1, to);
            line.startWidth = 0.035f;
            line.endWidth = 0.01f;
            line.material = SpriteMaterial();
            line.startColor = new Color(1f, 0.86f, 0.45f, 0.95f);
            line.endColor = new Color(1f, 0.45f, 0.15f, 0.1f);
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            Object.Destroy(tracer, 0.05f);
        }

        private static void Shell(Vector3 position, Vector3 eject, WeaponType type)
        {
            var shell = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shell.name = "Shell";
            shell.transform.position = position;
            bool hull = type == WeaponType.Shotgun;
            shell.transform.localScale = hull ? new Vector3(0.04f, 0.04f, 0.09f) : new Vector3(0.03f, 0.03f, 0.07f);
            var renderer = shell.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.72f, 0.58f, 0.22f);
            var body = shell.AddComponent<Rigidbody>();
            body.mass = 0.02f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            if (eject.sqrMagnitude < 0.01f) eject = Vector3.right;
            body.AddForce((eject.normalized + Vector3.up) * 1.6f, ForceMode.Impulse);
            body.AddTorque(Random.insideUnitSphere * 0.4f, ForceMode.Impulse);
            shell.AddComponent<BrassDrop>().Arm(BrassCue.Sound(type), BrassCue.Volume(type));
            Object.Destroy(shell, 1.4f);
        }

        private sealed class BrassDrop : MonoBehaviour
        {
            private float ejectedAt;
            private string sound = "";
            private float volume;
            private bool played;

            public void Arm(string id, float gain)
            {
                ejectedAt = Time.time;
                sound = id ?? "";
                volume = gain;
            }

            private void Update()
            {
                if (played) return;
                if (!BrassCue.Due(Time.time, ejectedAt)) return;
                played = true;
                if (sound.Length == 0 || volume <= 0f) return;
                OutpostZero.Shell.AudioManager.Instance?.PlayAt(sound, transform.position, volume);
                var mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
                mark.name = "BrassMark";
                Object.Destroy(mark.GetComponent<Collider>());
                mark.transform.position = transform.position + Vector3.down * 0.02f;
                mark.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                mark.transform.localScale = new Vector3(0.05f, 0.09f, 1f);
                var renderer = mark.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(0.72f, 0.58f, 0.22f, 0.85f);
                Object.Destroy(mark, 6f);
            }
        }

        private static Material SpriteMaterial()
        {
            if (spriteMaterial != null) return spriteMaterial;
            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            spriteMaterial = new Material(shader);
            return spriteMaterial;
        }

        private sealed class BurstFade : MonoBehaviour
        {
            private float target = 1f;
            private float life = 0.3f;
            private float age;
            private Vector3 start;

            public void Arm(float endScale, float duration)
            {
                target = endScale;
                life = Mathf.Max(0.02f, duration);
                start = transform.localScale;
            }

            private void Update()
            {
                age += Time.deltaTime;
                float t = Mathf.Clamp01(age / life);
                transform.localScale = Vector3.Lerp(start, Vector3.one * target, t);
                if (age >= life) Destroy(gameObject);
            }
        }
    }
}
