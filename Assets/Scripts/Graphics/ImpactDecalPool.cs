using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Graphics
{
    public class ImpactDecalPool : MonoBehaviour
    {
        private readonly List<Decal> pool = new List<Decal>();
        private Material material;

        private class Decal
        {
            public GameObject Object;
            public float Until;
            public float Born;
            public float Full;
            public bool Oil;
        }

        private void OnEnable()
        {
            CombatEvents.OnHit += Spawn;
            CombatEvents.OnKill += OnKill;
        }

        private void OnDisable()
        {
            CombatEvents.OnHit -= Spawn;
            CombatEvents.OnKill -= OnKill;
        }

        private void Update()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                var decal = pool[i];
                if (decal.Object == null || !decal.Object.activeSelf) continue;
                float remaining = decal.Until - Time.time;
                if (remaining <= 0f)
                {
                    decal.Object.SetActive(false);
                    continue;
                }
                float size = GoreMark.FadeScale(remaining, decal.Full);
                if (decal.Oil) size = GoreMark.Spread(Time.time - decal.Born, size);
                decal.Object.transform.localScale = new Vector3(size, size, size);
            }
        }

        private enum Mark
        {
            Blood,
            Hole,
            Scorch,
            Oil
        }

        private void Spawn(Vector3 point, Vector3 normal, GameObject target)
        {
            if (!CloseEnough(point)) return;
            var mark = Choose(target);
            int level = SettingsService.Instance != null ? SettingsService.Instance.Gore : 1;
            bool blood = mark == Mark.Blood;
            bool shotgun = CombatEvents.FromWeapon && CombatEvents.LastWeapon == WeaponType.Shotgun;
            int count = GoreMark.Splats(level, shotgun, blood);
            if (count <= 0) return;
            string kind = mark == Mark.Blood ? "blood" : mark == Mark.Scorch ? "scorch" : mark == Mark.Oil ? "oil" : "hole";
            float full = GoreMark.Size(kind, level);
            for (int i = 0; i < count; i++)
            {
                GoreMark.Offset(i, out float ox, out float oy);
                Vector3 at = point;
                if (i > 0)
                {
                    Vector3 right = Vector3.Cross(normal.sqrMagnitude > 0.001f ? normal : Vector3.up, Vector3.forward);
                    if (right.sqrMagnitude < 0.001f) right = Vector3.right;
                    right.Normalize();
                    Vector3 up = Vector3.Cross(right, normal.sqrMagnitude > 0.001f ? normal : Vector3.up);
                    at += right * ox + up * oy;
                }
                Place(at, normal, mark, full);
            }
            Burst(point, StrikeFace.Of(target));
        }

        private void OnKill(GameObject victim, GameObject killer)
        {
            if (victim == null) return;
            int level = SettingsService.Instance != null ? SettingsService.Instance.Gore : 1;
            if (GoreMark.Splats(level, false, true) <= 0) return;
            if (victim.GetComponentInParent<OutpostZero.AI.ZombieAI>() == null && victim.name.IndexOf("Zombie") < 0) return;
            Vector3 point = victim.transform.position;
            if (!CloseEnough(point)) return;
            Place(point, Vector3.up, Mark.Blood, GoreMark.Size("blood", level) * 1.8f);
        }

        private static bool CloseEnough(Vector3 point)
        {
            var player = PlayerRegistry.Current;
            if (player == null) return true;
            Vector3 delta = point - player.transform.position;
            return GoreMark.Near(delta.x, delta.y, delta.z);
        }

        private void Place(Vector3 point, Vector3 normal, Mark mark, float full)
        {
            var decal = Rent();
            decal.Object.transform.position = point + normal * 0.02f;
            if (normal.sqrMagnitude > 0.001f)
            {
                decal.Object.transform.rotation = Quaternion.LookRotation(-normal);
            }
            Paint(decal.Object.GetComponent<Renderer>(), mark);
            decal.Full = full;
            decal.Oil = mark == Mark.Oil;
            decal.Born = Time.time;
            decal.Until = Time.time + (mark == Mark.Scorch ? 14f : 8f);
            decal.Object.transform.localScale = new Vector3(full, full, full);
            decal.Object.SetActive(true);
        }

        private static Mark Choose(GameObject target)
        {
            if (target == null) return Mark.Hole;
            string name = target.name;
            if (name.Contains("Oil")) return Mark.Oil;
            if (name.Contains("Barrel") || name.Contains("Explosive")) return Mark.Scorch;
            if (name.Contains("Zombie") || target.GetComponentInParent<OutpostZero.AI.ZombieAI>() != null) return Mark.Blood;
            return Mark.Hole;
        }

        private static void Paint(Renderer renderer, Mark mark)
        {
            if (renderer == null) return;
            Color color = mark == Mark.Blood ? new Color(0.45f, 0.05f, 0.04f, 0.9f)
                : mark == Mark.Oil ? new Color(0.08f, 0.08f, 0.07f, 0.85f)
                : mark == Mark.Scorch ? new Color(0.12f, 0.1f, 0.08f, 0.9f)
                : new Color(0.22f, 0.2f, 0.18f, 0.8f);
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        private void Burst(Vector3 point, string face)
        {
            var go = new GameObject("ImpactBurst");
            go.transform.position = point;
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = face == "dust" ? 0.4f : 0.25f;
            main.startSpeed = face == "spark" ? 4.5f : face == "flesh" ? 3.2f : 2.2f;
            main.startSize = face == "splinter" ? 0.12f : 0.08f;
            main.startColor = face == "flesh" ? new Color(0.55f, 0.05f, 0.04f)
                : face == "metal" ? new Color(1f, 0.78f, 0.28f)
                : face == "wood" ? new Color(0.62f, 0.42f, 0.18f)
                : new Color(0.55f, 0.52f, 0.48f);
            main.maxParticles = 12;
            particles.Emit(face == "spark" ? 10 : 8);
            Destroy(go, 0.6f);
        }

        private Decal Rent()
        {
            foreach (var decal in pool)
            {
                if (decal.Object != null && !decal.Object.activeSelf) return decal;
            }
            int cap = QualityProfile.For(SettingsService.Instance != null ? SettingsService.Instance.Quality : 1).Decals;
            if (pool.Count >= cap)
            {
                Decal oldest = pool[0];
                for (int i = 1; i < pool.Count; i++)
                {
                    if (pool[i].Until < oldest.Until) oldest = pool[i];
                }
                oldest.Object.SetActive(false);
                return oldest;
            }
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                material = new Material(shader);
                material.color = new Color(0.35f, 0.05f, 0.04f, 0.85f);
            }
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "ImpactDecal";
            Destroy(quad.GetComponent<Collider>());
            quad.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            var renderer = quad.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
            quad.SetActive(false);
            var created = new Decal { Object = quad };
            pool.Add(created);
            return created;
        }
    }
}
