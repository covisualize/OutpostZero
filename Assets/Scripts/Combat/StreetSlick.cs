using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// Oil on the street stays dark until a shot or a blast crosses it.
    /// The camp trench keeps its own numbers. This slick is the one a barrel leaves.
    /// </summary>
    public static class StreetSlick
    {
        public const float Radius = 1.6f;
        public const float Life = 12f;
        public const float Light = 2.8f;
        public const float Drag = 0.7f;

        public static bool Wet(float age)
        {
            return age >= 0f && age < Life;
        }

        public static bool On(float x, float z, float originX, float originZ)
        {
            float dx = x - originX;
            float dz = z - originZ;
            return dx * dx + dz * dz <= Radius * Radius;
        }

        public static bool Crosses(float x0, float z0, float x1, float z1, float originX, float originZ)
        {
            float bx = x1 - x0;
            float bz = z1 - z0;
            float len2 = bx * bx + bz * bz;
            float t = 0f;
            if (len2 > 0.0001f)
            {
                float ax = x0 - originX;
                float az = z0 - originZ;
                t = -(ax * bx + az * bz) / len2;
                if (t < 0f) t = 0f;
                if (t > 1f) t = 1f;
            }
            float cx = x0 + bx * t - originX;
            float cz = z0 + bz * t - originZ;
            return cx * cx + cz * cz <= Radius * Radius;
        }

        public static bool Near(float x, float z, float originX, float originZ)
        {
            float dx = x - originX;
            float dz = z - originZ;
            return dx * dx + dz * dz <= Light * Light;
        }

        public static float Speed(float speed, bool onSlick)
        {
            if (speed < 0f) speed = 0f;
            if (!onSlick) return speed;
            return speed * Drag;
        }
    }

    /// <summary>The dark patch a broken oil barrel leaves until something lights it.</summary>
    public class OilPatch : MonoBehaviour
    {
        private static readonly List<OilPatch> OpenPatches = new List<OilPatch>();
        private float age;
        private bool lit;

        public static void Leave(Vector3 at)
        {
            var go = new GameObject("OilPatch");
            go.transform.position = at;
            go.AddComponent<OilPatch>();
        }

        public static void Shot(Vector3 from, Vector3 to)
        {
            for (int i = OpenPatches.Count - 1; i >= 0; i--)
            {
                var patch = OpenPatches[i];
                if (patch == null || patch.lit) continue;
                if (!StreetSlick.Wet(patch.age)) continue;
                if (!StreetSlick.Crosses(from.x, from.z, to.x, to.z, patch.transform.position.x, patch.transform.position.z))
                    continue;
                patch.Catch();
            }
        }

        public static bool Covers(float x, float z)
        {
            for (int i = 0; i < OpenPatches.Count; i++)
            {
                var patch = OpenPatches[i];
                if (patch == null || patch.lit) continue;
                if (!StreetSlick.Wet(patch.age)) continue;
                if (StreetSlick.On(x, z, patch.transform.position.x, patch.transform.position.z)) return true;
            }
            return false;
        }

        public static void Blast(Vector3 at)
        {
            for (int i = OpenPatches.Count - 1; i >= 0; i--)
            {
                var patch = OpenPatches[i];
                if (patch == null || patch.lit) continue;
                if (!StreetSlick.Wet(patch.age)) continue;
                if (!StreetSlick.Near(at.x, at.z, patch.transform.position.x, patch.transform.position.z)) continue;
                patch.Catch();
            }
        }

        private void Awake()
        {
            age = 0f;
            if (!OpenPatches.Contains(this)) OpenPatches.Add(this);
            var slick = GameObject.CreatePrimitive(PrimitiveType.Quad);
            slick.name = "StreetSlick";
            var solid = slick.GetComponent<Collider>();
            if (solid != null) Destroy(solid);
            slick.transform.SetParent(transform, false);
            slick.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            slick.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            float width = StreetSlick.Radius * 2f;
            slick.transform.localScale = new Vector3(width, width, 1f);
            var renderer = slick.GetComponent<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.05f, 0.05f, 0.04f, 0.9f);
        }

        private void OnDisable()
        {
            OpenPatches.Remove(this);
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (!StreetSlick.Wet(age)) Destroy(gameObject);
        }

        private void Catch()
        {
            if (lit) return;
            lit = true;
            var fire = new GameObject("FirePatch");
            fire.transform.position = transform.position;
            fire.AddComponent<GroundFire>();
            GameplayFeedback.Toast(OutpostZero.Colony.YardSay.Oil(null));
            Destroy(gameObject);
        }
    }
}
