#if UNITY_EDITOR
using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// The &lt;id&gt;.meta.json that BlenderScripts/pipeline.py writes beside every FBX:
    /// collider kind, pivot, LOD ratios, triangle counts, size, and source material names.
    /// </summary>
    [Serializable]
    public class ModelSidecar
    {
        public const string Suffix = ".meta.json";

        [Serializable]
        public class Size
        {
            public float width;
            public float depth;
            public float height;
        }

        public string id;
        public string category;
        public string generator;
        public string generatorHash;
        public string gitSha;
        public string blender;
        public string[] tags = new string[0];
        public string pivot = "bottom";
        public string collider = "box";
        public string rig = "";
        public float[] lods = { 1f };
        public int tris;
        public int[] lodTris = new int[0];
        public Size size = new Size();
        public float floor;
        public string[] materials = new string[0];

        private static readonly Regex LowerLod = new Regex("_LOD([1-9][0-9]*)$");

        public static string PathFor(string fbxPath)
        {
            string directory = Path.GetDirectoryName(fbxPath)?.Replace('\\', '/');
            string stem = Path.GetFileNameWithoutExtension(fbxPath);
            return string.IsNullOrEmpty(directory) ? stem + Suffix : directory + "/" + stem + Suffix;
        }

        public static ModelSidecar Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var sidecar = JsonUtility.FromJson<ModelSidecar>(json);
                return sidecar != null && !string.IsNullOrEmpty(sidecar.id) ? sidecar : null;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static ModelSidecar Load(string fbxPath)
        {
            string path = PathFor(fbxPath);
            return File.Exists(path) ? Parse(File.ReadAllText(path)) : null;
        }

        public static bool IsLowerLod(string objectName)
        {
            return !string.IsNullOrEmpty(objectName) && LowerLod.IsMatch(objectName);
        }

        public static string ColliderOf(ModelSidecar sidecar)
        {
            string kind = sidecar != null ? sidecar.collider : null;
            return kind == "mesh" || kind == "convex" || kind == "none" ? kind : "box";
        }

        /// <summary>
        /// Adds the collider the sidecar asks for. Only LOD0 (or an un-LODded mesh) collides,
        /// so decimated copies never double the physics shape.
        /// </summary>
        public static void AddCollider(GameObject root, string kind, bool forceConvex = false)
        {
            if (root == null || kind == "none") return;
            if (kind == "box")
            {
                var renderers = root.GetComponentsInChildren<Renderer>();
                var box = root.AddComponent<BoxCollider>();
                bool any = false;
                var bounds = new Bounds();
                foreach (var renderer in renderers)
                {
                    if (IsLowerLod(renderer.gameObject.name)) continue;
                    if (!any) bounds = renderer.bounds;
                    else bounds.Encapsulate(renderer.bounds);
                    any = true;
                }
                if (any)
                {
                    box.center = root.transform.InverseTransformPoint(bounds.center);
                    box.size = bounds.size;
                }
                return;
            }
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null || IsLowerLod(filter.gameObject.name)) continue;
                var mesh = filter.gameObject.AddComponent<MeshCollider>();
                mesh.sharedMesh = filter.sharedMesh;
                mesh.convex = kind == "convex" || forceConvex;
            }
        }
    }
}
#endif
