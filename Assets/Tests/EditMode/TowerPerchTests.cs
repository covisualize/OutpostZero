using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;

namespace OutpostZero.Tests.EditMode
{
    public class TowerPerchTests
    {
        [Test]
        public void ATowerGuardSeesFurtherAndStandsHigher()
        {
            Assert.AreEqual(GuardVolley.Range * TowerPerch.Reach, TowerPerch.Range(PostBite.Range(0), true), 0.001f);
            Assert.AreEqual(GuardVolley.Range, TowerPerch.Range(PostBite.Range(0), false), 0.001f);
            Assert.AreEqual(GuardVolley.Range * PostBite.Reach * TowerPerch.Reach, TowerPerch.Range(PostBite.Range(1), true), 0.001f);
            Assert.AreEqual(0f, TowerPerch.Range(PostBite.Range(2), true));
            Assert.Greater(TowerPerch.Height(true), TowerPerch.Height(false));
        }

        [Test]
        public void OnlyATowerPostIsPerched()
        {
            var kinds = new[] { "Barricade", "Watchtower" };
            var x = new[] { 0f, 0f };
            var z = new[] { 12f, 0f };
            var sites = new[] { 0, 0 };
            var integrity = new[] { 100, 100 };
            int pick = GuardStand.Pick("gate", kinds, x, z, sites, integrity, 0);
            Assert.AreEqual(kinds[pick] == "Watchtower", GuardStand.Perched("gate", 0, kinds, x, z, sites, integrity));

            var towers = new[] { "Watchtower" };
            Assert.IsTrue(GuardStand.Perched("gate", 0, towers, new[] { 0f }, new[] { 0f }, new[] { 0 }, new[] { 100 }));
            Assert.IsFalse(GuardStand.Perched("gate", 0, towers, new[] { 0f }, new[] { 0f }, new[] { 3 }, new[] { 100 }));
            Assert.IsFalse(GuardStand.Perched("gate", 0, new string[0], new float[0], new float[0], new int[0], new int[0]));
        }

        [Test]
        public void TheRaidVolleyUsesThePerch()
        {
            string source = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets/Scripts/Colony/NightRaidController.cs")).Replace("\r\n", "\n");
            StringAssert.Contains("TowerPerch.Range(PostBite.Range(crew[g].injury), perched)", source);
            StringAssert.Contains("TowerPerch.Height(perched)", source);
            StringAssert.Contains("GuardStand.Perched(side, post", source);
        }
    }
}
