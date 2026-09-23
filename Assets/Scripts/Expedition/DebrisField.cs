using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using OutpostZero.Core;
using OutpostZero.Graphics;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// Draws the street litter with GPU instancing: one batch per debris role, no GameObjects and no
    /// colliders. Bottles are left to StreetDetail because they carry a kick trigger.
    /// </summary>
    public class DebrisField : MonoBehaviour
    {
        private class Batch
        {
            public Mesh mesh;
            public Material material;
            public bool shadows;
            public readonly List<Matrix4x4> matrices = new List<Matrix4x4>();
            public Bounds bounds;
        }

        private readonly Dictionary<string, Batch> batches = new Dictionary<string, Batch>();
        private static readonly Dictionary<PrimitiveType, Mesh> shapes = new Dictionary<PrimitiveType, Mesh>();

        public int Count { get; private set; }

        public static bool Instanced(string role) => role != "bottle";

        public void Add(DressingPlan.Mark mark, PrimitiveType shape, Color color)
        {
            if (!batches.TryGetValue(mark.Role, out var batch))
            {
                batch = new Batch
                {
                    mesh = Shape(shape),
                    material = MaterialFor(mark.Role, color),
                    shadows = mark.Role != "paper" && mark.Role != "glass",
                };
                batch.bounds = new Bounds(new Vector3(mark.X, mark.Y, mark.Z), Vector3.zero);
                batches[mark.Role] = batch;
            }
            if (batch.mesh == null || batch.material == null) return;
            var at = new Vector3(mark.X, mark.Y, mark.Z);
            var turn = Quaternion.Euler(mark.Role == "tyre" ? 90f : 0f, mark.Yaw, 0f);
            batch.matrices.Add(Matrix4x4.TRS(at, turn, new Vector3(mark.W, mark.H, mark.D)));
            batch.bounds.Encapsulate(new Bounds(at, Vector3.one * Mathf.Max(mark.W, Mathf.Max(mark.H, mark.D))));
            Count++;
        }

        private void LateUpdate()
        {
            foreach (var batch in batches.Values)
            {
                if (batch.matrices.Count == 0 || batch.mesh == null || batch.material == null) continue;
                var args = new RenderParams(batch.material)
                {
                    worldBounds = batch.bounds,
                    shadowCastingMode = batch.shadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    receiveShadows = true,
                    layer = GameLayers.Environment,
                };
                UnityEngine.Graphics.RenderMeshInstanced(args, batch.mesh, 0, batch.matrices);
            }
        }

        private void OnDestroy()
        {
            foreach (var batch in batches.Values)
                if (batch.material != null) Destroy(batch.material);
            batches.Clear();
        }

        private static Material MaterialFor(string role, Color color)
        {
            var family = DebrisLook.FamilyFor(role);
            var source = MaterialLibrary.Triplanar(family);
            if (source == null) return null;
            var material = new Material(source) { name = "Debris_" + role, enableInstancing = true };
            var tint = MaterialLibrary.TintFor(color);
            if (material.HasProperty("_Tint")) material.SetColor("_Tint", DebrisLook.Pale(role) ? color : tint);
            return material;
        }

        private static Mesh Shape(PrimitiveType type)
        {
            if (shapes.TryGetValue(type, out var mesh) && mesh != null) return mesh;
            var probe = GameObject.CreatePrimitive(type);
            mesh = probe.GetComponent<MeshFilter>().sharedMesh;
            Destroy(probe);
            shapes[type] = mesh;
            return mesh;
        }
    }

    /// <summary>Surface for each piece of litter; paper has no family of its own, so it tints cloth pale.</summary>
    public static class DebrisLook
    {
        public static SurfaceFamily FamilyFor(string role)
        {
            switch (role)
            {
                case "rubble": return SurfaceFamily.BrickGrey;
                case "brick": return SurfaceFamily.BrickRed;
                case "tyre": return SurfaceFamily.Rubber;
                case "glass": return SurfaceFamily.Glass;
                case "bottle": return SurfaceFamily.Glass;
                case "paper": return SurfaceFamily.Cloth;
                default: return SurfaceFamily.ConcreteCracked;
            }
        }

        public static bool Pale(string role) => role == "paper";
    }
}
