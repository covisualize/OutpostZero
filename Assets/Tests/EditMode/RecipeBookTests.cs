using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Colony;

namespace OutpostZero.Tests.EditMode
{
    public class RecipeBookTests
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

        [Test]
        public void EveryRecipeAssetMatchesTheBuiltInTableInBookOrder()
        {
            string book = Read("Assets/Resources/RecipeBook.asset");
            StringAssert.Contains(Guid("Assets/Scripts/Colony/RecipeBook.cs.meta"), book);
            var listed = new List<string>();
            foreach (Match m in Regex.Matches(book, "- \\{fileID: 11400000, guid: (\\w+), type: 2\\}")) listed.Add(m.Groups[1].Value);

            var rows = RecipeTable.BuiltInRows();
            Assert.GreaterOrEqual(rows.Count, 20, "the issue asks for twenty recipes");
            Assert.AreEqual(rows.Count, listed.Count, "one book entry per recipe");
            string script = Guid("Assets/Scripts/Colony/CraftingRecipe.cs.meta");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                string path = "Assets/Data/Recipes/" + row.Id + ".asset";
                Assert.IsTrue(File.Exists(Path.Combine(Root, path)), path);
                Assert.AreEqual(Guid(path + ".meta"), listed[i], row.Id + " is out of place in the book");
                string text = Read(path);
                StringAssert.Contains("guid: " + script, text, row.Id);
                var f = Fields(text);
                Assert.AreEqual(row.Id, f["id"]);
                Assert.AreEqual(row.Label, f["label"], row.Id);
                Assert.AreEqual(row.OutputId, f["outputId"], row.Id);
                Assert.AreEqual(row.OutputCount.ToString(), f["outputCount"], row.Id);
                Assert.AreEqual(row.Cost.Scrap.ToString(), f["scrap"], row.Id);
                Assert.AreEqual(row.Cost.Cloth.ToString(), f["cloth"], row.Id);
                Assert.AreEqual(row.Cost.Chemicals.ToString(), f["chemicals"], row.Id);
                Assert.AreEqual(row.Cost.Tape.ToString(), f["tape"], row.Id);
                Assert.AreEqual(row.Cost.Raw.ToString(), f["raw"], row.Id);
                Assert.AreEqual(row.Cost.Station.ToString(), f["station"], row.Id);
                Assert.AreEqual(row.Cost.Skill ?? "", f["skill"], row.Id);
                Assert.AreEqual(row.Cost.Know ?? "", f["know"], row.Id);
                Assert.AreEqual(row.Cost.Level.ToString(), f["level"], row.Id);
                Assert.AreEqual(row.Tier.ToString(), f["tier"], row.Id);
                Assert.AreEqual(row.Print ?? "", f["blueprint"], row.Id);
            }
            Assert.AreEqual(rows.Count, Directory.GetFiles(Path.Combine(Root, "Assets", "Data", "Recipes"), "*.asset").Length, "no stray recipe assets");
        }

        [Test]
        public void ALoadedBookOverridesTheCodeTablesUntilCleared()
        {
            Assert.IsFalse(RecipeTable.FromAsset, "nothing loads a book in EditMode");
            try
            {
                var rows = RecipeTable.BuiltInRows();
                var bandage = rows.Find(r => r.Id == "bandage");
                bandage.Cost.Scrap = 3;
                bandage.Tier = 2;
                bandage.Print = "dressing";
                var trimmed = new List<RecipeTable.Row> { bandage, rows.Find(r => r.Id == "molotov") };
                RecipeTable.Use(trimmed);

                Assert.IsTrue(RecipeTable.FromAsset);
                Assert.AreEqual(2, CraftingBench.Recipes.Length, "the book decides the bench list");
                Assert.AreEqual(3, CraftingBench.Recipes[0].ScrapCost);
                Assert.IsTrue(CraftBill.TryOf("bandage", out var cost));
                Assert.AreEqual(3, cost.Scrap);
                Assert.AreEqual(2, CraftGate.TierOf("bandage"));
                Assert.AreEqual("print", CraftGate.Deny("bandage", 2, ""));
                Assert.IsTrue(CraftBill.TryOf("pipe_bomb", out var fallback), "an id the book lacks still has its built-in bill");
                Assert.AreEqual(8, fallback.Scrap);

                RecipeTable.Use(new List<RecipeTable.Row>());
                Assert.IsFalse(RecipeTable.FromAsset, "an empty book leaves the built-in list");
            }
            finally
            {
                RecipeTable.Clear();
            }
            Assert.AreSame(CraftingBench.BuiltIn, CraftingBench.Recipes);
            Assert.IsTrue(CraftBill.TryOf("bandage", out var restored));
            Assert.AreEqual(1, restored.Scrap);
            Assert.AreEqual(1, CraftGate.TierOf("bandage"));
        }

        [Test]
        public void TheBenchLoadsTheBookAndTheDataStepSyncsIt()
        {
            StringAssert.Contains("RecipeBook.Ensure();", Read("Assets/Scripts/Colony/CraftingBench.cs"));
            StringAssert.Contains("RecipeBookSync.Sync();", Read("Assets/Scripts/Editor/DefaultDataGenerator.cs"));
        }
    }
}
