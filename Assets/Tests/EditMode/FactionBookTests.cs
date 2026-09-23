using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Colony;

namespace OutpostZero.Tests.EditMode
{
    public class FactionBookTests
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

        private static List<string> Stock(string asset)
        {
            var list = new List<string>();
            var block = Regex.Match(asset, "^  stock:\\n((?:  - .*\\n)*)", RegexOptions.Multiline);
            Assert.IsTrue(block.Success, "stock list");
            foreach (Match m in Regex.Matches(block.Groups[1].Value, "^  - (.*)$", RegexOptions.Multiline)) list.Add(m.Groups[1].Value.Trim());
            return list;
        }

        [Test]
        public void EveryFactionAssetMatchesTheBuiltInTableInSaveOrder()
        {
            string book = Read("Assets/Resources/FactionBook.asset");
            StringAssert.Contains(Guid("Assets/Scripts/Colony/FactionBook.cs.meta"), book);
            var listed = new List<string>();
            foreach (Match m in Regex.Matches(book, "- \\{fileID: 11400000, guid: (\\w+), type: 2\\}")) listed.Add(m.Groups[1].Value);

            var rows = FactionTable.BuiltInRows();
            Assert.AreEqual(CaravanBook.Ids.Length, rows.Count);
            Assert.AreEqual(rows.Count, listed.Count, "one book entry per faction");
            string script = Guid("Assets/Scripts/Colony/FactionDefinition.cs.meta");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                string path = "Assets/Data/Factions/" + row.Id + ".asset";
                Assert.IsTrue(File.Exists(Path.Combine(Root, path)), path);
                Assert.AreEqual(Guid(path + ".meta"), listed[i], row.Id + " is out of place in the book");
                string text = Read(path);
                StringAssert.Contains("guid: " + script, text, row.Id);
                var f = Fields(text);
                Assert.AreEqual(row.Id, f["id"]);
                Assert.AreEqual(row.Label, f["label"], row.Id);
                CollectionAssert.AreEqual(row.Stock, Stock(text), row.Id);
                Assert.AreEqual(row.Premium, f["premium"], row.Id);
                Assert.AreEqual(row.RefuseBelow.ToString(), f["refuseBelow"], row.Id);
                Assert.AreEqual(row.Markup.ToString(), f["markup"], row.Id);
            }
            Assert.AreEqual(rows.Count, Directory.GetFiles(Path.Combine(Root, "Assets", "Data", "Factions"), "*.asset").Length, "no stray faction assets");
        }

        [Test]
        public void ALoadedBookOverridesTheTablesUntilCleared()
        {
            Assert.IsFalse(FactionTable.FromAsset, "nothing loads a book in EditMode");
            try
            {
                var rows = FactionTable.BuiltInRows();
                var clinic = rows.Find(r => r.Id == "clinic");
                clinic.Label = "Mercy Ward";
                clinic.Stock = new[] { "antibiotics" };
                clinic.Premium = "";
                clinic.RefuseBelow = 0;
                clinic.Markup = 400;
                var stranger = new FactionTable.Row { Id = "raiders", Label = "Raiders" };
                FactionTable.Use(new List<FactionTable.Row> { clinic, stranger });

                Assert.IsTrue(FactionTable.FromAsset);
                Assert.IsFalse(FactionTable.TryRow("raiders", out _), "saves have no standing slot for an unknown faction");
                Assert.AreEqual("Mercy Ward", CaravanBook.Display("clinic"));
                CollectionAssert.AreEqual(new[] { "antibiotics" }, CaravanBook.Stock("clinic", 90), "no premium means trust adds nothing");
                Assert.IsTrue(CaravanBook.Refuses("clinic", -1));
                Assert.IsFalse(CaravanBook.Refuses("clinic", 0));
                CaravanBook.Stock("clinic")[0] = "rock";
                Assert.AreEqual("antibiotics", CaravanBook.Stock("clinic")[0], "callers get a copy of the table");
                Assert.AreEqual(CaravanBook.MarkupCeiling, CaravanBook.Markup("clinic"), "a book markup is held to the range");
                Assert.AreEqual(21, CaravanBook.Price("clinic", "medkit", 0, 0));
                Assert.AreEqual(120, CaravanBook.Markup("militia"), "a faction the book lacks keeps its built-in markup");
                Assert.IsTrue(CaravanBook.Refuses("militia", -21), "a faction the book lacks keeps its built-in temper");
                Assert.AreEqual("Iron Militia", CaravanBook.Display("militia"));
            }
            finally
            {
                FactionTable.Clear();
            }
            Assert.AreEqual("The Clinic", CaravanBook.Display("clinic"));
            CollectionAssert.AreEqual(new[] { "medkit", "bandage", "antibiotics" }, CaravanBook.Stock("clinic", CaravanBook.Trusted));
            Assert.IsFalse(CaravanBook.Refuses("clinic", -100));
            Assert.IsFalse(CaravanBook.Refuses("militia", -20));
        }

        [Test]
        public void TheStallLoadsTheBookAndTheDataStepSyncsIt()
        {
            StringAssert.Contains("FactionBook.Ensure();", Read("Assets/Scripts/Colony/FactionTrade.cs"));
            StringAssert.Contains("FactionBookSync.Sync();", Read("Assets/Scripts/Editor/DefaultDataGenerator.cs"));
        }
    }
}
