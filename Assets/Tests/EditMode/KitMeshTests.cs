using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Core;
using OutpostZero.Expedition;
using OutpostZero.Tests.EditMode.Artifacts;
using UnityEngine;

namespace OutpostZero.Tests.EditMode
{
    public class KitMeshTests
    {
        static string Root => Directory.GetCurrentDirectory();

        static KitBook Book()
        {
            var root = MiniJson.Object(MiniJson.Parse(File.ReadAllText(Path.Combine(Root, "Assets", "Resources", "KitCatalog.json"))));
            var pieces = MiniJson.List(root["pieces"]).Select(MiniJson.Object).Select(p => new KitPiece
            {
                id = MiniJson.Str(p, "id"),
                surface = MiniJson.Str(p, "surface"),
                w = (float)MiniJson.Num(p, "w"),
                h = (float)MiniJson.Num(p, "h"),
                d = (float)MiniJson.Num(p, "d"),
                colliders = MiniJson.List(p["colliders"]).Select(MiniJson.Object).Select(b => new KitBox
                {
                    x = (float)MiniJson.Num(b, "x"),
                    y = (float)MiniJson.Num(b, "y"),
                    z = (float)MiniJson.Num(b, "z"),
                    w = (float)MiniJson.Num(b, "w"),
                    h = (float)MiniJson.Num(b, "h"),
                    d = (float)MiniJson.Num(b, "d"),
                }).ToArray()
            }).ToArray();
            return new KitBook { pieces = pieces };
        }

        static KitPiece Piece(string id, string surface, params KitBox[] boxes) =>
            new KitPiece { id = id, surface = surface, w = 2f, h = 3f, d = 0.2f, colliders = boxes };

        [Test]
        public void PanesKeepTheBoxBuild()
        {
            var book = Book();
            Assert.IsFalse(KitPlan.UsesMesh(KitPlan.Find(book.pieces, "wall_window")));
            Assert.IsTrue(KitPlan.UsesMesh(KitPlan.Find(book.pieces, "wall_door")));
            Assert.IsTrue(KitPlan.UsesMesh(KitPlan.Find(book.pieces, "shelf")));
            Assert.IsFalse(KitPlan.UsesMesh(null));
            Assert.IsFalse(KitPlan.UsesMesh(Piece("empty", "concrete")));
        }

        [Test]
        public void ShadeLandsTheBakedAlbedoOnTheDistrictTint()
        {
            var wall = Piece("wall_plain", "concrete", new KitBox { w = 2f, h = 3f, d = 0.2f });
            foreach (var variant in new[] { "brick", "plaster", "concrete" })
            {
                var shade = KitPlan.Shade(wall, variant);
                var baked = KitPlan.Albedo("concrete");
                var want = KitPlan.Tint(wall, variant);
                Assert.AreEqual(want.r, baked.r * shade.r, 0.001f, variant);
                Assert.AreEqual(want.g, baked.g * shade.g, 0.001f, variant);
                Assert.AreEqual(want.b, baked.b * shade.b, 0.001f, variant);
            }
            var shelf = Piece("shelf", "wood");
            Assert.AreEqual(Color.white, KitPlan.Shade(shelf, "brick"));
        }

        [Test]
        public void AlbedoMatchesGenerateKit()
        {
            string py = File.ReadAllText(Path.Combine(Root, "BlenderScripts", "generate_kit.py"));
            var row = new Regex(@"""(\w+)"":\s*\(([\d.]+),\s*([\d.]+),\s*([\d.]+),");
            var rows = row.Matches(py).Cast<Match>().ToArray();
            Assert.GreaterOrEqual(rows.Length, 4);
            foreach (var m in rows)
            {
                var c = KitPlan.Albedo(m.Groups[1].Value);
                Assert.AreEqual(float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture), c.r, 0.001f, m.Groups[1].Value);
                Assert.AreEqual(float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture), c.g, 0.001f, m.Groups[1].Value);
                Assert.AreEqual(float.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture), c.b, 0.001f, m.Groups[1].Value);
            }
        }

        [Test]
        public void MeshHalfTurnMapsTheImportedBoxOntoTheCatalogBox()
        {
            var box = new Vector3(1.6f, 2.1f, 0.2f);
            var imported = new Vector3(-box.x, box.y, -box.z);
            float yaw = KitPlan.MeshYaw * Mathf.Deg2Rad;
            var turned = new Vector3(
                Mathf.Cos(yaw) * imported.x + Mathf.Sin(yaw) * imported.z,
                imported.y,
                -Mathf.Sin(yaw) * imported.x + Mathf.Cos(yaw) * imported.z);
            Assert.AreEqual(box.x, turned.x, 0.0001f);
            Assert.AreEqual(box.y, turned.y, 0.0001f);
            Assert.AreEqual(box.z, turned.z, 0.0001f);
        }

        [Test]
        public void AssemblerSnapsToTheGrid()
        {
            Assert.AreEqual(4f, KitPlan.Snap(3.2f, 2f), 0.0001f);
            Assert.AreEqual(-2f, KitPlan.Snap(-2.9f, 2f), 0.0001f);
            Assert.AreEqual(1.37f, KitPlan.Snap(1.37f, 0f), 0.0001f);
            Assert.AreEqual(0, KitPlan.SnapYaw(-360f));
            Assert.AreEqual(270, KitPlan.SnapYaw(-90f));
            Assert.AreEqual(90, KitPlan.SnapYaw(100f));
            Assert.AreEqual(180, KitPlan.SnapYaw(540f));
            var placed = KitPlan.Place("stairs", new Vector3(5.1f, 4.4f, -0.8f), 181f, 2f, 3f);
            Assert.AreEqual("stairs", placed.id);
            Assert.AreEqual(6f, placed.x, 0.0001f);
            Assert.AreEqual(3f, placed.y, 0.0001f);
            Assert.AreEqual(0f, placed.z, 0.0001f);
            Assert.AreEqual(180, placed.yaw);
            Assert.AreEqual("Kit_stairs", KitPlan.PrefabName("stairs"));
        }

        [Test]
        public void KitSurfacesTagFootsteps()
        {
            Assert.AreEqual(SurfaceKind.Metal, KitPlan.Surface("metal"));
            Assert.AreEqual(SurfaceKind.Wood, KitPlan.Surface("wood"));
            Assert.AreEqual(SurfaceKind.Glass, KitPlan.Surface("glass"));
            Assert.AreEqual(SurfaceKind.Concrete, KitPlan.Surface("concrete"));
            Assert.AreEqual(SurfaceKind.Concrete, KitPlan.Surface(null));
        }

        [Test]
        public void PrefabSetNamesEveryCatalogPieceAndItsPrefab()
        {
            var book = Book();
            string set = File.ReadAllText(Path.Combine(Root, "Assets", "Resources", "KitPrefabs.asset"));
            string script = File.ReadAllText(Path.Combine(Root, "Assets", "Scripts", "Expedition", "KitPrefabSet.cs.meta"));
            string scriptGuid = Regex.Match(script, @"guid: (\w+)").Groups[1].Value;
            StringAssert.Contains("guid: " + scriptGuid + ",", set);
            var entry = new Regex(@"- id: (\w+)\n\s+prefab: \{fileID: (-?\d+), guid: (\w+), type: 3\}");
            var entries = entry.Matches(set).Cast<Match>().ToDictionary(m => m.Groups[1].Value, m => m);
            foreach (var piece in book.pieces)
            {
                Assert.IsTrue(entries.ContainsKey(piece.id), piece.id);
                string prefab = Path.Combine(Root, "Assets", "Prefabs", "Kit", KitPlan.PrefabName(piece.id) + ".prefab");
                Assert.IsTrue(File.Exists(prefab), prefab);
                string meta = File.ReadAllText(prefab + ".meta");
                StringAssert.Contains("guid: " + entries[piece.id].Groups[3].Value, meta, piece.id);
                StringAssert.Contains("--- !u!1 &" + entries[piece.id].Groups[2].Value + " stripped", File.ReadAllText(prefab), piece.id);
            }
        }
    }
}
