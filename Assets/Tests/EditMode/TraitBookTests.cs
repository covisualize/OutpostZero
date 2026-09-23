using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-55: traits are TraitDefinition assets listed in Resources/TraitBook, held equal to the built-in table.</summary>
    public class TraitBookTests
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

        private static List<string> Clashes(string asset)
        {
            var list = new List<string>();
            if (Regex.IsMatch(asset, "^  clashes: \\[\\]$", RegexOptions.Multiline)) return list;
            var block = Regex.Match(asset, "^  clashes:\\n((?:  - .*\\n)*)", RegexOptions.Multiline);
            Assert.IsTrue(block.Success, "clashes list");
            foreach (Match m in Regex.Matches(block.Groups[1].Value, "^  - (.*)$", RegexOptions.Multiline)) list.Add(m.Groups[1].Value.Trim());
            return list;
        }

        private static string Num(float value) => value.ToString(CultureInfo.InvariantCulture);

        [Test]
        public void EveryTraitAssetMatchesTheBuiltInTableInDrawOrder()
        {
            string book = Read("Assets/Resources/TraitBook.asset");
            StringAssert.Contains(Guid("Assets/Scripts/Colony/TraitBook.cs.meta"), book);
            var listed = new List<string>();
            foreach (Match m in Regex.Matches(book, "- \\{fileID: 11400000, guid: (\\w+), type: 2\\}")) listed.Add(m.Groups[1].Value);

            var rows = TraitTable.BuiltInRows();
            Assert.GreaterOrEqual(rows.Count, 15, "the issue asks for at least fifteen traits");
            Assert.AreEqual(rows.Count, listed.Count, "one book entry per trait");
            string script = Guid("Assets/Scripts/Colony/TraitDefinition.cs.meta");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                string path = "Assets/Data/Traits/" + row.Id.Replace(" ", "") + ".asset";
                Assert.IsTrue(File.Exists(Path.Combine(Root, path)), path);
                Assert.AreEqual(Guid(path + ".meta"), listed[i], row.Id + " is out of place in the book");
                string text = Read(path);
                StringAssert.Contains("guid: " + script, text, row.Id);
                var f = Fields(text);
                Assert.AreEqual(row.Id, f["id"]);
                Assert.AreEqual(row.Key, f["nameKey"], row.Id);
                Assert.AreEqual(row.Combat.ToString(), f["combat"], row.Id);
                Assert.AreEqual(row.Medicine.ToString(), f["medicine"], row.Id);
                Assert.AreEqual(row.Engineering.ToString(), f["engineering"], row.Id);
                Assert.AreEqual(row.Cooking.ToString(), f["cooking"], row.Id);
                Assert.AreEqual(row.Scavenge.ToString(), f["scavenge"], row.Id);
                CollectionAssert.AreEqual(row.Clashes, Clashes(text), row.Id);
                Assert.AreEqual(Num(row.Hunger), f["hunger"], row.Id);
                Assert.AreEqual(Num(row.Aim), f["aim"], row.Id);
                Assert.AreEqual(row.WatchCost.ToString(), f["watchCost"], row.Id);
                Assert.AreEqual(row.WatchPays ? "1" : "0", f["watchPays"], row.Id);
                Assert.AreEqual(row.RestCut.ToString(), f["restCut"], row.Id);
                Assert.AreEqual(row.CookPlate.ToString(), f["cookPlate"], row.Id);
                Assert.AreEqual(Num(row.Warn), f["warn"], row.Id);
                Assert.AreEqual(row.Haul.ToString(), f["haul"], row.Id);
                Assert.AreNotEqual(row.Key, Loc.T(row.Key, "en"), row.Id + " has no English name");
                Assert.AreNotEqual(row.Key, Loc.T(row.Key, "es"), row.Id + " has no Spanish name");
            }
            Assert.AreEqual(rows.Count, Directory.GetFiles(Path.Combine(Root, "Assets", "Data", "Traits"), "*.asset").Length, "no stray trait assets");
        }

        [Test]
        public void TheBuiltInRowsKeepTheOldTraitNumbers()
        {
            Assert.AreEqual(TraitHook.GluttonHunger, TraitHook.HungerDrop("Glutton"), 0.001f);
            Assert.AreEqual(TraitHook.GluttonHunger, TraitHook.HungerDrop("Engineer", "Glutton"), 0.001f);
            Assert.AreEqual(TraitHook.PlainHunger, TraitHook.HungerDrop("Cook"), 0.001f);
            Assert.AreEqual(0.8f, TraitHook.Aim("Brave", "Sharpshooter"), 0.001f);
            Assert.AreEqual(1f, TraitHook.Aim(null), 0.001f);
            Assert.AreEqual(6, TraitHook.WatchCost("Brave", "Cowardly"), "a coward's dread beats a brave face");
            Assert.AreEqual(0, TraitHook.WatchCost("Engineer", "Brave"));
            Assert.AreEqual(2, TraitHook.WatchCost("Engineer"));
            Assert.AreEqual(0, TraitHook.WatchPay("Cook", "Cowardly", 3));
            Assert.AreEqual(3, TraitHook.WatchPay("Brave", 3));
            Assert.AreEqual(1, TraitHook.RestGain("Insomniac", 2));
            Assert.AreEqual(5, TraitHook.RestGain("Insomniac", 8));
            Assert.AreEqual(8, TraitHook.RestGain("Optimist", 8));
            Assert.AreEqual(4, TraitHook.CookPlate("Brave", "Cook", true));
            Assert.AreEqual(0, TraitHook.CookPlate("Cook", false));
            Assert.AreEqual(3, TraitHook.Haul("Loner", "Scrounger", null));
            Assert.AreEqual(0, TraitHook.Haul("Loner", null, null));
            Assert.IsTrue(SurvivorDraw.Clashes("Cowardly", "Brave"));
            Assert.IsTrue(SurvivorDraw.Clashes("Light Sleeper", "Insomniac"));
            Assert.IsTrue(SurvivorDraw.Clashes("Volatile", "Optimist"));
            Assert.IsFalse(SurvivorDraw.Clashes("Cook", "Brave"));
            Assert.AreEqual("Night Owl", Loc.Trait("Night Owl"));
            Assert.AreEqual("Unknown", Loc.Trait("Unknown"));
        }

        [Test]
        public void ALoadedBookRetunesTraitsAndSetsTheDrawUntilCleared()
        {
            Assert.IsFalse(TraitTable.FromAsset, "nothing loads a book in EditMode");
            string before = SurvivorDraw.Signature(SurvivorDraw.Open(1701));
            try
            {
                var rows = TraitTable.BuiltInRows();
                var glutton = rows.Find(r => r.Id == "Glutton");
                glutton.Hunger = 30f;
                glutton.Cooking = 2;
                glutton.Clashes = new[] { "Cook" };
                var sharp = rows.Find(r => r.Id == "Sharpshooter");
                sharp.Aim = 0.7f;
                rows.Reverse();
                TraitTable.Use(rows);

                Assert.IsTrue(TraitTable.FromAsset);
                Assert.AreEqual(30f, TraitHook.HungerDrop("Glutton"), 0.001f);
                Assert.AreEqual(0.7f, TraitHook.Aim("Sharpshooter"), 0.001f);
                Assert.AreEqual(2, TraitTable.Skill("Glutton", "cooking"));
                Assert.IsTrue(SurvivorDraw.Clashes("Cook", "Glutton"), "a clash listed on one side holds both ways");
                Assert.AreEqual("Night Owl", TraitTable.Ids()[0], "book order is draw order");
                Assert.AreNotEqual(before, SurvivorDraw.Signature(SurvivorDraw.Open(1701)));
            }
            finally
            {
                TraitTable.Clear();
            }
            Assert.AreEqual(TraitHook.GluttonHunger, TraitHook.HungerDrop("Glutton"), 0.001f);
            Assert.AreEqual("Steady Hands", TraitTable.Ids()[0]);
            Assert.AreEqual(before, SurvivorDraw.Signature(SurvivorDraw.Open(1701)));
        }

        [Test]
        public void TheRosterLoadsTheBookAndTheDataStepSyncsIt()
        {
            StringAssert.Contains("TraitBook.Ensure();", Read("Assets/Scripts/Colony/SurvivorRoster.cs"));
            StringAssert.Contains("TraitBookSync.Sync();", Read("Assets/Scripts/Editor/DefaultDataGenerator.cs"));
        }
    }
}
