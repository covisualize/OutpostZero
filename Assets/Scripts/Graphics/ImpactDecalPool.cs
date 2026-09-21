using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Combat;
using OutpostZero.Core;

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
        }

        private void OnEnable() => CombatEvents.OnHit += Spawn;
        private void OnDisable() => CombatEvents.OnHit -= Spawn;

        private void Update()
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].Object == null || !pool[i].Object.activeSelf) continue;
                if (Time.time < pool[i].Until) continue;
                pool[i].Object.SetActive(false);
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
            var mark = Choose(target);
            var decal = Rent();
            decal.Object.transform.position = point + normal * 0.02f;
            if (normal.sqrMagnitude > 0.001f)
            {
                decal.Object.transform.rotation = Quaternion.LookRotation(-normal);
            }
            Paint(decal.Object.GetComponent<Renderer>(), mark);
            decal.Until = Time.time + (mark == Mark.Scorch ? 14f : 8f);
            decal.Object.SetActive(true);
            Burst(point);
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
            float scale = mark == Mark.Scorch ? 0.7f : mark == Mark.Blood ? 0.42f : 0.28f;
            renderer.transform.localScale = new Vector3(scale, scale, scale);
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }

        private void Burst(Vector3 point)
        {
            var go = new GameObject("ImpactBurst");
            go.transform.position = point;
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.25f;
            main.startSpeed = 2.5f;
            main.startSize = 0.08f;
            main.maxParticles = 12;
            particles.Emit(8);
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
