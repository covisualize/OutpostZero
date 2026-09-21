using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Tests.EditMode
{
    public class ArtifactAuditTests
    {
        [Serializable]
        private class ManifestFile
        {
            public string[] assets;
            public string[] textures;
            public int textureSize;
        }

        [Test]
        public void ManifestModelsHaveTexturesAndUvs()
        {
            string root = Directory.GetCurrentDirectory();
            string json = File.ReadAllText(Path.Combine(root, "BlenderScripts", "assets.manifest.json"));
            var manifest = JsonUtility.FromJson<ManifestFile>(json);
            Assert.Greater(manifest.assets.Length, 30);
            Assert.AreEqual(128, manifest.textureSize);
            var missing = new List<string>();
            foreach (var relative in manifest.assets)
            {
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
