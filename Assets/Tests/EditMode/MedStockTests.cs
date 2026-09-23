using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class MedStockTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        [Test]
        public void AMedicOpensAStockedMedOnlyForSomebodyHurt()
        {
            Assert.IsTrue(MedStock.Open(2, 1, 0f), "a hurt survivor is worth a med");
            Assert.IsTrue(MedStock.Open(1, 0, 20f), "so is a hurt leader");
            Assert.IsFalse(MedStock.Open(3, 0, 0f), "nobody hurt keeps the stock sealed");
            Assert.IsFalse(MedStock.Open(0, 3, 40f), "an empty shelf has nothing to open");
            Assert.IsFalse(MedStock.Open(1, 0, 0.2f), "a scratch on the leader is not worth one");
        }

        [Test]
        public void TheWorstHurtGetsTheMed()
        {
            Assert.AreEqual(2, MedStock.Worst(new[] { 1, 0, 3, 2 }));
            Assert.AreEqual(0, MedStock.Worst(new[] { 2, 2 }), "a tie goes to the first on the roster");
            Assert.AreEqual(-1, MedStock.Worst(new[] { 0, 0 }));
            Assert.AreEqual(-1, MedStock.Worst(new int[0]));
            Assert.AreEqual(-1, MedStock.Worst(null));
        }

        [Test]
        public void TheMedStockSurvivesASave()
        {
            var data = new SaveGameData { meds = 4 };
            Assert.IsTrue(SaveCodec.TryDeserialize(SaveCodec.Serialize(data), out var back, out var error), error);
            Assert.AreEqual(4, back.meds);
            Assert.IsTrue(SaveCodec.TryDeserialize("{\"schemaVersion\":1}", out var legacy, out error), error);
            Assert.AreEqual(0, legacy.meds, "an old save starts with an empty shelf");
            string save = Read("Assets/Scripts/Shell/SaveSystem.cs");
            StringAssert.Contains("data.meds = ColonyStorage.Instance.Meds;", save);
            StringAssert.Contains("ColonyStorage.Instance?.SetMeds(data.meds);", save);
        }

        [Test]
        public void TheClinicTakesTheCampShelfFirstThenThePack()
        {
            Assert.AreEqual(4, FactionQuest.Shelf(10, 4), "four on the shelf leave six for the pack");
            Assert.AreEqual(10, FactionQuest.Shelf(10, 15), "a full shelf covers the whole delivery");
            Assert.AreEqual(0, FactionQuest.Shelf(10, 0));
            Assert.AreEqual(0, FactionQuest.Shelf(0, 5));
            string trade = Read("Assets/Scripts/Colony/FactionTrade.cs");
            StringAssert.Contains("if (rest > 0 && (inventory == null || !inventory.TrySpendMeds(rest)))", trade);
            StringAssert.Contains("if (shelf > 0) storage.TakeMeds(shelf);", trade);
            StringAssert.Contains("key.Add(ColonyStorage.Instance != null ? ColonyStorage.Instance.Meds : 0);", trade);
        }

        [Test]
        public void MedsMoveBetweenThePackAndTheCampAndTheMedicSpendsThem()
        {
            string ui = Read("Assets/Scripts/UI/OutpostInterface.cs");
            StringAssert.Contains("if (inventory.TrySpendMedical(1)) ColonyStorage.Instance?.AddMeds(1);", ui);
            StringAssert.Contains("pack.AddMedicalKits(1);\n                        ColonyStorage.Instance.TakeMeds(1);", ui);
            StringAssert.Contains("key.Add(storage.Meds);", ui);
            string roster = Read("Assets/Scripts/Colony/SurvivorRoster.cs");
            StringAssert.Contains("MedStock.Open(storage.Meds", roster);
            StringAssert.Contains("storage.TakeMeds(1) > 0", roster);
            Assert.AreEqual("Camp meds", Loc.T("camp.meds", "en"));
            Assert.AreEqual("Almacenar", Loc.T("pack.stock", "es"));
        }
    }
}
