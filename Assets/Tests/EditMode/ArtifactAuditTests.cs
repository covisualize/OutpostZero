using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using OutpostZero.Core;
using OutpostZero.EditorTools;

namespace OutpostZero.Tests.EditMode
{
    public class ArtifactAuditTests
    {
        [Serializable]
        private class ManifestEntry
        {
            public string id;
            public string category;
            public string output;
            public string collider;
            public float[] lods;
        }

        [Serializable]
        private class ManifestFile
        {
            public int schema;
            public ManifestEntry[] entries;
            public string[] textures;
            public int textureSize;
        }

        private static ManifestFile LoadManifest(string root)
        {
            string json = File.ReadAllText(Path.Combine(root, "BlenderScripts", "assets.manifest.json"));
            return JsonUtility.FromJson<ManifestFile>(json);
        }

        [Test]
        public void ManifestModelsHaveTexturesAndUvs()
        {
            string root = Directory.GetCurrentDirectory();
            var manifest = LoadManifest(root);
            Assert.AreEqual(2, manifest.schema);
            Assert.Greater(manifest.entries.Length, 30);
            Assert.AreEqual(128, manifest.textureSize);
            var missing = new List<string>();
            foreach (var entry in manifest.entries)
            {
                string relative = "Assets/Models/" + entry.output;
                string path = Path.Combine(root, relative);
                if (!File.Exists(path))
                {
                    missing.Add(relative + " missing fbx");
                    continue;
                }
                byte[] bytes = File.ReadAllBytes(path);
                if (!Contains(bytes, "LayerElementUV")) missing.Add(relative + " missing uvs");
                if (relative.Contains("/Characters/") && !Contains(bytes, "Hips")) missing.Add(relative + " missing hips");
                string stem = relative.Substring(0, relative.Length - 4);
                foreach (var suffix in manifest.textures)
                {
                    string map = Path.Combine(root, stem + "_" + suffix + ".png");
                    if (!File.Exists(map)) missing.Add(stem + "_" + suffix + " missing texture");
                }
            }
            Assert.IsEmpty(missing);
            Assert.IsTrue(File.Exists(Path.Combine(root, ModelPaths.StreetLamp)));
            Assert.IsFalse(File.Exists(Path.Combine(root, "Assets", "Models", "Props", "StreetLamp.fbx")));
        }

        [Test]
        public void EverySidecarMatchesItsManifestEntry()
        {
            string root = Directory.GetCurrentDirectory();
            var problems = new List<string>();
            foreach (var entry in LoadManifest(root).entries)
            {
                string fbx = "Assets/Models/" + entry.output;
                var sidecar = ModelSidecar.Load(Path.Combine(root, fbx));
                if (sidecar == null)
                {
                    problems.Add(fbx + " has no sidecar");
                    continue;
                }
                if (sidecar.id != entry.id) problems.Add(fbx + " sidecar id " + sidecar.id);
                if (sidecar.tris <= 0) problems.Add(fbx + " sidecar has no triangles");
                if (sidecar.materials.Length == 0) problems.Add(fbx + " sidecar has no materials");
                int levels = entry.lods == null || entry.lods.Length == 0 ? 1 : entry.lods.Length;
                if (sidecar.lodTris.Length != levels) problems.Add(fbx + " sidecar lod count");
                if (!File.Exists(ModelSidecar.PathFor(Path.Combine(root, fbx)) + ".meta")) problems.Add(fbx + " sidecar meta");
            }
            Assert.IsEmpty(problems);
        }

        [Test]
        public void SidecarParsesAndFallsBackToABox()
        {
            var sidecar = ModelSidecar.Parse("{\"id\":\"Prop_X\",\"collider\":\"convex\",\"lods\":[1.0,0.5],\"lodTris\":[40,20],\"size\":{\"width\":2,\"depth\":1,\"height\":3}}");
            Assert.IsNotNull(sidecar);
            Assert.AreEqual("convex", ModelSidecar.ColliderOf(sidecar));
            Assert.AreEqual(2, sidecar.lods.Length);
            Assert.AreEqual(3f, sidecar.size.height);
            Assert.AreEqual("box", ModelSidecar.ColliderOf(null));
            Assert.AreEqual("box", ModelSidecar.ColliderOf(ModelSidecar.Parse("{\"id\":\"Y\",\"collider\":\"sphere\"}")));
            Assert.IsNull(ModelSidecar.Parse("{}"));
            Assert.IsNull(ModelSidecar.Parse("not json"));
            Assert.AreEqual("Assets/Models/Props/Prop_Dumpster.meta.json", ModelSidecar.PathFor("Assets/Models/Props/Prop_Dumpster.fbx"));
        }

        [Test]
        public void OnlyLodZeroCountsAsTheCollisionMesh()
        {
            Assert.IsTrue(ModelSidecar.IsLowerLod("Vehicle_Wrecked_Sedan_LOD1"));
            Assert.IsTrue(ModelSidecar.IsLowerLod("Building_Warehouse_Depot_LOD12"));
            Assert.IsFalse(ModelSidecar.IsLowerLod("Vehicle_Wrecked_Sedan_LOD0"));
            Assert.IsFalse(ModelSidecar.IsLowerLod("Vehicle_Wrecked_Sedan"));
            Assert.IsFalse(ModelSidecar.IsLowerLod(null));
        }

        [Test]
        public void MeshCollidersSkipTheDecimatedCopy()
        {
            var root = new GameObject("Sedan");
            try
            {
                var mesh = new Mesh();
                foreach (var name in new[] { "Sedan_LOD0", "Sedan_LOD1" })
                {
                    var child = new GameObject(name);
                    child.transform.SetParent(root.transform);
                    child.AddComponent<MeshFilter>().sharedMesh = mesh;
                    child.AddComponent<MeshRenderer>();
                }
                ModelSidecar.AddCollider(root, "convex");
                var colliders = root.GetComponentsInChildren<MeshCollider>();
                Assert.AreEqual(1, colliders.Length);
                Assert.AreEqual("Sedan_LOD0", colliders[0].gameObject.name);
                Assert.IsTrue(colliders[0].convex);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static bool Contains(byte[] bytes, string needle)
        {
            byte[] pattern = Encoding.ASCII.GetBytes(needle);
            for (int i = 0; i <= bytes.Length - pattern.Length; i++)
            {
                int match = 0;
                while (match < pattern.Length && bytes[i + match] == pattern[match]) match++;
                if (match == pattern.Length) return true;
            }
            return false;
        }
    }
}
