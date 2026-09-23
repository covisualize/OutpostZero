using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;

namespace OutpostZero.Tests.EditMode
{
    public class ModuleHealthTests
    {
        [TearDown]
        public void ClearBook()
        {
            ModuleTable.Clear();
        }

        [Test]
        public void AHundredHpModuleTakesTheBlowAsDealt()
        {
            Assert.AreEqual(100, ModuleHealth.Hp("Barricade"));
            Assert.AreEqual(12, ModuleHealth.Loss(12, 100));
            Assert.AreEqual(88, ModuleHealth.Hit(100, 12, ModuleHealth.Of("Barricade", 0)));
            Assert.AreEqual(0, ModuleHealth.Hit(5, 12, 100));
            Assert.AreEqual(40, ModuleHealth.Hit(40, 0, 100), "no blow, no wear");
        }

        [Test]
        public void TougherModulesLoseLessAndEveryBlowTakesOne()
        {
            Assert.AreEqual(150, ModuleHealth.Hp("Watchtower"));
            Assert.AreEqual(8, ModuleHealth.Loss(12, 150));
            Assert.AreEqual(20, ModuleHealth.Loss(12, 60));
            Assert.AreEqual(1, ModuleHealth.Loss(1, 150));
            Assert.AreEqual(12, ModuleHealth.Loss(12, 0), "a missing hp falls back to 100");
        }

        [Test]
        public void AReinforcedWallStandsHalfAsMuchAgain()
        {
            Assert.AreEqual(150, ModuleHealth.Of("Barricade", ModuleHealth.Reinforced));
            Assert.AreEqual(100, ModuleHealth.Of("Barricade", 1));
            Assert.AreEqual(100, ModuleHealth.Of("Workbench", 2), "the bench tier is its own upgrade");
            Assert.Greater(ModuleHealth.Hit(100, 30, ModuleHealth.Of("Barricade", 2)), ModuleHealth.Hit(100, 30, ModuleHealth.Of("Barricade", 0)));

            Assert.IsTrue(ModuleHealth.CanReinforce("Barricade", 0, 100, 0));
            Assert.IsFalse(ModuleHealth.CanReinforce("Barricade", 1, 100, 0), "a site is not a wall yet");
            Assert.IsFalse(ModuleHealth.CanReinforce("Barricade", 0, 0, 0));
            Assert.IsFalse(ModuleHealth.CanReinforce("Barricade", 0, 100, 2));
            Assert.IsFalse(ModuleHealth.CanReinforce("Lamp", 0, 100, 0));
        }

        [Test]
        public void TheBookSetsHpAndTheYardUsesItForEveryBlow()
        {
            var row = new ModuleTable.Row { Id = "Barricade", Hp = 200 };
            ModuleTable.Use(new[] { row });
            Assert.AreEqual(200, ModuleHealth.Hp("Barricade"));
            Assert.AreEqual(300, ModuleHealth.Of("Barricade", 2));
            ModuleTable.Clear();
            Assert.AreEqual(100, ModuleHealth.Hp("Barricade"));

            string root = Directory.GetCurrentDirectory();
            string grid = File.ReadAllText(Path.Combine(root, "Assets/Scripts/Colony/GridBuilder.cs"));
            Assert.AreEqual(4, System.Text.RegularExpressions.Regex.Matches(grid, "ModuleHealth\\.Hit\\(").Count, "strikes at a spot, strikes from a side, trap chips and storm wear");
            StringAssert.Contains("Reinforce()", grid);
            string save = File.ReadAllText(Path.Combine(root, "Assets/Scripts/Shell/SaveSystem.cs"));
            StringAssert.Contains("tier = module.tier", save);
        }
    }
}
