using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Items;

namespace OutpostZero.Tests.EditMode
{
    public class ZombieLootTests
    {
        static string Repo => Directory.GetCurrentDirectory();

        [Test]
        public void EveryArchetypeAssetNamesABuiltInTable()
        {
            foreach (var name in new[] { "Walker", "Runner", "Brute" })
            {
                string yaml = File.ReadAllText(Path.Combine(Repo, "Assets", "Data", "Zombies", name + ".asset"));
                var match = Regex.Match(yaml, @"lootTable: (\w+)");
                Assert.IsTrue(match.Success, name);
                Assert.AreEqual(name.ToLowerInvariant(), match.Groups[1].Value);
                Assert.IsTrue(LootTables.Ids.Contains(match.Groups[1].Value), name);
            }
        }

        [Test]
        public void DropsSkipEmptyRollsAndRepeatForTheSameSalt()
        {
            LootTables.Reset();
            for (int salt = 0; salt < 300; salt++)
            {
                foreach (var table in new[] { LootTables.Walker, LootTables.Runner, LootTables.Brute })
                {
                    var a = LootTables.Drops(table, salt);
                    var b = LootTables.Drops(table, salt);
                    Assert.AreEqual(a.Length, b.Length);
                    for (int i = 0; i < a.Length; i++)
                    {
                        Assert.Greater(a[i].Count, 0);
                        Assert.AreEqual(a[i].ItemId, b[i].ItemId);
                        Assert.IsNotNull(ItemCatalog.Find(a[i].ItemId), a[i].ItemId);
                    }
                }
            }
            Assert.IsEmpty(LootTables.Drops("", 3));
            Assert.IsEmpty(LootTables.Drops("no_such_table", 3));
        }

        [Test]
        public void BrutesAlwaysPayAndWalkersStayNearTheOldScrapRate()
        {
            LootTables.Reset();
            float walkerScrap = 0f;
            for (int salt = 0; salt < 1000; salt++)
            {
                var brute = LootTables.Drops(LootTables.Brute, salt);
                Assert.GreaterOrEqual(brute.Where(g => g.ItemId == "scrap").Sum(g => g.Count), 3);
                walkerScrap += LootTables.Drops(LootTables.Walker, salt).Where(g => g.ItemId == "scrap").Sum(g => g.Count);
            }
            float mean = walkerScrap / 1000f;
            Assert.Greater(mean, 1f);
            Assert.Less(mean, 2f);
        }

        [Test]
        public void ZombieDeathRollsItsArchetypeTable()
        {
            string source = File.ReadAllText(Path.Combine(Repo, "Assets", "Scripts", "AI", "ZombieAI.cs"));
            StringAssert.Contains("lootTable = archetype.lootTable", source);
            StringAssert.Contains("LootTables.Drops(lootTable", source);
        }
    }
}
