using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.AI;

namespace OutpostZero.Tests.EditMode
{
    public class DifficultyBookTests
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

        private static float F(Dictionary<string, string> f, string key) => float.Parse(f[key], CultureInfo.InvariantCulture);

        [TearDown]
        public void ClearBook()
        {
            DifficultyTable.Clear();
            DifficultyProfile.Active = 2;
        }

        [Test]
        public void ThreeDifficultyAssetsMatchTheBuiltInRowsInBookOrder()
        {
            string book = Read("Assets/Resources/DifficultyBook.asset");
            StringAssert.Contains(Guid("Assets/Scripts/AI/DifficultyBook.cs.meta"), book);
            var listed = new List<string>();
            foreach (Match m in Regex.Matches(book, "- \\{fileID: 11400000, guid: (\\w+), type: 2\\}")) listed.Add(m.Groups[1].Value);
            var rows = DifficultyTable.BuiltInRows();
            Assert.AreEqual(3, rows.Count);
            Assert.AreEqual(3, listed.Count);
            string script = Guid("Assets/Scripts/AI/DifficultyDefinition.cs.meta");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                Assert.AreEqual(i + 1, row.Level);
                string path = "Assets/Data/Difficulty/" + row.Name + ".asset";
                Assert.AreEqual(Guid(path + ".meta"), listed[i], row.Name + " is out of place in the book");
                string text = Read(path);
                StringAssert.Contains("guid: " + script, text, row.Name);
                var f = Fields(text);
                Assert.AreEqual(row.Level.ToString(), f["level"], row.Name);
                Assert.AreEqual(row.Name, f["displayName"]);
                Assert.AreEqual(row.TensionPerTier, F(f, "tensionPerTier"), 0.0001f, row.Name);
                Assert.AreEqual(row.TensionPerDay, F(f, "tensionPerDay"), 0.0001f, row.Name);
                Assert.AreEqual(row.IntervalScale, F(f, "intervalScale"), 0.0001f, row.Name);
                Assert.AreEqual(row.ExtraKillBase.ToString(), f["extraKillBase"], row.Name);
                Assert.AreEqual(row.ExtraKillPerTier.ToString(), f["extraKillPerTier"], row.Name);
                Assert.AreEqual(row.RunnerTier.ToString(), f["runnerTier"], row.Name);
                Assert.AreEqual(row.BruteTier.ToString(), f["bruteTier"], row.Name);
                Assert.AreEqual(row.BuildUpBatch.ToString(), f["buildUpBatch"], row.Name);
                Assert.AreEqual(row.PeakBatch.ToString(), f["peakBatch"], row.Name);
                Assert.AreEqual(row.RelaxBatch.ToString(), f["relaxBatch"], row.Name);
                Assert.AreEqual(row.AliveScale, F(f, "aliveScale"), 0.0001f, row.Name);
                Assert.AreEqual(row.WalkerWeight.ToString(), f["walkerWeight"], row.Name);
                Assert.AreEqual(row.RunnerWeight.ToString(), f["runnerWeight"], row.Name);
                Assert.AreEqual(row.BruteWeight.ToString(), f["bruteWeight"], row.Name);
                Assert.AreEqual(row.CooldownScale, F(f, "cooldownScale"), 0.0001f, row.Name);
                Assert.AreEqual(row.LootScale, F(f, "lootScale"), 0.0001f, row.Name);
            }
            Assert.AreEqual(3, Directory.GetFiles(Path.Combine(Root, "Assets", "Data", "Difficulty"), "*.asset").Length, "no stray difficulty assets");
        }

        [Test]
        public void TheRowsKeepTheOldCurves()
        {
            Assert.AreEqual(0f, DifficultyProfile.For(3, 5, 1).Tension - 4f, 0.001f);
            Assert.AreEqual(0, DifficultyProfile.For(3, 5, 1).ExtraKills);
            Assert.AreEqual("", DifficultyProfile.For(3, 5, 1).Prefer);
            Assert.AreEqual(8f, DifficultyProfile.For(3, 5, 2).Tension, 0.001f);
            Assert.AreEqual(2, DifficultyProfile.For(3, 5, 2).ExtraKills);
            Assert.AreEqual("Brute", DifficultyProfile.For(3, 5, 2).Prefer);
            Assert.AreEqual("Runner", DifficultyProfile.For(2, 5, 2).Prefer);
            Assert.AreEqual(18.5f, DifficultyProfile.For(3, 5, 3).Tension, 0.001f);
            Assert.AreEqual(4, DifficultyProfile.For(3, 5, 3).ExtraKills);
            Assert.AreEqual(0.75f, DifficultyProfile.For(1, 1, 3).Interval, 0.001f);
            Assert.AreEqual(6, DifficultyProfile.Batch(2, 3));
            Assert.AreEqual(1, DifficultyProfile.Batch(1, 1));
            Assert.AreEqual(0, DifficultyProfile.Batch(0, 2));
            Assert.AreSame(DifficultyTable.Of(2), DifficultyTable.Of(0), "a stored 0 is Survivor");
        }

        [Test]
        public void NightmareIsMeasurablyHarderOnEveryAxis()
        {
            var easy = DifficultyTable.Of(1);
            var mid = DifficultyTable.Of(2);
            var hard = DifficultyTable.Of(3);
            Assert.Less(easy.AliveScale, hard.AliveScale + 0.0001f);
            Assert.Greater(easy.CooldownScale, mid.CooldownScale);
            Assert.Greater(mid.CooldownScale, hard.CooldownScale);
            Assert.Greater(easy.LootScale, mid.LootScale);
            Assert.Greater(mid.LootScale, hard.LootScale);
            float Share(DifficultyTable.Row r) => (r.RunnerWeight + r.BruteWeight) / (float)(r.WalkerWeight + r.RunnerWeight + r.BruteWeight);
            Assert.Less(Share(easy), Share(mid));
            Assert.Less(Share(mid), Share(hard));
            Assert.Less(DifficultyProfile.AliveCap(3, 1), DifficultyProfile.AliveCap(3, 3));
            Assert.AreEqual(DifficultyProfile.AliveCap(3), DifficultyProfile.AliveCap(3, 2));
            Assert.LessOrEqual(DifficultyProfile.AliveCap(0, 3), DifficultyProfile.LowTierAlive);
            Assert.AreEqual(5f, DifficultyProfile.Cooldown(4f, 1), 0.001f);
            Assert.AreEqual(3.2f, DifficultyProfile.Cooldown(4f, 3), 0.001f);
        }

        [Test]
        public void WeightsPickArchetypesByName()
        {
            var names = new[] { "Zombie_Walker", "Zombie_Runner", "Zombie_Brute" };
            var row = new DifficultyTable.Row { WalkerWeight = 50, RunnerWeight = 30, BruteWeight = 20 };
            Assert.AreEqual(0, DifficultyTable.Pick(names, row, 0f));
            Assert.AreEqual(0, DifficultyTable.Pick(names, row, 0.49f));
            Assert.AreEqual(1, DifficultyTable.Pick(names, row, 0.5f));
            Assert.AreEqual(2, DifficultyTable.Pick(names, row, 0.8f));
            Assert.AreEqual(2, DifficultyTable.Pick(names, row, 0.9999f));
            Assert.AreEqual(-1, DifficultyTable.Pick(names, new DifficultyTable.Row { WalkerWeight = 0, RunnerWeight = 0, BruteWeight = 0 }, 0.3f));
            Assert.AreEqual(-1, DifficultyTable.Pick(new string[0], row, 0.3f));
        }

        [Test]
        public void ScarcityScalesLootOnAverageAndIsSeeded()
        {
            Assert.AreEqual(0, DifficultyTable.Scarce(0, 1.25f, 7));
            Assert.AreEqual(4, DifficultyTable.Scarce(4, 1f, 7));
            Assert.AreEqual(DifficultyTable.Scarce(3, 0.75f, 42), DifficultyTable.Scarce(3, 0.75f, 42));
            int lean = 0, rich = 0;
            for (int salt = 0; salt < 4000; salt++)
            {
                lean += DifficultyTable.Scarce(1, 0.75f, salt);
                rich += DifficultyTable.Scarce(4, 1.25f, salt);
            }
            Assert.AreEqual(3000, lean, 150);
            Assert.AreEqual(20000, rich, 40);
        }

        [Test]
        public void ALoadedBookOverridesTheCodeRows()
        {
            DifficultyTable.Use(new[] { new DifficultyTable.Row { Level = 3, Name = "Nightmare", PeakBatch = 9, LootScale = 0.5f, CooldownScale = 0.5f } });
            Assert.IsTrue(DifficultyTable.FromAsset);
            Assert.AreEqual(9, DifficultyProfile.Batch(2, 3));
            Assert.AreEqual(2f, DifficultyProfile.Cooldown(4f, 3), 0.001f);
            Assert.AreEqual(4, DifficultyProfile.Batch(2, 2), "levels the book lacks keep the code row");
            DifficultyTable.Clear();
            Assert.AreEqual(6, DifficultyProfile.Batch(2, 3));
        }

        [Test]
        public void TheSpawnerCooldownAndLootReadTheActiveDifficulty()
        {
            string spawner = Read("Assets/Scripts/AI/ZombieSpawner.cs");
            StringAssert.Contains("DifficultyTable.Pick(VariantNames(), DifficultyTable.Of(DifficultyProfile.Active)", spawner);
            string ai = Read("Assets/Scripts/AI/ZombieAI.cs");
            StringAssert.Contains("DifficultyProfile.Cooldown(SpecialBeat.Cooldown, DifficultyProfile.Active)", ai);
            Assert.IsFalse(ai.Contains("Time.time + SpecialBeat.Cooldown"), "every cooldown goes through the difficulty");
            string loot = Read("Assets/Scripts/Items/LootContainer.cs");
            StringAssert.Contains("DifficultyTable.Scarce(", loot);
            string director = Read("Assets/Scripts/AI/HordeDirector.cs");
            StringAssert.Contains("DifficultyProfile.AliveCap(quality, runDifficulty)", director);
            StringAssert.Contains("DifficultyBookSync.Sync();", Read("Assets/Scripts/Editor/DefaultDataGenerator.cs"));
        }
    }
}
