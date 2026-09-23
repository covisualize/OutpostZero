using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Street marks as pooled URP decal projectors on the procedural atlas: blood on the ground and
    /// the wall behind a kill, surface-matched bullet holes, blast scorches, spreading oil and bloody
    /// boot prints. With no decal material it falls back to flat quads.
    /// </summary>
    public class ImpactDecalPool : MonoBehaviour
    {
        public static ImpactDecalPool Instance { get; private set; }

        private const float WallDepth = 0.3f;
        private const float GroundDepth = 0.5f;

        private readonly List<Decal> pool = new List<Decal>();
        private readonly List<Vector3> stains = new List<Vector3>();
        private Material decalMaterial;
        private Material quadMaterial;
        private bool loaded;
        private int bootSteps;
        private int stamp;

        private class Decal
        {
            public GameObject Object;
            public DecalProjector Projector;
            public Renderer Quad;
            public float Until;
            public float Born;
            public float Full;
            public float Depth;
            public float Retired = -1f;
            public bool Oil;
            public bool Stay;
            public Vector3 At;
            public string Kind;
            public string Cell;

            public bool Live => Object != null && Object.activeSelf && Retired < 0f;
        }

        private void OnEnable()
        {
            Instance = this;
            CombatEvents.OnHit += Spawn;
            CombatEvents.OnKill += OnKill;
            CombatEvents.OnBlast += OnBlast;
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            CombatEvents.OnHit -= Spawn;
            CombatEvents.OnKill -= OnKill;
            CombatEvents.OnBlast -= OnBlast;
        }

        private static int Gore => SettingsService.Instance != null ? SettingsService.Instance.Gore : 1;

        private static int Surfaces => ~(GameLayers.EnemyMask | GameLayers.PlayerMask | GameLayers.LootMask
            | GameLayers.ProjectileMask | GameLayers.InteractableMask | (1 << GameLayers.Corpse) | (1 << 2));

        public void StampBoot(Vector3 at, Vector3 right)
        {
            int gore = Gore;
            bool through = false;
            for (int i = 0; i < stains.Count; i++)
            {
                if (BootPrint.Near(at.x, at.z, stains[i].x, stains[i].z))
                {
                    through = true;
                    break;
                }
            }
            bootSteps = BootPrint.Charge(bootSteps, through, gore);
            if (!BootPrint.Due(bootSteps, gore)) return;
            Vector3 flat = new Vector3(right.x, 0f, right.z);
            if (flat.sqrMagnitude < 0.001f) flat = Vector3.right;
            flat.Normalize();
            float side = BootPrint.Side(bootSteps);
            Ground(at + flat * side, out Vector3 point, out Vector3 normal);
            Vector3 heading = Vector3.Cross(flat, Vector3.up);
            Place(point, normal, DecalAtlas.Footprint, BootPrint.Size(gore), side < 0f ? 0 : 1, heading);
            bootSteps = BootPrint.Spend(bootSteps);
        }

        private void Update()
        {
            bool street = GameManager.Instance == null || MarkStay.OnStreet(GameManager.Instance.CurrentState);
            var player = PlayerRegistry.Current;
            float now = Time.time;
            for (int i = 0; i < pool.Count; i++)
            {
                var decal = pool[i];
                if (decal.Object == null) continue;
                if (decal.Retired >= 0f)
                {
                    float elapsed = now - decal.Retired;
                    if (DecalBudget.Retired(elapsed))
                    {
                        decal.Retired = -1f;
                        decal.Stay = false;
                        decal.Object.SetActive(false);
                        continue;
                    }
                    Apply(decal, decal.Full, DecalBudget.Retire(elapsed) * Opacity(decal, now));
                    continue;
                }
                if (decal.Stay)
                {
                    bool near = player == null || GoreMark.Near(
                        decal.At.x - player.transform.position.x,
                        decal.At.y - player.transform.position.y,
                        decal.At.z - player.transform.position.z);
                    if (!MarkStay.Visible(decal.Kind, street, near))
                    {
                        if (!street) decal.Stay = false;
                        decal.Object.SetActive(false);
                        continue;
                    }
                    if (!decal.Object.activeSelf) decal.Object.SetActive(true);
                    Apply(decal, decal.Full, 1f);
                    continue;
                }
                if (!decal.Object.activeSelf) continue;
                if (decal.Until - now <= 0f)
                {
                    decal.Object.SetActive(false);
                    continue;
                }
                float size = decal.Oil ? GoreMark.Spread(now - decal.Born, decal.Full) : decal.Full;
                Apply(decal, size, Opacity(decal, now));
            }
        }

        private static float Opacity(Decal decal, float now)
        {
            if (decal.Stay) return 1f;
            return GoreMark.FadeScale(decal.Until - now, 1f);
        }

        private void Spawn(Vector3 point, Vector3 normal, GameObject target)
        {
            if (!CloseEnough(point)) return;
            string kind = Choose(target);
            int level = Gore;
            bool blood = kind == DecalAtlas.Blood;
            bool shotgun = CombatEvents.FromWeapon && CombatEvents.LastWeapon == WeaponType.Shotgun;
            int count = GoreMark.Splats(level, shotgun, blood);
            if (count <= 0) return;
            float full = GoreMark.Size(StayKind(kind), level);
            string face = StrikeFace.Of(target);
            if (!blood)
            {
                if (kind == DecalAtlas.Oil || kind == DecalAtlas.Scorch)
                {
                    Ground(point, out point, out normal);
                    Place(point, normal, kind, full, stamp++, Vector3.zero);
                }
                else
                {
                    Place(point, normal, kind, full, stamp++, Vector3.zero);
                }
                Burst(point, face, 0f, 0f);
                return;
            }

            Vector3 shot = new Vector3(CombatEvents.DirX, CombatEvents.DirY, CombatEvents.DirZ);
            BloodDrift.Along(shot.x, shot.y, shot.z, out float driftX, out float driftY, out float driftZ);
            bool streak = BloodDrift.Shows(level, true) && (driftX != 0f || driftZ != 0f);
            Ground(point, out Vector3 floor, out Vector3 up);
            for (int i = 0; i < count; i++)
            {
                GoreMark.Offset(i, out float ox, out float oy);
                Place(floor + new Vector3(ox, 0f, oy), up, DecalAtlas.Blood, full, stamp++, Vector3.zero);
            }
            if (streak)
            {
                for (int s = 1; s <= BloodDrift.Drops; s++)
                {
                    float t = s / (float)BloodDrift.Drops;
                    Ground(point + new Vector3(driftX, 0f, driftZ) * t, out Vector3 drop, out Vector3 dropUp);
                    Place(drop, dropUp, DecalAtlas.Blood, full * 0.7f, stamp++, Vector3.zero);
                }
            }
            if (shot.sqrMagnitude > 0.001f) SplatterBehind(point, shot.normalized, level, shotgun, full);
            Burst(point, face, streak ? driftX : 0f, streak ? driftZ : 0f);
        }

        /// <summary>Blood carried past the body lands on whatever stands behind it along the shot.</summary>
        private void SplatterBehind(Vector3 point, Vector3 shot, int level, bool shotgun, float full)
        {
            int count = BackSplat.Count(level, shotgun);
            Vector3 side = Vector3.Cross(Vector3.up, shot);
            if (side.sqrMagnitude < 0.001f) side = Vector3.right;
            side.Normalize();
            for (int i = 0; i < count; i++)
            {
                BackSplat.Spray(i, count, out float yaw, out float pitch);
                Vector3 ray = Quaternion.AngleAxis(yaw, Vector3.up) * (Quaternion.AngleAxis(pitch, side) * shot);
                if (!Physics.Raycast(point + shot * 0.3f, ray, out RaycastHit hit, BackSplat.Reach, Surfaces, QueryTriggerInteraction.Ignore)) continue;
                Place(hit.point, hit.normal, DecalAtlas.Blood, full * 0.85f, stamp++, Vector3.zero);
                if (i == 0 && BackSplat.Drips(hit.normal.y))
                {
                    Place(hit.point - Vector3.up * BackSplat.DripDrop, hit.normal, DecalAtlas.Drip, full * 1.1f, stamp++, Vector3.up);
                }
            }
        }

        private void OnKill(GameObject victim, GameObject killer)
        {
            if (victim == null) return;
            int level = Gore;
            if (GoreMark.Splats(level, false, true) <= 0) return;
            if (victim.GetComponentInParent<OutpostZero.AI.ZombieAI>() == null && victim.name.IndexOf("Zombie") < 0) return;
            if (!CloseEnough(victim.transform.position)) return;
            Ground(victim.transform.position, out Vector3 point, out Vector3 normal);
            Place(point, normal, DecalAtlas.Blood, GoreMark.Size("blood", level) * 1.8f, stamp++, Vector3.zero);
        }

        private void OnBlast(Vector3 at, float radius)
        {
            if (!CloseEnough(at)) return;
            Ground(at, out Vector3 point, out Vector3 normal);
            Place(point, normal, DecalAtlas.Scorch, BlastScorch.SizeFor(radius), stamp++, Vector3.zero);
            for (int i = 0; i < 3; i++)
            {
                GoreMark.Offset(i + 1, out float ox, out float oy);
                Burst(point + new Vector3(ox, 0.1f, oy) * 6f, "concrete", 0f, 0f);
            }
        }

        private static bool CloseEnough(Vector3 point)
        {
            var player = PlayerRegistry.Current;
            if (player == null) return true;
            Vector3 delta = point - player.transform.position;
            return GoreMark.Near(delta.x, delta.y, delta.z);
        }

        private static void Ground(Vector3 at, out Vector3 point, out Vector3 normal)
        {
            if (Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, BlastScorch.Probe, Surfaces, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                normal = hit.normal;
                return;
            }
            point = at;
            normal = Vector3.up;
        }

        /// <summary>
        /// <paramref name="up"/> fixes which way the texture's top faces (drips run down a wall, prints
        /// point where the boot walked); zero picks an up from the seed so repeats don't line up.
        /// </summary>
        private void Place(Vector3 point, Vector3 normal, string kind, float full, int seed, Vector3 up)
        {
            string stay = StayKind(kind);
            if (kind == DecalAtlas.Blood) Remember(point);
            if (normal.sqrMagnitude < 0.001f) normal = Vector3.up;
            normal.Normalize();
            if (up.sqrMagnitude < 0.001f)
            {
                bool wall = Mathf.Abs(normal.y) < 0.5f;
                float spin = (seed * 137.508f) % 360f;
                up = wall ? Quaternion.AngleAxis(kind == DecalAtlas.Blood ? spin * 0.1f - 18f : spin, normal) * Vector3.up
                    : Quaternion.AngleAxis(spin, Vector3.up) * Vector3.forward;
            }
            Vector3.OrthoNormalize(ref normal, ref up);

            var decal = Rent();
            decal.Object.transform.rotation = Quaternion.LookRotation(-normal, up);
            decal.Depth = Mathf.Abs(normal.y) < 0.5f ? WallDepth : GroundDepth;
            decal.Object.transform.position = decal.Projector != null ? point : point + normal * 0.02f;
            decal.Full = full;
            decal.Oil = kind == DecalAtlas.Oil;
            decal.Born = Time.time;
            decal.Retired = -1f;
            decal.Kind = stay;
            decal.Cell = kind;
            decal.Stay = MarkStay.Holds(stay);
            decal.At = point;
            float life = MarkStay.Life(stay);
            decal.Until = decal.Stay ? 0f : Time.time + life;
            Paint(decal, kind, DecalAtlas.Cell(kind, seed));
            Apply(decal, decal.Oil ? GoreMark.Spread(0f, full) : full, 1f);
            decal.Object.SetActive(true);
        }

        /// <summary>The MarkStay family of an atlas kind: holes and scorches hold, the rest fade.</summary>
        public static string StayKind(string kind)
        {
            if (kind == DecalAtlas.HoleConcrete || kind == DecalAtlas.HoleMetal || kind == DecalAtlas.HoleWood) return "hole";
            if (kind == DecalAtlas.Scorch) return "scorch";
            if (kind == DecalAtlas.Oil) return "oil";
            return "blood";
        }

        private void Remember(Vector3 point)
        {
            stains.Add(point);
            if (stains.Count > 32) stains.RemoveAt(0);
        }

        private static string Choose(GameObject target)
        {
            if (target == null) return DecalAtlas.HoleConcrete;
            string name = target.name;
            if (name.Contains("Oil")) return DecalAtlas.Oil;
            if (name.Contains("Barrel") || name.Contains("Explosive")) return DecalAtlas.Scorch;
            string face = StrikeFace.Of(target);
            if (face == "flesh") return DecalAtlas.Blood;
            return DecalAtlas.HoleFor(face);
        }

        private static void Apply(Decal decal, float size, float opacity)
        {
            if (decal.Projector != null)
            {
                decal.Projector.size = new Vector3(size, size, decal.Depth);
                decal.Projector.fadeFactor = opacity;
                return;
            }
            float shown = size * (0.35f + 0.65f * opacity);
            decal.Object.transform.localScale = new Vector3(shown, shown, shown);
        }

        private static Color Flat(string kind)
        {
            if (kind == DecalAtlas.Blood || kind == DecalAtlas.Drip || kind == DecalAtlas.Footprint) return new Color(0.45f, 0.05f, 0.04f, 0.9f);
            if (kind == DecalAtlas.Oil) return new Color(0.08f, 0.08f, 0.07f, 0.85f);
            if (kind == DecalAtlas.Scorch) return new Color(0.12f, 0.1f, 0.08f, 0.9f);
            return new Color(0.22f, 0.2f, 0.18f, 0.8f);
        }

        private static void Paint(Decal decal, string kind, int cell)
        {
            if (decal.Projector != null)
            {
                if (cell < 0) cell = 0;
                decal.Projector.uvScale = new Vector2(DecalAtlas.ScaleU, DecalAtlas.ScaleV);
                decal.Projector.uvBias = new Vector2(DecalAtlas.BiasU(cell), DecalAtlas.BiasV(cell));
                return;
            }
            if (decal.Quad == null) return;
            var block = new MaterialPropertyBlock();
            Color color = Flat(kind);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            decal.Quad.SetPropertyBlock(block);
        }

        private void Burst(Vector3 point, string face, float driftX, float driftZ)
        {
            var go = new GameObject("ImpactBurst");
            go.transform.position = point;
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            string sound = StrikeFace.Sound(face);
            main.startLifetime = sound == "dust" ? 0.4f : 0.25f;
            main.startSpeed = sound == "spark" ? 4.5f : sound == "spray" ? 3.2f : 2.2f;
            main.startSize = sound == "splinter" ? 0.12f : 0.08f;
            main.startColor = face == "flesh" ? new Color(0.55f, 0.05f, 0.04f)
                : face == "metal" ? new Color(1f, 0.78f, 0.28f)
                : face == "wood" ? new Color(0.62f, 0.42f, 0.18f)
                : new Color(0.55f, 0.52f, 0.48f);
            int emit = sound == "spark" ? 10 : 8;
            main.maxParticles = 12;
            if (face == "flesh" && (driftX != 0f || driftZ != 0f))
            {
                var shot = new ParticleSystem.EmitParams();
                shot.velocity = new Vector3(driftX, 0.35f, driftZ) * 6f;
                particles.Emit(shot, emit);
            }
            else particles.Emit(emit);
            Destroy(go, 0.6f);
        }

        private Decal Rent()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                var decal = pool[i];
                if (decal.Object != null && !decal.Object.activeSelf && !decal.Stay && decal.Retired < 0f) return decal;
            }
            int cap = DecalBudget.Cap(QualityProfile.For(SettingsService.Instance != null ? SettingsService.Instance.Quality : 1).Decals);
            int live = 0;
            for (int i = 0; i < pool.Count; i++)
                if (pool[i].Live) live++;
            if (DecalBudget.OverBudget(live, cap)) RetireOldest();
            if (DecalBudget.CanGrow(pool.Count, cap)) return Create();

            Decal faded = null;
            for (int i = 0; i < pool.Count; i++)
            {
                var decal = pool[i];
                if (decal.Object == null || decal.Retired < 0f) continue;
                if (faded == null || decal.Retired < faded.Retired) faded = decal;
            }
            if (faded == null) faded = pool[0];
            faded.Retired = -1f;
            faded.Stay = false;
            faded.Object.SetActive(false);
            return faded;
        }

        private void RetireOldest()
        {
            Decal oldest = null;
            for (int pass = 0; pass < 2 && oldest == null; pass++)
            {
                for (int i = 0; i < pool.Count; i++)
                {
                    var decal = pool[i];
                    if (!decal.Live) continue;
                    if (pass == 0 && decal.Stay) continue;
                    if (oldest == null || decal.Born < oldest.Born) oldest = decal;
                }
            }
            if (oldest != null) oldest.Retired = Time.time;
        }

        private Decal Create()
        {
            if (!loaded)
            {
                loaded = true;
                decalMaterial = Resources.Load<Material>(DecalAtlas.MaterialPath);
            }
            if (decalMaterial != null)
            {
                var go = new GameObject("ImpactDecal");
                go.transform.SetParent(transform, false);
                var projector = go.AddComponent<DecalProjector>();
                projector.material = decalMaterial;
                projector.scaleMode = DecalScaleMode.ScaleInvariant;
                projector.pivot = Vector3.zero;
                projector.drawDistance = GoreMark.Cull;
                go.SetActive(false);
                var made = new Decal { Object = go, Projector = projector };
                pool.Add(made);
                return made;
            }

            if (quadMaterial == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                quadMaterial = new Material(shader);
                quadMaterial.color = new Color(0.35f, 0.05f, 0.04f, 0.85f);
            }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "ImpactDecal";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            var renderer = quad.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = quadMaterial;
            quad.SetActive(false);
            var created = new Decal { Object = quad, Quad = renderer };
            pool.Add(created);
            return created;
        }
    }
}
