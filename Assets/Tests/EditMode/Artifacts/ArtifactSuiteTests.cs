using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OutpostZero.Core;
using OutpostZero.EditorTools;

namespace OutpostZero.Tests.EditMode.Artifacts
{
    [Category("Artifacts")]
    public class ArtifactSuiteTests
    {
        private static string Root => Directory.GetCurrentDirectory();

        private static void Clean(string title, List<string> problems)
        {
            Assert.IsEmpty(problems, ArtifactSuite.Report(title, problems));
        }

        [Test]
        public void ManifestMatchesDisk()
        {
            var manifest = ArtifactSuite.LoadManifest(Root);
            Assert.Greater(manifest.Entries.Count, 100);
            Clean("manifest", ArtifactSuite.ManifestMatchesDisk(Root, manifest));
        }

        [Test]
        public void UvsAndTextures()
        {
            var manifest = ArtifactSuite.LoadManifest(Root);
            CollectionAssert.AreEqual(new[] { "Albedo", "Normal", "AO", "Mask", "Icon" }, manifest.Textures);
            Clean("textures", ArtifactSuite.UvsAndTextures(Root, manifest));
        }

        [Test]
        public void PrefabExistsForEveryModel()
        {
            Clean("prefabs", ArtifactSuite.PrefabExistsForEveryModel(Root, ArtifactSuite.LoadManifest(Root)));
        }

        [Test]
        public void ScaleAndPivotSane()
        {
            Clean("scale and pivot", ArtifactSuite.ScaleAndPivotSane(Root, ArtifactSuite.LoadManifest(Root)));
        }

        [Test]
        public void BoxCollidersMatchTheBindPose()
        {
            Clean("colliders", ArtifactSuite.BoxCollidersMatchTheBindPose(Root, ArtifactSuite.LoadManifest(Root)));
        }

        [Test]
        public void SurfaceTagsAndLayersFollowTheSidecar()
        {
            Clean("surface tags and layers", ArtifactSuite.SurfaceTagsAndLayers(Root, ArtifactSuite.LoadManifest(Root)));
        }

        [Test]
        public void SurfaceGuessReadsMaterialNames()
        {
            Assert.AreEqual(SurfaceKind.Flesh, SurfaceTag.Guess("Characters", new[] { "Mat_Metal_Buckles" }));
            Assert.AreEqual(SurfaceKind.Concrete, SurfaceTag.Guess("Props", new[] { "Mat_Concrete_Weathered", "Mat_Rebar_Rusted" }));
            Assert.AreEqual(SurfaceKind.Gravel, SurfaceTag.Guess("Props", new[] { "Mat_Sandbag_Canvas" }));
            Assert.AreEqual(SurfaceKind.Wood, SurfaceTag.Guess("Kit", new[] { "Mat_Wood_Planks", "Mat_Wood_Nails" }));
            Assert.AreEqual(SurfaceKind.Concrete, SurfaceTag.Guess("Kit", new[] { "Mat_Item_Grey" }));
            Assert.AreEqual(SurfaceKind.Default, SurfaceTag.Guess("Unknown", null));
            Assert.AreEqual("step_hard", SurfaceTag.StepId(SurfaceKind.Concrete));
            Assert.AreEqual("step", SurfaceTag.StepId(SurfaceKind.Flesh));
            Assert.AreEqual(GameLayers.Enemy, GameLayers.ForAsset("Characters", "Zombie_Brute"));
            Assert.AreEqual(0, GameLayers.ForAsset("Characters", "Survivor_Leader"));
            Assert.AreEqual(GameLayers.Loot, GameLayers.ForAsset("Weapons", "Loot_Flare"));
            Assert.AreEqual(GameLayers.Environment, GameLayers.ForAsset("Kit", "Kit_floor"));
        }

        [Test]
        public void MaterialsResolved()
        {
            Clean("materials", ArtifactSuite.MaterialsResolved(Root, ArtifactSuite.LoadManifest(Root)));
        }

        [Test]
        public void TriangleBudget()
        {
            var budgets = ArtifactSuite.Budgets(Root);
            CollectionAssert.AreEquivalent(new[] { "/Characters/", "/Kit/", "/Props/", "/Weapons/", "/Environment/", "/BaseBuilding/" }, budgets.Select(b => b.Key));
            Clean("triangles", ArtifactSuite.TriangleBudget(Root, ArtifactSuite.LoadManifest(Root)));
        }

        [Test]
        public void ScenePathsResolve()
        {
            var paths = new List<string>(BuildScript.Scenes);
            foreach (var field in typeof(ModelPaths).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(string)) continue;
                string value = field.GetValue(null) as string;
                if (value != null && value.EndsWith(".fbx")) paths.Add(value);
            }
            Assert.Greater(paths.Count, 5);
            Clean("scene paths", ArtifactSuite.ScenePathsResolve(Root, paths));
        }

        [Test]
        public void TheWholeSuiteFinishesWellInsideTheCiBudget()
        {
            var clock = Stopwatch.StartNew();
            var manifest = ArtifactSuite.LoadManifest(Root);
            ArtifactSuite.ManifestMatchesDisk(Root, manifest);
            ArtifactSuite.UvsAndTextures(Root, manifest);
            ArtifactSuite.PrefabExistsForEveryModel(Root, manifest);
            ArtifactSuite.ScaleAndPivotSane(Root, manifest);
            ArtifactSuite.BoxCollidersMatchTheBindPose(Root, manifest);
            ArtifactSuite.MaterialsResolved(Root, manifest);
            ArtifactSuite.TriangleBudget(Root, manifest);
            Assert.Less(clock.Elapsed.TotalSeconds, 30d);
        }

        [Test]
        public void DeletingATextureTurnsTheSuiteRedAndNamesIt()
        {
            using (var copy = new OneModel(Root, "Kit_floor"))
            {
                Assert.IsEmpty(ArtifactSuite.UvsAndTextures(copy.Path, copy.Manifest));
                Assert.IsEmpty(ArtifactSuite.MaterialsResolved(copy.Path, copy.Manifest));
                copy.Delete("Assets/Models/Kit/Kit_floor_Albedo.png");
                copy.Delete("Assets/Models/Kit/Kit_floor_Albedo.png.meta");
                CollectionAssert.Contains(ArtifactSuite.UvsAndTextures(copy.Path, copy.Manifest), "Assets/Models/Kit/Kit_floor_Albedo.png is missing");
                var materials = ArtifactSuite.MaterialsResolved(copy.Path, copy.Manifest);
                Assert.IsTrue(materials.Any(p => p.StartsWith("Assets/Materials/Baked/Kit_floor.mat points at a texture that no longer exists")), string.Join("\n", materials));
            }
        }

        [Test]
        public void RenamingAnFbxTurnsTheSuiteRedAndNamesBothSides()
        {
            using (var copy = new OneModel(Root, "Kit_floor"))
            {
                Assert.IsEmpty(ArtifactSuite.ManifestMatchesDisk(copy.Path, copy.Manifest));
                Assert.IsEmpty(ArtifactSuite.PrefabExistsForEveryModel(copy.Path, copy.Manifest));
                copy.Move("Assets/Models/Kit/Kit_floor.fbx", "Assets/Models/Kit/Kit_floor_old.fbx");
                copy.Move("Assets/Models/Kit/Kit_floor.fbx.meta", "Assets/Models/Kit/Kit_floor_old.fbx.meta");
                var problems = ArtifactSuite.ManifestMatchesDisk(copy.Path, copy.Manifest);
                CollectionAssert.Contains(problems, "Assets/Models/Kit/Kit_floor.fbx is in the manifest but missing on disk");
                CollectionAssert.Contains(problems, "Assets/Models/Kit/Kit_floor_old.fbx is on disk but not in the manifest");
                CollectionAssert.Contains(ArtifactSuite.PrefabExistsForEveryModel(copy.Path, copy.Manifest), "Assets/Prefabs/Kit/Kit_floor.prefab does not come from Assets/Models/Kit/Kit_floor.fbx");
            }
        }

        [Test]
        public void APoseSizedColliderTurnsTheSuiteRed()
        {
            using (var copy = new OneModel(Root, "Kit_floor"))
            {
                Assert.IsEmpty(ArtifactSuite.BoxCollidersMatchTheBindPose(copy.Path, copy.Manifest));
                string prefab = System.IO.Path.Combine(copy.Path, "Assets/Prefabs/Kit/Kit_floor.prefab");
                string text = File.ReadAllText(prefab);
                int at = text.IndexOf("m_Size: {x: ");
                Assert.GreaterOrEqual(at, 0, "Kit_floor should carry a box collider");
                int end = text.IndexOf('}', at);
                File.WriteAllText(prefab, text.Substring(0, at) + "m_Size: {x: 9, y: 9, z: 9" + text.Substring(end));
                var problems = ArtifactSuite.BoxCollidersMatchTheBindPose(copy.Path, copy.Manifest);
                Assert.IsTrue(problems.Any(p => p.StartsWith("Assets/Prefabs/Kit/Kit_floor.prefab collider size.x 9")), string.Join("\n", problems));
            }
        }

        [Test]
        public void MiniJsonReadsWhatThePipelineWrites()
        {
            var node = MiniJson.Object(MiniJson.Parse("{\"a\": [1, 2.5, -3e1], \"b\": {\"c\": \"x\\\"y\\u00e9\"}, \"d\": true, \"e\": null, \"f\": []}"));
            CollectionAssert.AreEqual(new object[] { 1d, 2.5d, -30d }, MiniJson.List(node["a"]));
            Assert.AreEqual("x\"yé", MiniJson.Str(MiniJson.Object(node["b"]), "c"));
            Assert.AreEqual(true, node["d"]);
            Assert.IsNull(node["e"]);
            Assert.IsEmpty(MiniJson.List(node["f"]));
            Assert.Throws<System.FormatException>(() => MiniJson.Parse("{\"a\": 1,}"));
        }

        [Test]
        public void SidecarBoxUsesTheBindPoseInUnityAxes()
        {
            var sidecar = new ModelSidecar { floor = 0f };
            sidecar.size.width = 0.8f;
            sidecar.size.depth = 0.4f;
            sidecar.size.height = 1.8f;
            sidecar.center.x = 0.1f;
            sidecar.center.y = -0.2f;
            Assert.IsTrue(ModelSidecar.Box(sidecar, out var middle, out var extent));
            Assert.AreEqual(0.8f, extent.x, 1e-5f);
            Assert.AreEqual(1.8f, extent.y, 1e-5f);
            Assert.AreEqual(0.4f, extent.z, 1e-5f);
            Assert.AreEqual(-0.1f, middle.x, 1e-5f);
            Assert.AreEqual(0.9f, middle.y, 1e-5f);
            Assert.AreEqual(0.2f, middle.z, 1e-5f);
            sidecar.size.height = 0f;
            Assert.IsFalse(ModelSidecar.Box(sidecar, out _, out _));
            Assert.IsFalse(ModelSidecar.Box(null, out _, out _));
        }

        /// <summary>A throwaway repo root holding one model and everything that points at it.</summary>
        private sealed class OneModel : System.IDisposable
        {
            public readonly string Path;
            public readonly ArtifactSuite.Manifest Manifest;

            public OneModel(string root, string id)
            {
                Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "artifact-suite-" + System.Guid.NewGuid().ToString("N"));
                var full = ArtifactSuite.LoadManifest(root);
                var entry = full.Entries.First(e => e.Id == id);
                var files = new List<string> { entry.Fbx, entry.Fbx + ".meta", entry.Sidecar, entry.Sidecar + ".meta", entry.Prefab, entry.Prefab + ".meta", "Assets/Materials/Baked/" + id + ".mat", "Assets/Materials/Baked/" + id + ".mat.meta", "BlenderScripts/asset_audit.py" };
                foreach (var suffix in full.Textures)
                {
                    files.Add("Assets/Models/" + entry.Stem + "_" + suffix + ".png");
                    files.Add("Assets/Models/" + entry.Stem + "_" + suffix + ".png.meta");
                }
                foreach (var file in files)
                {
                    string target = System.IO.Path.Combine(Path, file);
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target));
                    File.Copy(System.IO.Path.Combine(root, file), target);
                }
                string textures = string.Join(", ", full.Textures.Select(t => "\"" + t + "\""));
                File.WriteAllText(System.IO.Path.Combine(Path, "BlenderScripts", "assets.manifest.json"),
                    "{\"textures\": [" + textures + "], \"textureSize\": " + full.TextureSize + ", \"entries\": [{\"id\": \"" + entry.Id + "\", \"category\": \"" + entry.Category + "\", \"output\": \"" + entry.Output + "\"}]}");
                Manifest = ArtifactSuite.LoadManifest(Path);
            }

            public void Delete(string relative) => File.Delete(System.IO.Path.Combine(Path, relative));

            public void Move(string from, string to) => File.Move(System.IO.Path.Combine(Path, from), System.IO.Path.Combine(Path, to));

            public void Dispose()
            {
                if (Directory.Exists(Path)) Directory.Delete(Path, true);
            }
        }
    }
}
