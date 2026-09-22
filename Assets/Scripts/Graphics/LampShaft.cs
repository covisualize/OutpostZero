using UnityEngine;
using UnityEngine.Rendering;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// The wide flashlight shows a soft cone in the air.
    /// The tip stays bright and the far rim fades out.
    /// </summary>
    public static class LampShaft
    {
        public const float Length = 8f;
        public const int Sides = 12;
        public const float Alpha = 0.22f;

        public static float Radius(float length, float outerDegrees)
        {
            if (length < 0f) length = 0f;
            float half = outerDegrees * 0.5f;
            if (half < 0f) half = 0f;
            if (half > 89f) half = 89f;
            return length * Mathf.Tan(half * Mathf.Deg2Rad);
        }

        public static int VertexCount(int sides)
        {
            if (sides < 3) sides = 3;
            return sides + 1;
        }

        public static int IndexCount(int sides)
        {
            if (sides < 3) sides = 3;
            return sides * 3;
        }

        public static float Fade(float along)
        {
            if (along < 0f) along = 0f;
            if (along > 1f) along = 1f;
            float left = 1f - along;
            return left * left;
        }

        public static Mesh Build(float outerDegrees)
        {
            int sides = Sides;
            float radius = Radius(Length, outerDegrees);
            var mesh = new Mesh { name = "LampShaft" };
            var verts = new Vector3[sides + 1];
            var uv = new Vector2[sides + 1];
            var colors = new Color[sides + 1];
            verts[0] = Vector3.zero;
            uv[0] = new Vector2(0.5f, 0f);
            colors[0] = new Color(1f, 0.96f, 0.88f, 1f);
            for (int i = 0; i < sides; i++)
            {
                float turn = (i / (float)sides) * Mathf.PI * 2f;
                verts[i + 1] = new Vector3(Mathf.Cos(turn) * radius, Mathf.Sin(turn) * radius, Length);
                uv[i + 1] = new Vector2(i / (float)sides, 1f);
                colors[i + 1] = new Color(1f, 0.96f, 0.88f, 0f);
            }
            var tris = new int[sides * 3];
            for (int i = 0; i < sides; i++)
            {
                tris[i * 3] = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = (i + 1) % sides + 1;
            }
            mesh.vertices = verts;
            mesh.uv = uv;
            mesh.colors = colors;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            return mesh;
        }

        public static GameObject Raise(Transform light)
        {
            if (light == null) return null;
            var existing = light.Find("Lamp_Shaft");
            if (existing != null) return existing.gameObject;
            var shaft = new GameObject("Lamp_Shaft");
            shaft.transform.SetParent(light, false);
            var filter = shaft.AddComponent<MeshFilter>();
            filter.sharedMesh = Build(LampCookie.Outer);
            var renderer = shaft.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            var shared = Resources.Load<Material>("OutpostLampCone");
            if (shared != null) renderer.sharedMaterial = shared;
            else
            {
                var shader = Shader.Find("OutpostZero/LampCone");
                if (shader != null) renderer.sharedMaterial = new Material(shader);
            }
            return shaft;
        }
    }

    /// <summary>Top-down shadows use two cascades and a near plane that clears a two-meter body.</summary>
    public static class ShadowRig
    {
        public const int Cascades = 2;
        public const float Near = 0.2f;
    }

    /// <summary>A building wall casts and receives shadows so a lamp does not shine through it.</summary>
    public static class WallSeal
    {
        public static bool Casts(string name)
        {
            return !string.IsNullOrEmpty(name) && name.StartsWith("Building_");
        }

        public static void Seal(GameObject go)
        {
            if (go == null || !Casts(go.name)) return;
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].shadowCastingMode = ShadowCastingMode.On;
                renderers[i].receiveShadows = true;
            }
        }
    }
}
