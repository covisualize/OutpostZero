using System;
using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Core;
using Object = UnityEngine.Object;

namespace OutpostZero.Combat
{
    /// <summary>
    /// Muzzle flash, bullet tracers, ejected shells, impacts, blood and barrel bursts. Every effect is
    /// a <see cref="VfxEvent"/> rented from <see cref="VfxPool"/>: an authored prefab from the
    /// VfxLibrary plays when one is set, otherwise the procedural build here is made once and re-armed.
    /// </summary>
    public static class CombatVfx
    {
        private static Material spriteMaterial;
        private static MaterialPropertyBlock block;

        public static void Shot(Vector3 muzzle, Vector3 direction, Vector3 end, Vector3 eject)
        {
            Shot(muzzle, direction, end, eject, true, WeaponType.Pistol);
        }

        public static void Shot(Vector3 muzzle, Vector3 direction, Vector3 end, Vector3 eject, bool tracer, WeaponType type)
        {
            Shot(muzzle, direction, end, eject, tracer, type, VfxBook.MuzzleFor(type));
        }

        public static void Shot(Vector3 muzzle, Vector3 direction, Vector3 end, Vector3 eject, bool tracer, WeaponType type, VfxEvent flash)
        {
            direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            if (flash != VfxEvent.None && MuzzleShape.Shows(type) && FlashCap.Take(Time.time, Quiet())) Muzzle(muzzle, direction, flash);
            if (tracer) Tracer(muzzle, end);
            Shell(muzzle, eject, type);
        }

        /// <summary>
        /// Plays an event that needs only a spot and a direction: the data-driven hook for weapons,
        /// archetypes and hazards that name their effect.
        /// </summary>
        public static void Play(VfxEvent id, Vector3 at, Vector3 direction)
        {
            if (id == VfxEvent.None) return;
            Quaternion facing = direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
            if (VfxPool.PlayAuthored(id, at, facing)) return;
            switch (id)
            {
                case VfxEvent.MuzzlePistol:
                case VfxEvent.MuzzleShotgun:
                case VfxEvent.MuzzleRifle: Muzzle(at, direction, id); return;
                case VfxEvent.ImpactSpark: Impact(at, "metal", direction.x, direction.z); return;
                case VfxEvent.ImpactDust: Impact(at, "concrete", direction.x, direction.z); return;
                case VfxEvent.ImpactSplinter: Impact(at, "wood", direction.x, direction.z); return;
                case VfxEvent.BloodSpray: Impact(at, "flesh", direction.x, direction.z); return;
                case VfxEvent.BloodMist: Mist(at); return;
                case VfxEvent.BloodDrip: Drip(at); return;
                case VfxEvent.DeathBurst: Death(at, direction); return;
                case VfxEvent.SmokeColumn: Column(at); return;
                case VfxEvent.Shockwave: Ring(at); return;
                case VfxEvent.Fireball: Ball(at); return;
                case VfxEvent.Debris: Chunks(at); return;
                case VfxEvent.ExplosionFlash: Burst(at, HazardKind.Explosive); return;
                case VfxEvent.ToxicCloud: Burst(at, HazardKind.Toxic); return;
                case VfxEvent.OilFire: Burst(at, HazardKind.Oil); return;
                case VfxEvent.Embers: Embers(at); return;
                case VfxEvent.LampSparks: Sparks(at); return;
                case VfxEvent.FootDust: Puff(at, 4, false); return;
                case VfxEvent.FootSplash: Puff(at, 4, true); return;
                case VfxEvent.Lightning: Bolt(at); return;
                default: return;
            }
        }

        public static VfxEvent BlastFor(HazardKind kind)
        {
            if (kind == HazardKind.Toxic) return VfxEvent.ToxicCloud;
            if (kind == HazardKind.Oil) return VfxEvent.OilFire;
            return VfxEvent.ExplosionFlash;
        }

        public static bool Bolt(Vector3 position)
        {
            if (!FlashCap.Take(Time.time, Quiet())) return false;
            if (VfxPool.PlayAuthored(VfxEvent.Lightning, position, Quaternion.identity)) return true;
            var flash = VfxPool.Rent(VfxEvent.Lightning, position, Quaternion.identity, () =>
            {
                var go = new GameObject("Lightning");
                var built = go.AddComponent<Light>();
                built.type = LightType.Point;
                built.range = 40f;
                built.color = new Color(0.75f, 0.82f, 1f);
                return go;
            }, out _);
            flash.GetComponent<Light>().intensity = 2.2f;
            Fade(flash).Arm(Vector3.one, 1f, 0.08f);
            return true;
        }

        private static bool Quiet()
        {
            return SettingsService.Instance != null && SettingsService.Instance.QuietFlash;
        }

        public static void Burst(Vector3 origin, HazardKind kind)
        {
            Burst(origin, kind, BlastFor(kind));
        }

        public static void Burst(Vector3 origin, HazardKind kind, VfxEvent blast)
        {
            if (blast != BlastFor(kind) && VfxPool.PlayAuthored(blast, origin, Quaternion.identity))
            {
                Leave(origin, kind);
                return;
            }
            if (!VfxPool.PlayAuthored(VfxEvent.ExplosionFlash, origin, Quaternion.identity))
            {
                Color color = kind == HazardKind.Toxic ? new Color(0.45f, 0.85f, 0.3f, 0.55f)
                    : kind == HazardKind.Oil ? new Color(0.15f, 0.12f, 0.08f, 0.7f)
                    : new Color(1f, 0.45f, 0.12f, 0.65f);
                float radius = kind == HazardKind.Explosive ? 4.2f : 2.4f;
                var sphere = VfxPool.Rent(VfxEvent.ExplosionFlash, origin, Quaternion.identity, () =>
                {
                    var go = Primitive(PrimitiveType.Sphere, "Burst");
                    var lightObject = new GameObject("BurstLight");
                    lightObject.transform.SetParent(go.transform, false);
                    lightObject.AddComponent<Light>().type = LightType.Point;
                    return go;
                }, out _);
                Tint(sphere.GetComponent<Renderer>(), color);
                var light = sphere.GetComponentInChildren<Light>();
                light.range = radius * 2f;
                light.intensity = kind == HazardKind.Explosive ? 6f : 2f;
                light.color = color;
                Fade(sphere).Arm(Vector3.one * 0.4f, radius, 0.35f);
            }
            Leave(origin, kind);
        }

        private static void Leave(Vector3 origin, HazardKind kind)
        {
            if (BlastWake.Ring(kind)) Ring(origin);
            if (BlastBall.Shows(kind)) Ball(origin);
            if (BlastWake.Smokes(kind)) Column(origin);
            VfxEvent remain = kind == HazardKind.Toxic ? VfxEvent.ToxicCloud : kind == HazardKind.Oil ? VfxEvent.OilFire : VfxEvent.GroundFire;
            Vector3 floor = new Vector3(origin.x, 0.02f, origin.z);
            if (!VfxPool.PlayAuthored(remain, floor, Quaternion.identity))
            {
                var go = VfxPool.Rent(remain, floor, Quaternion.identity, () => BuildRemain(kind), out _);
                go.GetComponent<VfxReturn>().Arm(remain, Time.time + BlastWake.Hold(kind));
                var cloud = go.GetComponent<ParticleSystem>();
                if (cloud != null)
                {
                    cloud.Clear(true);
                    cloud.Play(true);
                }
            }
            if (BlastChunk.Throws(kind)) Chunks(origin);
            OutpostZero.Shell.AudioManager.Instance?.PlayAt(BlastWake.Sound(kind), origin, BlastWake.Volume(kind));
        }

        private static GameObject BuildRemain(HazardKind kind)
        {
            var go = new GameObject("Blast_" + BlastWake.Wake(kind));
            if (kind == HazardKind.Oil || kind == HazardKind.Explosive)
            {
                var fire = Primitive(PrimitiveType.Sphere, "GroundFire");
                fire.transform.SetParent(go.transform, false);
                fire.transform.localScale = new Vector3(1.4f, 0.35f, 1.4f);
                Tint(fire.GetComponent<Renderer>(), new Color(1f, 0.42f, 0.08f, 0.75f));
            }
            if (kind == HazardKind.Oil)
            {
                var slick = Primitive(PrimitiveType.Quad, "OilSlick");
                slick.transform.SetParent(go.transform, false);
                slick.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                slick.transform.localScale = new Vector3(2.4f, 2.4f, 1f);
                Tint(slick.GetComponent<Renderer>(), new Color(0.05f, 0.05f, 0.04f, 0.9f));
            }
            if (kind == HazardKind.Toxic)
            {
                var cloud = go.AddComponent<ParticleSystem>();
                var main = cloud.main;
                main.startLifetime = 1.2f;
                main.startSpeed = 0.35f;
                main.startSize = 0.7f;
                main.startColor = new Color(0.4f, 0.85f, 0.28f, 0.45f);
                main.loop = true;
                main.maxParticles = 20;
                var emission = cloud.emission;
                emission.rateOverTime = 14f;
            }
            return go;
        }

        private static void Chunks(Vector3 origin)
        {
            if (VfxPool.PlayAuthored(VfxEvent.Debris, origin, Quaternion.identity)) return;
            var root = VfxPool.Rent(VfxEvent.Debris, origin, Quaternion.identity, () =>
            {
                var go = new GameObject("BlastChunks");
                for (int i = 0; i < BlastChunk.Count; i++)
                {
                    var chunk = Primitive(PrimitiveType.Cube, "BarrelBit");
                    chunk.transform.SetParent(go.transform, false);
                    chunk.transform.localScale = Vector3.one * 0.12f;
                    Tint(chunk.GetComponent<Renderer>(), new Color(0.28f, 0.16f, 0.1f, 1f));
                    chunk.AddComponent<Rigidbody>().mass = 0.2f;
                }
                return go;
            }, out _);
            root.GetComponent<VfxReturn>().Arm(VfxEvent.Debris, Time.time + BlastChunk.Life);
            var bodies = root.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                float rad = i * (6.2831853f / bodies.Length);
                var body = bodies[i];
                body.transform.localPosition = Vector3.up * 0.3f;
                body.transform.localRotation = Quaternion.identity;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.AddForce(new Vector3(Mathf.Cos(rad), 0.8f, Mathf.Sin(rad)) * BlastChunk.Speed, ForceMode.Impulse);
            }
        }

        private static void Ring(Vector3 origin)
        {
            Vector3 at = new Vector3(origin.x, 0.08f, origin.z);
            if (VfxPool.PlayAuthored(VfxEvent.Shockwave, at, Quaternion.identity)) return;
            var ring = VfxPool.Rent(VfxEvent.Shockwave, at, Quaternion.identity, () =>
            {
                var go = Primitive(PrimitiveType.Cylinder, "ShockRing");
                Tint(go.GetComponent<Renderer>(), new Color(1f, 0.55f, 0.15f, 0.45f));
                return go;
            }, out _);
            Fade(ring).Arm(new Vector3(0.2f, 0.02f, 0.2f), 4.2f, BlastWake.RingTime);
        }

        private static void Ball(Vector3 origin)
        {
            Vector3 at = origin + Vector3.up * 0.8f;
            if (!VfxPool.PlayAuthored(VfxEvent.Fireball, at, Quaternion.identity))
            {
                var root = VfxPool.Rent(VfxEvent.Fireball, at, Quaternion.identity, () =>
                {
                    var go = new GameObject("Fireball");
                    var sheet = Primitive(PrimitiveType.Quad, "FireballSheet");
                    sheet.transform.SetParent(go.transform, false);
                    sheet.AddComponent<FireSheet>();
                    return go;
                }, out _);
                root.GetComponent<VfxReturn>().Arm(VfxEvent.Fireball, Time.time + BlastBall.Life);
                var sheetRenderer = root.GetComponentInChildren<Renderer>();
                root.GetComponentInChildren<FireSheet>().Arm(sheetRenderer);
            }

            Vector3 floor = new Vector3(origin.x, 0.12f, origin.z);
            var warp = VfxPool.Rent(VfxEvent.Shockwave, floor, Quaternion.identity, () => Primitive(PrimitiveType.Cylinder, "WarpRing"), out _);
            Tint(warp.GetComponent<Renderer>(), new Color(0.85f, 0.9f, 1f, 0.28f));
            Fade(warp).Arm(new Vector3(0.4f, 0.01f, 0.4f), BlastBall.WarpScale(1f), BlastBall.WarpTime);
        }

        private static Color FrameColor(int frame)
        {
            if (frame <= 0) return new Color(1f, 0.95f, 0.7f, 0.95f);
            if (frame == 1) return new Color(1f, 0.55f, 0.12f, 0.9f);
            if (frame == 2) return new Color(0.85f, 0.22f, 0.05f, 0.75f);
            return new Color(0.25f, 0.08f, 0.04f, 0.35f);
        }

        private static void Column(Vector3 origin)
        {
            if (VfxPool.PlayAuthored(VfxEvent.SmokeColumn, origin, Quaternion.identity)) return;
            Spray(VfxEvent.SmokeColumn, origin, BlastWake.Smoke, 1.4f, 0.45f, new Color(0.25f, 0.22f, 0.2f, 0.55f), -0.15f, 24, 16);
        }

        private static WeaponType ShapeOf(VfxEvent flash)
        {
            if (flash == VfxEvent.MuzzleShotgun) return WeaponType.Shotgun;
            if (flash == VfxEvent.MuzzleRifle) return WeaponType.Rifle;
            return WeaponType.Pistol;
        }

        private static void Muzzle(Vector3 position, Vector3 direction, VfxEvent flash)
        {
            if (direction.sqrMagnitude < 0.001f) direction = Vector3.forward;
            if (VfxPool.PlayAuthored(flash, position, Quaternion.LookRotation(direction))) return;
            WeaponType type = ShapeOf(flash);
            Flash(flash, position, direction, type, 0.15f, 1f);
            if (MuzzleShape.Strobe(type)) Flash(flash, position, direction, type, 0.28f, 0.45f);
            if (MuzzleShape.Smoke(type)) SmokePuff(position);
        }

        private static void Flash(VfxEvent id, Vector3 position, Vector3 direction, WeaponType type, float reach, float scale)
        {
            var flash = VfxPool.Rent(id, position + direction * reach, Quaternion.identity, () =>
            {
                var go = new GameObject("MuzzleFlash");
                var built = go.AddComponent<Light>();
                built.type = LightType.Point;
                built.color = new Color(1f, 0.78f, 0.45f);
                var spark = Primitive(PrimitiveType.Sphere, "MuzzleSpark");
                spark.transform.SetParent(go.transform, false);
                Tint(spark.GetComponent<Renderer>(), new Color(1f, 0.9f, 0.55f, 0.9f));
                return go;
            }, out _);
            var light = flash.GetComponent<Light>();
            light.range = MuzzleShape.Range(type);
            light.intensity = MuzzleShape.Intensity(type) * scale;
            flash.transform.GetChild(0).localScale = Vector3.one * (MuzzleShape.Scale(type) * scale);
            Fade(flash).Arm(Vector3.one, 0.45f, MuzzleShape.Hold(type));
        }

        private static void SmokePuff(Vector3 position)
        {
            Spray(VfxEvent.MuzzleSmoke, position, 0.35f, 0.6f, 0.18f, new Color(0.35f, 0.32f, 0.28f, 0.55f), 0f, 8, 6);
        }

        public static void Tracer(Vector3 from, Vector3 to)
        {
            var tracer = VfxPool.Rent(VfxEvent.Tracer, from, Quaternion.identity, () =>
            {
                var go = new GameObject("Tracer");
                var built = go.AddComponent<LineRenderer>();
                built.positionCount = 2;
                built.useWorldSpace = true;
                built.startWidth = 0.035f;
                built.endWidth = 0.01f;
                built.sharedMaterial = SpriteMaterial();
                built.startColor = new Color(1f, 0.86f, 0.45f, 0.95f);
                built.endColor = new Color(1f, 0.45f, 0.15f, 0.1f);
                built.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                built.receiveShadows = false;
                return go;
            }, out _);
            var line = tracer.GetComponent<LineRenderer>();
            line.SetPosition(0, from);
            line.SetPosition(1, to);
        }

        private static void Shell(Vector3 position, Vector3 eject, WeaponType type)
        {
            var shell = VfxPool.Rent(VfxEvent.Shell, position, Quaternion.identity, () =>
            {
                var go = Primitive(PrimitiveType.Cube, "Shell", true);
                Tint(go.GetComponent<Renderer>(), new Color(0.72f, 0.58f, 0.22f));
                var built = go.AddComponent<Rigidbody>();
                built.mass = 0.02f;
                built.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                go.AddComponent<BrassDrop>();
                return go;
            }, out _);
            bool hull = type == WeaponType.Shotgun;
            shell.transform.localScale = hull ? new Vector3(0.04f, 0.04f, 0.09f) : new Vector3(0.03f, 0.03f, 0.07f);
            var body = shell.GetComponent<Rigidbody>();
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            if (eject.sqrMagnitude < 0.01f) eject = Vector3.right;
            body.AddForce((eject.normalized + Vector3.up) * 1.6f, ForceMode.Impulse);
            body.AddTorque(UnityEngine.Random.insideUnitSphere * 0.4f, ForceMode.Impulse);
            shell.GetComponent<BrassDrop>().Arm(BrassCue.Sound(type), BrassCue.Volume(type), type);
        }

        private sealed class BrassDrop : MonoBehaviour
        {
            private float ejectedAt;
            private string sound = "";
            private float volume;
            private bool played;
            private WeaponType kind;

            public void Arm(string id, float gain, WeaponType type)
            {
                ejectedAt = Time.time;
                sound = id ?? "";
                volume = gain;
                kind = type;
                played = false;
            }

            private void Update()
            {
                if (played) return;
                if (!BrassCue.Due(Time.time, ejectedAt)) return;
                played = true;
                if (sound.Length == 0 || volume <= 0f) return;
                OutpostZero.Shell.AudioManager.Instance?.PlayAt(sound, transform.position, volume);
                if (ShellRing.Calls(kind) && OutpostZero.Sensory.NoiseManager.Instance != null)
                    OutpostZero.Sensory.NoiseManager.Instance.EmitNoise(transform.position, ShellRing.Radius, ShellRing.Loud, NoiseType.ShellClink, null);
                Mark(VfxEvent.BrassMark, transform.position + Vector3.down * 0.02f, new Vector3(0.05f, 0.09f, 1f), new Color(0.72f, 0.58f, 0.22f, 0.85f));
            }
        }

        private static void Mark(VfxEvent id, Vector3 at, Vector3 scale, Color color)
        {
            if (VfxPool.PlayAuthored(id, at, Quaternion.Euler(90f, 0f, 0f))) return;
            var mark = VfxPool.Rent(id, at, Quaternion.Euler(90f, 0f, 0f), () =>
            {
                var go = Primitive(PrimitiveType.Quad, id == VfxEvent.BrassMark ? "BrassMark" : "BloodDrip");
                Tint(go.GetComponent<Renderer>(), color);
                return go;
            }, out _);
            mark.transform.localScale = scale;
        }

        public static void Mist(Vector3 point)
        {
            Vector3 at = point + Vector3.up * 0.15f;
            if (!VfxPool.PlayAuthored(VfxEvent.BloodMist, at, Quaternion.identity))
                Spray(VfxEvent.BloodMist, at, 0.35f, 1.6f, 0.16f, new Color(0.55f, 0.08f, 0.07f, 0.7f), -0.4f, 16, 14);
            OutpostZero.Shell.AudioManager.Instance?.PlayAt("mist", point, WoundShow.Mist);
        }

        public static void Drip(Vector3 feet)
        {
            Mark(VfxEvent.BloodDrip, feet + Vector3.up * 0.02f, new Vector3(0.12f, 0.16f, 1f), new Color(0.4f, 0.04f, 0.03f, 0.9f));
        }

        /// <summary>A body going down throws a heavier, slower gout than a hit.</summary>
        public static void Death(Vector3 at, Vector3 direction)
        {
            if (VfxPool.PlayAuthored(VfxEvent.DeathBurst, at, Quaternion.identity)) return;
            var go = Spray(VfxEvent.DeathBurst, at, 0.55f, 2.4f, 0.14f, new Color(0.45f, 0.04f, 0.03f, 0.85f), 1.1f, 28, 0);
            var shot = new ParticleSystem.EmitParams();
            Vector3 push = new Vector3(direction.x, 0f, direction.z);
            push = push.sqrMagnitude > 0.001f ? push.normalized : Vector3.zero;
            var particles = go.GetComponent<ParticleSystem>();
            for (int i = 0; i < 24; i++)
            {
                shot.velocity = (push * 2.2f + UnityEngine.Random.insideUnitSphere * 1.6f + Vector3.up * 1.4f);
                particles.Emit(shot, 1);
            }
        }

        /// <summary>A hit's burst by surface: flesh sprays along the shot, metal sparks, wood splinters, stone dusts.</summary>
        public static void Impact(Vector3 point, string face, float driftX, float driftZ)
        {
            Impact(point, face, driftX, driftZ, VfxBook.ImpactFor(face));
        }

        public static void Impact(Vector3 point, string face, float driftX, float driftZ, VfxEvent id)
        {
            if (id == VfxEvent.None) return;
            Vector3 drift = new Vector3(driftX, 0f, driftZ);
            if (VfxPool.PlayAuthored(id, point, drift.sqrMagnitude > 0.001f ? Quaternion.LookRotation(drift) : Quaternion.identity)) return;
            string sound = StrikeFace.Sound(face);
            Color color = face == "flesh" ? new Color(0.55f, 0.05f, 0.04f)
                : face == "metal" ? new Color(1f, 0.78f, 0.28f)
                : face == "wood" ? new Color(0.62f, 0.42f, 0.18f)
                : new Color(0.55f, 0.52f, 0.48f);
            int emit = sound == "spark" ? 10 : 8;
            bool shot = face == "flesh" && drift.sqrMagnitude > 0.001f;
            var go = Spray(id, point,
                sound == "dust" ? 0.4f : 0.25f,
                sound == "spark" ? 4.5f : sound == "spray" ? 3.2f : 2.2f,
                sound == "splinter" ? 0.12f : 0.08f,
                color, sound == "spark" ? 0.8f : 0.3f, 12, shot ? 0 : emit);
            if (!shot) return;
            var along = new ParticleSystem.EmitParams { velocity = new Vector3(driftX, 0.35f, driftZ) * 6f };
            go.GetComponent<ParticleSystem>().Emit(along, emit);
        }

        public static void Embers(Vector3 origin)
        {
            Vector3 at = origin + Vector3.up * 0.2f;
            if (VfxPool.PlayAuthored(VfxEvent.Embers, at, Quaternion.identity)) return;
            Spray(VfxEvent.Embers, at, 1.1f, 0.7f, 0.05f, new Color(1f, 0.45f, 0.1f, 0.9f), -0.2f, YardGlow.Embers, YardGlow.Embers);
        }

        public static void Sparks(Vector3 origin)
        {
            if (VfxPool.PlayAuthored(VfxEvent.LampSparks, origin, Quaternion.identity)) return;
            Spray(VfxEvent.LampSparks, origin, 0.22f, 2.4f, 0.03f, new Color(1f, 0.9f, 0.45f, 1f), 1.2f, YardGlow.Sparks, YardGlow.Sparks);
        }

        public static void Puff(Vector3 feet, int count, bool wet)
        {
            if (count <= 0) return;
            VfxEvent id = wet ? VfxEvent.FootSplash : VfxEvent.FootDust;
            Vector3 at = feet + Vector3.up * 0.05f;
            if (VfxPool.PlayAuthored(id, at, Quaternion.identity)) return;
            Spray(id, at, wet ? 0.28f : 0.4f, wet ? 1.4f : 0.8f, wet ? 0.1f : 0.14f,
                wet ? new Color(0.62f, 0.74f, 0.82f, 0.7f) : new Color(0.55f, 0.5f, 0.42f, 0.55f), wet ? 0.6f : 0f, 12, count);
        }

        /// <summary>A looping knot of flies over <paramref name="bin"/>; it lives with the bin, so it is not pooled.</summary>
        public static GameObject Flies(Transform bin)
        {
            if (bin == null) return null;
            var entry = VfxLibrary.Active != null ? VfxLibrary.Active.Find(VfxEvent.Flies) : null;
            GameObject go;
            if (entry != null && entry.prefab != null)
            {
                go = Object.Instantiate(entry.prefab, bin, false);
            }
            else
            {
                go = new GameObject("Flies");
                go.transform.SetParent(bin, false);
                var particles = go.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.loop = true;
                main.startLifetime = 2.4f;
                main.startSpeed = 0.35f;
                main.startSize = 0.025f;
                main.startColor = new Color(0.05f, 0.05f, 0.04f, 1f);
                main.maxParticles = FlySwarm.Count;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                var emission = particles.emission;
                emission.rateOverTime = FlySwarm.Count / 2.4f;
                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = FlySwarm.Radius;
                var noise = particles.noise;
                noise.enabled = true;
                noise.strength = 1.2f;
                noise.frequency = 1.6f;
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                if (renderer != null) renderer.sharedMaterial = SpriteMaterial();
            }
            go.name = "Flies";
            go.transform.localPosition = Vector3.up * FlySwarm.Height;
            return go;
        }

        /// <summary>
        /// One-shot particle burst on a pooled system: built with emission off, cleared and re-emitted on
        /// every rent so a reused instance never carries old particles or keeps streaming.
        /// </summary>
        private static GameObject Spray(VfxEvent id, Vector3 at, float life, float speed, float size, Color color, float gravity, int max, int count)
        {
            var go = VfxPool.Rent(id, at, Quaternion.identity, () =>
            {
                var built = new GameObject(id.ToString());
                var system = built.AddComponent<ParticleSystem>();
                system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var setup = system.main;
                setup.loop = false;
                setup.playOnAwake = false;
                var emission = system.emission;
                emission.enabled = false;
                var renderer = built.GetComponent<ParticleSystemRenderer>();
                if (renderer != null) renderer.sharedMaterial = SpriteMaterial();
                return built;
            }, out _);
            var particles = go.GetComponent<ParticleSystem>();
            particles.Clear(true);
            var main = particles.main;
            main.startLifetime = life;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.gravityModifier = gravity;
            main.maxParticles = max;
            if (count > 0) particles.Emit(count);
            return go;
        }

        private static GameObject Primitive(PrimitiveType type, string name, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!keepCollider)
            {
                var collider = go.GetComponent<Collider>();
                if (collider != null) Object.Destroy(collider);
            }
            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = SpriteMaterial();
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            return go;
        }

        /// <summary>Colours a renderer without cloning its material, so pooled effects allocate nothing per play.</summary>
        private static void Tint(Renderer renderer, Color color)
        {
            if (renderer == null) return;
            if (block == null) block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", color);
            block.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(block);
        }

        private static BurstFade Fade(GameObject go)
        {
            var fade = go.GetComponent<BurstFade>();
            return fade != null ? fade : go.AddComponent<BurstFade>();
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

        private sealed class FireSheet : MonoBehaviour
        {
            private float born;
            private Renderer sheet;

            public void Arm(Renderer target)
            {
                born = Time.time;
                sheet = target;
                Update();
            }

            private void Update()
            {
                float life = BlastBall.Life > 0f ? BlastBall.Life : 0.02f;
                float t = (Time.time - born) / life;
                if (t < 0f) t = 0f;
                if (t > 1f) t = 1f;
                float scale = BlastBall.Scale(t);
                transform.localScale = new Vector3(scale, scale, 1f);
                Tint(sheet, FrameColor(BlastBall.Frame(t)));
                var cam = Camera.main;
                if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
            }
        }

        private sealed class BurstFade : MonoBehaviour
        {
            private float target = 1f;
            private float life = 0.3f;
            private float age;
            private Vector3 start;

            public void Arm(Vector3 from, float endScale, float duration)
            {
                target = endScale;
                life = Mathf.Max(0.02f, duration);
                start = from;
                age = 0f;
                transform.localScale = from;
                var tag = GetComponent<VfxReturn>();
                if (tag != null) tag.Arm(tag.Id, Time.time + life + 0.05f);
            }

            private void Update()
            {
                age += Time.deltaTime;
                float t = Mathf.Clamp01(age / life);
                transform.localScale = Vector3.Lerp(start, Vector3.one * target, t);
                if (age >= life) VfxPool.Release(gameObject);
            }
        }
    }

    /// <summary>How the flies over a dumpster hang: a small cloud a little above the lid.</summary>
    public static class FlySwarm
    {
        public const int Count = 14;
        public const float Radius = 0.45f;
        public const float Height = 1.5f;
    }
}
