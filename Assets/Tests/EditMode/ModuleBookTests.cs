using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Graphics;

namespace OutpostZero.Tests.EditMode
{
    public class ModuleBookTests
    {
        private static string Root => Directory.GetCurrentDirectory();

        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Root, relative)).Replace("\r\n", "\n");
        }

        private static string Guid(string metaRelative)
        {
            return Regex.Match(Read(metaRelative), "guid: (\\w+)").Groups[1].Value;
        }

        private static Dictionary<string, string> Fields(string asset)
        {
            var fields = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(asset, "^  (\\w+): ?(.*)$", RegexOptions.Multiline)) fields[m.Groups[1].Value] = m.Groups[2].Value.Trim();
            return fields;
        }

        private static float Part(string inline, string key)
        {
            var m = Regex.Match(inline, "\\b" + key + ": ([-0-9.eE]+)");
            Assert.IsTrue(m.Success, key + " in " + inline);
            return float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        [Test]
        public void EveryModuleAssetMatchesTheBuiltInTableInBookOrder()
        {
            string book = Read("Assets/Resources/ModuleBook.asset");
            StringAssert.Contains(Guid("Assets/Scripts/Colony/ModuleBook.cs.meta"), book);
            var listed = new List<string>();
            foreach (Match m in Regex.Matches(book, "- \\{fileID: 11400000, guid: (\\w+), type: 2\\}")) listed.Add(m.Groups[1].Value);

            var rows = ModuleTable.BuiltInRows();
            Assert.AreEqual(System.Enum.GetValues(typeof(ModuleKind)).Length, rows.Count, "one row per module kind");
            Assert.AreEqual(rows.Count, listed.Count, "one book entry per module");
            string script = Guid("Assets/Scripts/Colony/BuildingModuleDefinition.cs.meta");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                string path = "Assets/Data/Modules/" + row.Id + ".asset";
                Assert.IsTrue(File.Exists(Path.Combine(Root, path)), path);
                Assert.AreEqual(Guid(path + ".meta"), listed[i], row.Id + " is out of place in the book");
                string text = Read(path);
                StringAssert.Contains("guid: " + script, text, row.Id);
                var f = Fields(text);
                Assert.AreEqual(row.Id, f["id"]);
                Assert.AreEqual(row.Scrap.ToString(), f["scrap"], row.Id);
                Assert.AreEqual(row.Hours.ToString(), f["buildHours"], row.Id);
                Assert.AreEqual(row.Wears ? "1" : "0", f["wears"], row.Id);
                Assert.AreEqual(((int)row.Family).ToString(), f["surface"], row.Id);
                Assert.AreEqual(row.Size.x, Part(f["size"], "x"), 0.0001f, row.Id);
                Assert.AreEqual(row.Size.y, Part(f["size"], "y"), 0.0001f, row.Id);
                Assert.AreEqual(row.Size.z, Part(f["size"], "z"), 0.0001f, row.Id);
                Assert.AreEqual(row.Tint.r, Part(f["tint"], "r"), 0.0001f, row.Id);
                Assert.AreEqual(row.Tint.g, Part(f["tint"], "g"), 0.0001f, row.Id);
                Assert.AreEqual(row.Tint.b, Part(f["tint"], "b"), 0.0001f, row.Id);
            }
            Assert.AreEqual(rows.Count, Directory.GetFiles(Path.Combine(Root, "Assets", "Data", "Modules"), "*.asset").Length, "no stray module assets");
        }

        [Test]
        public void ALoadedBookOverridesCostHoursAndLookUntilCleared()
        {
            Assert.IsFalse(ModuleTable.FromAsset, "nothing loads a book in EditMode");
            try
            {
                var rows = ModuleTable.BuiltInRows();
                var tower = rows.Find(r => r.Id == "Watchtower");
                tower.Scrap = 30;
                tower.Hours = 0;
                tower.Size = new Vector3(2f, 3f, 2f);
                tower.Family = SurfaceFamily.MetalRusted;
                tower.Tint = Color.red;
                var wall = rows.Find(r => r.Id == "Barricade");
                ModuleTable.Use(new List<ModuleTable.Row> { tower, wall });

                Assert.IsTrue(ModuleTable.FromAsset);
                Assert.AreEqual(30, GridBuilder.Cost(ModuleKind.Watchtower));
                Assert.AreEqual(1, BuildSite.Need("Watchtower"), "a site always takes at least an hour");
                Assert.AreEqual(new Vector3(2f, 3f, 2f), GridBuilder.Scale("Watchtower", 10), "only walls shrink as they wear");
                Assert.AreEqual(SurfaceFamily.MetalRusted, GridBuilder.FamilyFor("Watchtower"));
                Assert.AreEqual(Color.red, GridBuilder.ColorFor("Watchtower"));
                Assert.AreEqual(0.9f, GridBuilder.Scale("Barricade", 50).x, 0.0001f, "a half-worn wall is half as wide");
                Assert.AreEqual(22, GridBuilder.Cost(ModuleKind.Turret), "a kind the book lacks keeps its built-in cost");
                Assert.AreEqual(5, BuildSite.Need("Turret"));
            }
            finally
            {
                ModuleTable.Clear();
            }
            Assert.AreEqual(16, GridBuilder.Cost(ModuleKind.Watchtower));
            Assert.AreEqual(4, BuildSite.Need("Watchtower"));
            Assert.AreEqual(SurfaceFamily.Plywood, GridBuilder.FamilyFor("Watchtower"));
            Assert.AreEqual(new Vector3(1.8f, 1.1f, 0.4f), GridBuilder.Scale("Barricade", 100));
        }

        [Test]
        public void TheYardLoadsTheBookAndTheDataStepSyncsIt()
        {
            StringAssert.Contains("ModuleBook.Ensure();", Read("Assets/Scripts/Colony/GridBuilder.cs"));
            StringAssert.Contains("ModuleBookSync.Sync();", Read("Assets/Scripts/Editor/DefaultDataGenerator.cs"));
        }
    }
}
