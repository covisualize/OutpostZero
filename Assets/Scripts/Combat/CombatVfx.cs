using UnityEngine;
using OutpostZero.Colony;
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
            if (MuzzleShape.Shows(type) && FlashCap.Take(Time.time, Quiet())) Muzzle(muzzle, direction, type);
            if (tracer) Tracer(muzzle, end);
            Shell(muzzle, eject, type);
        }

        public static bool Bolt(Vector3 position)
        {
            if (!FlashCap.Take(Time.time, Quiet())) return false;
            var flash = new GameObject("Lightning");
            Seat(flash);
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
            Seat(sphere);
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
            Leave(origin, kind);
        }

        private static void Leave(Vector3 origin, HazardKind kind)
        {
            if (BlastWake.Ring(kind)) Ring(origin);
            if (BlastWake.Smokes(kind)) Column(origin);
            var go = new GameObject("Blast_" + BlastWake.Wake(kind));
            Seat(go);
            go.transform.position = new Vector3(origin.x, 0.02f, origin.z);
            go.AddComponent<BlastRemain>().Arm(kind);
            OutpostZero.Shell.AudioManager.Instance?.PlayAt(BlastWake.Sound(kind), origin, BlastWake.Volume(kind));
        }

        private static void Ring(Vector3 origin)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ShockRing";
            Seat(ring);
            Object.Destroy(ring.GetComponent<Collider>());
            ring.transform.position = new Vector3(origin.x, 0.08f, origin.z);
            ring.transform.localScale = new Vector3(0.2f, 0.02f, 0.2f);
            var renderer = ring.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(1f, 0.55f, 0.15f, 0.45f);
            ring.AddComponent<BurstFade>().Arm(4.2f, BlastWake.RingTime);
        }

        private static void Column(Vector3 origin)
        {
            var go = new GameObject("SmokeColumn");
            Seat(go);
            go.transform.position = origin;
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = BlastWake.Smoke;
            main.startSpeed = 1.4f;
            main.startSize = 0.45f;
            main.startColor = new Color(0.25f, 0.22f, 0.2f, 0.55f);
            main.gravityModifier = -0.15f;
            main.maxParticles = 24;
            particles.Emit(16);
            Object.Destroy(go, BlastWake.Smoke);
        }

        private static void Muzzle(Vector3 position, Vector3 direction, WeaponType type)
        {
            Flash(position, direction, type, 0.15f, 1f);
            if (MuzzleShape.Strobe(type)) Flash(position, direction, type, 0.28f, 0.45f);
            if (MuzzleShape.Smoke(type)) SmokePuff(position);
        }

        private static void Flash(Vector3 position, Vector3 direction, WeaponType type, float reach, float scale)
        {
            var flash = new GameObject("MuzzleFlash");
            Seat(flash);
            flash.transform.position = position + direction * reach;
            var light = flash.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = MuzzleShape.Range(type);
            light.intensity = MuzzleShape.Intensity(type) * scale;
            light.color = new Color(1f, 0.78f, 0.45f);
            var spark = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.Destroy(spark.GetComponent<Collider>());
            spark.transform.SetParent(flash.transform, false);
            spark.transform.localScale = Vector3.one * (MuzzleShape.Scale(type) * scale);
            var renderer = spark.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = SpriteMaterial();
                renderer.material.color = new Color(1f, 0.9f, 0.55f, 0.9f);
            }
            flash.AddComponent<BurstFade>().Arm(0.45f, MuzzleShape.Hold(type));
        }

        private static void SmokePuff(Vector3 position)
        {
            var go = new GameObject("MuzzleSmoke");
            Seat(go);
            go.transform.position = position;
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 0.6f;
            main.startSize = 0.18f;
            main.startColor = new Color(0.35f, 0.32f, 0.28f, 0.55f);
            main.maxParticles = 8;
            particles.Emit(6);
            Object.Destroy(go, 0.4f);
        }

        private static void Seat(GameObject go)
        {
            if (go != null && go.GetComponent<VfxSeat>() == null) go.AddComponent<VfxSeat>();
        }

        public static void Tracer(Vector3 from, Vector3 to)
        {
            var tracer = new GameObject("Tracer");
            Seat(tracer);
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
            Seat(shell);
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
                Seat(mark);
                Object.Destroy(mark.GetComponent<Collider>());
                mark.transform.position = transform.position + Vector3.down * 0.02f;
                mark.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                mark.transform.localScale = new Vector3(0.05f, 0.09f, 1f);
                var renderer = mark.GetComponent<Renderer>();
                if (renderer != null) renderer.material.color = new Color(0.72f, 0.58f, 0.22f, 0.85f);
                Object.Destroy(mark, 6f);
            }
        }

        public static void Mist(Vector3 point)
        {
            var go = new GameObject("HeadMist");
            Seat(go);
            go.transform.position = point + Vector3.up * 0.15f;
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 1.6f;
            main.startSize = 0.16f;
            main.startColor = new Color(0.55f, 0.08f, 0.07f, 0.7f);
            main.gravityModifier = -0.4f;
            main.maxParticles = 16;
            particles.Emit(14);
            Object.Destroy(go, 0.6f);
            OutpostZero.Shell.AudioManager.Instance?.PlayAt("mist", point, WoundShow.Mist);
        }

        public static void Drip(Vector3 feet)
        {
            var mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mark.name = "BloodDrip";
            Seat(mark);
            Object.Destroy(mark.GetComponent<Collider>());
            mark.transform.position = feet + Vector3.up * 0.02f;
            mark.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mark.transform.localScale = new Vector3(0.12f, 0.16f, 1f);
            var renderer = mark.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.4f, 0.04f, 0.03f, 0.9f);
            Object.Destroy(mark, 6f);
        }

        public static void Embers(Vector3 origin)
        {
            var go = new GameObject("FireEmbers");
            Seat(go);
            go.transform.position = origin + Vector3.up * 0.2f;
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 1.1f;
            main.startSpeed = 0.7f;
            main.startSize = 0.05f;
            main.startColor = new Color(1f, 0.45f, 0.1f, 0.9f);
            main.gravityModifier = -0.2f;
            main.maxParticles = YardGlow.Embers;
            particles.Emit(YardGlow.Embers);
            Object.Destroy(go, 1.2f);
        }

        public static void Sparks(Vector3 origin)
        {
            var go = new GameObject("LampSparks");
            Seat(go);
            go.transform.position = origin;
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.22f;
            main.startSpeed = 2.4f;
            main.startSize = 0.03f;
            main.startColor = new Color(1f, 0.9f, 0.45f, 1f);
            main.gravityModifier = 1.2f;
            main.maxParticles = YardGlow.Sparks;
            particles.Emit(YardGlow.Sparks);
            Object.Destroy(go, 0.4f);
        }

        public static void Puff(Vector3 feet, int count, bool wet)
        {
            if (count <= 0) return;
            var go = new GameObject(wet ? "StepSplash" : "StepDust");
            Seat(go);
            go.transform.position = feet + Vector3.up * 0.05f;
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = wet ? 0.28f : 0.4f;
            main.startSpeed = wet ? 1.4f : 0.8f;
            main.startSize = wet ? 0.1f : 0.14f;
            main.startColor = wet ? new Color(0.62f, 0.74f, 0.82f, 0.7f) : new Color(0.55f, 0.5f, 0.42f, 0.55f);
            main.maxParticles = 12;
            particles.Emit(count);
            Object.Destroy(go, 0.6f);
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

        private sealed class BlastRemain : MonoBehaviour
        {
            private HazardKind kind;
            private float until;

            public void Arm(HazardKind hazard)
            {
                kind = hazard;
                until = Time.time + BlastWake.Hold(kind);
                if (kind == HazardKind.Oil || kind == HazardKind.Explosive)
                {
                    var fire = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    fire.name = "GroundFire";
                    Object.Destroy(fire.GetComponent<Collider>());
                    fire.transform.SetParent(transform, false);
                    fire.transform.localScale = new Vector3(1.4f, 0.35f, 1.4f);
                    var renderer = fire.GetComponent<Renderer>();
                    if (renderer != null) renderer.material.color = new Color(1f, 0.42f, 0.08f, 0.75f);
                }
                if (kind == HazardKind.Oil)
                {
                    var slick = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    slick.name = "OilSlick";
                    Object.Destroy(slick.GetComponent<Collider>());
                    slick.transform.SetParent(transform, false);
                    slick.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    slick.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
                    var renderer = slick.GetComponent<Renderer>();
                    if (renderer != null) renderer.material.color = new Color(0.05f, 0.05f, 0.04f, 0.9f);
                }
                if (kind == HazardKind.Toxic)
                {
                    var cloud = gameObject.AddComponent<ParticleSystem>();
                    var main = cloud.main;
                    main.startLifetime = 1.2f;
                    main.startSpeed = 0.35f;
                    main.startSize = 0.7f;
                    main.startColor = new Color(0.4f, 0.85f, 0.28f, 0.45f);
                    main.loop = true;
                    main.maxParticles = 20;
                    cloud.Play();
                }
            }

            private void Update()
            {
                if (Time.time < until) return;
                Destroy(gameObject);
            }
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
