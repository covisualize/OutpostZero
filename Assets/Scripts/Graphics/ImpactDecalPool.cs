using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Combat;

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

        private void Spawn(Vector3 point, Vector3 normal, GameObject target)
        {
            var decal = Rent();
            decal.Object.transform.position = point + normal * 0.02f;
            if (normal.sqrMagnitude > 0.001f)
            {
                decal.Object.transform.rotation = Quaternion.LookRotation(-normal);
            }
            decal.Until = Time.time + 8f;
            decal.Object.SetActive(true);
            Burst(point);
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
