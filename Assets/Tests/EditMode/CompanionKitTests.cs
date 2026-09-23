using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;

namespace OutpostZero.Tests.EditMode
{
    public class CompanionKitTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        [Test]
        public void OnlyALivingNonLeaderWithAtMostOneWoundCanGo()
        {
            Assert.IsTrue(CompanionKit.Fit(true, false, 0));
            Assert.IsTrue(CompanionKit.Fit(true, false, 1));
            Assert.IsFalse(CompanionKit.Fit(true, false, 2));
            Assert.IsFalse(CompanionKit.Fit(true, true, 0));
            Assert.IsFalse(CompanionKit.Fit(false, false, 0));
        }

        [Test]
        public void TheRationIsOneFoodAndOneWaterFromStorage()
        {
            Assert.IsTrue(CompanionKit.Pack(5, 3, out int food, out int water));
            Assert.AreEqual(1, food);
            Assert.AreEqual(1, water);
            Assert.IsFalse(CompanionKit.Pack(0, 3, out food, out water));
            Assert.AreEqual(0, food);
            Assert.AreEqual(1, water);
            Assert.IsFalse(CompanionKit.Pack(0, 0, out food, out water));
            Assert.AreEqual(0, food + water);
        }

        [Test]
        public void GuardSetsTheShotAndTheCadence()
        {
            Assert.AreEqual(10f, CompanionKit.Damage(0), 0.001f);
            Assert.AreEqual(25f, CompanionKit.Damage(5), 0.001f);
            Assert.Greater(CompanionKit.Cadence(0), CompanionKit.Cadence(5));
            Assert.AreEqual(1.2f, CompanionKit.Cadence(20), 0.001f);
            Assert.IsTrue(CompanionKit.Fires(0f, 3f, 0, 4f));
            Assert.IsFalse(CompanionKit.Fires(0f, 3f, 0, CompanionKit.Range + 1f));
            Assert.IsFalse(CompanionKit.Fires(0f, 3f, 0, -1f));
            Assert.IsFalse(CompanionKit.Fires(2f, 3f, 0, 4f));
            Assert.IsTrue(CompanionKit.Fires(2f, 3f + CompanionKit.Cadence(10), 10, 4f));
        }

        [Test]
        public void BitesComeHomeAndOnlyAReturnCarriesScrap()
        {
            Assert.AreEqual(2, CompanionKit.Wounds(0, 2, false));
            Assert.AreEqual(3, CompanionKit.Wounds(1, 3, true));
            Assert.AreEqual(0, CompanionKit.Wounds(0, -1, false));
            Assert.AreEqual(3, CompanionKit.Haul(4, true));
            Assert.AreEqual(0, CompanionKit.Haul(4, false));
            Assert.AreEqual(0, CompanionKit.Haul(0, true));
        }

        [Test]
        public void TheBoardPicksOneCompanionWhoLeavesAndReturnsWithTheLeader()
        {
            string roster = Read("Assets/Scripts/Colony/SurvivorRoster.cs");
            StringAssert.Contains("survivors[i].task == CompanionKit.Task) survivors[i].task = \"Rest\"", roster);
            StringAssert.Contains("public Survivor PackCompanion()", roster);
            StringAssert.Contains("public void CompanionHome(", roster);
            string manager = Read("Assets/Scripts/Core/GameManager.cs");
            StringAssert.Contains("fromCamp ? SurvivorRoster.Instance.PackCompanion() : SurvivorRoster.Instance.Companion", manager);
            StringAssert.Contains("CompanionFollower.Raise(mate);", manager);
            StringAssert.Contains("CompanionFollower.Home(true);", manager);
            StringAssert.Contains("CompanionFollower.Home(false);", manager);
            StringAssert.Contains("CompanionFollower.Dismiss();", manager);
            string board = Read("Assets/Scripts/UI/OutpostInterface.cs");
            StringAssert.Contains("roster.Assign(id, CompanionKit.Task)", board);
        }
    }
}
