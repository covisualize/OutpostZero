using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Items;

namespace OutpostZero.Tests.EditMode
{
    public class StallShelfTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        private static int Total(List<KeyValuePair<string, int>> shelf) => shelf.Sum(pair => pair.Value);

        [Test]
        public void TheShelfRollsTheFactionTableMoreOftenAsTheDaysGoOn()
        {
            Assert.AreEqual(1, StallShelf.Rolls(1));
            Assert.AreEqual(1, StallShelf.Rolls(8));
            Assert.AreEqual(2, StallShelf.Rolls(9));
            Assert.AreEqual(3, StallShelf.Rolls(17));
            Assert.AreEqual(4, StallShelf.Rolls(25));
            Assert.AreEqual(StallShelf.RollCap, StallShelf.Rolls(300));

            foreach (string faction in CaravanBook.BuiltInIds)
            {
                var first = StallShelf.Roll(faction, 3, 0);
                CollectionAssert.AreEqual(CaravanBook.Stock(faction), first.Select(pair => pair.Key).ToArray(), faction + " lists its whole table");
                var again = StallShelf.Roll(faction, 3, 0);
                CollectionAssert.AreEqual(first.Select(p => p.Value).ToArray(), again.Select(p => p.Value).ToArray(), "the same day rolls the same shelf");

                int early = 0, late = 0;
                for (int day = 1; day <= 8; day++) early += Total(StallShelf.Roll(faction, day, 0));
                for (int day = 25; day <= 32; day++) late += Total(StallShelf.Roll(faction, day, 0));
                Assert.Greater(late, early * 3, faction + " stocks up as the days go on");
                Assert.Greater(early, 0, faction);
            }
        }

        [Test]
        public void TrustAddsThePremiumAndATableThatIsMissingStocksOncePerRoll()
        {
            var cold = StallShelf.Roll("clinic", 10, CaravanBook.Trusted - 1);
            Assert.IsFalse(cold.Any(pair => pair.Key == "antibiotics"));
            var warm = StallShelf.Roll("clinic", 10, CaravanBook.Trusted);
            Assert.AreEqual(StallShelf.PremiumCount(2), warm.Single(pair => pair.Key == "antibiotics").Value);
            Assert.AreEqual(1, StallShelf.PremiumCount(1));
            Assert.AreEqual(2, StallShelf.PremiumCount(4));

            try
            {
                var rows = FactionTable.BuiltInRows();
                rows.Add(new FactionTable.Row { Id = "raiders", Label = "Road Raiders", Stock = new[] { "ammo_9mm", "flare" }, LootTable = "no_such_table" });
                FactionTable.Use(rows);
                var shelf = StallShelf.Roll("raiders", 17, 0);
                CollectionAssert.AreEqual(new[] { 3, 3 }, shelf.Select(pair => pair.Value).ToArray());
                Assert.AreEqual("stall_clinic", CaravanBook.Table("clinic"));
            }
            finally
            {
                FactionTable.Clear();
            }
        }

        [Test]
        public void SalesComeOffTodaysShelfAndANewDayRestocks()
        {
            var shelf = new List<KeyValuePair<string, int>> { new KeyValuePair<string, int>("medkit", 2), new KeyValuePair<string, int>("bandage", 1) };
            string sold = StallShelf.MarkSold("", 5, "clinic", "medkit");
            Assert.AreEqual("5:clinic:medkit=1", sold);
            Assert.AreEqual(1, StallShelf.Left(shelf, sold, 5, "clinic", "medkit"));
            sold = StallShelf.MarkSold(sold, 5, "clinic", "medkit");
            sold = StallShelf.MarkSold(sold, 5, "clinic", "bandage");
            Assert.AreEqual("5:clinic:medkit=2,bandage=1", sold);
            Assert.AreEqual(0, StallShelf.Left(shelf, sold, 5, "clinic", "medkit"));
            Assert.AreEqual(0, StallShelf.Left(shelf, sold, 5, "clinic", "bandage"));
            Assert.AreEqual(0, StallShelf.Left(shelf, sold, 5, "clinic", "flare"), "an item off the shelf has none");
            Assert.AreEqual(2, StallShelf.Left(shelf, sold, 6, "clinic", "medkit"), "a new day restocks");
            Assert.AreEqual(2, StallShelf.Left(shelf, sold, 5, "caravan", "medkit"), "another faction keeps its own shelf");
            Assert.AreEqual("6:caravan:water=1", StallShelf.MarkSold(sold, 6, "caravan", "water"));
            Assert.AreEqual(sold, StallShelf.MarkSold(sold, 5, "bad id", "water"));
            Assert.AreEqual(0, StallShelf.Sold("5:clinic:medkit=x", 5, "clinic", "medkit"));
        }

        [Test]
        public void EachFactionNamesItsStallTableAndTheTablesSellOnlyWhatTheFactionStocks()
        {
            var ids = new List<string>(LootTables.Ids);
            foreach (string faction in CaravanBook.BuiltInIds)
            {
                string table = StallShelf.CodeTable(faction);
                Assert.AreEqual(StallShelf.TablePrefix + faction, table);
                Assert.Contains(table, ids);
                Assert.IsTrue(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Assets/Data/Loot/" + table + ".asset")), table);
                Assert.AreEqual(table, Regex.Match(Read("Assets/Data/Factions/" + faction + ".asset"), "^  lootTable: (.*)$", RegexOptions.Multiline).Groups[1].Value.Trim(), faction);
                foreach (var entry in LootTables.Entries(table))
                    CollectionAssert.Contains(CaravanBook.CodeStock(faction), entry.itemId, table);
                string guid = Regex.Match(Read("Assets/Data/Loot/" + table + ".asset.meta"), "guid: (\\w+)").Groups[1].Value;
                StringAssert.Contains("guid: " + guid, Read("Assets/Resources/ItemDatabase.asset"), table + " is registered");
            }
        }

        [Test]
        public void TheStallSellsFromTheShelfAndSavesWhatWentToday()
        {
            string trade = Read("Assets/Scripts/Colony/FactionTrade.cs");
            StringAssert.Contains("if (Left(itemId) <= 0)", trade);
            StringAssert.Contains("sold = StallShelf.MarkSold(sold, Day, faction, itemId);", trade);
            string save = Read("Assets/Scripts/Shell/SaveSystem.cs");
            StringAssert.Contains("data.stallSold = FactionTrade.Instance.Sold;", save);
            StringAssert.Contains("RestoreSold(data.stallSold)", save);
            StringAssert.Contains("foreach (var row in faction.Shelf)", Read("Assets/Scripts/UI/OutpostInterface.cs"));
            Assert.AreEqual("Agotado por hoy", StallVoice.SoldOut("es"));
        }
    }
}
