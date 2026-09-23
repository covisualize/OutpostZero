using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class CampUtilityTests
    {
        [Test]
        public void NeedsWoundsAndCollapseStillOverruleTheHour()
        {
            Assert.AreEqual("Cook", CampUtility.Choose("a", "Build", 30f, 80f, 60f, 0, 10f, 11f, false), "the starving eat");
            Assert.AreEqual("Rest", CampUtility.Choose("a", "Build", 80f, 80f, 5f, 0, 10f, 11f, true), "a breakdown rests");
            Assert.AreEqual("Medic", CampUtility.Choose("a", "Guard", 80f, 80f, 60f, 2, 10f, 23f, false));
            Assert.AreEqual("Clear", CampUtility.Choose("a", "Clear", 80f, 80f, 60f, 0, 10f, 3f, false), "clearing never waits");
            Assert.AreEqual("Rest", CampUtility.Choose("a", "Scavenge", 80f, 80f, 25f, 0, 10f, 11f, false), "the depressed skip the piles");
        }

        [Test]
        public void WorkersWorkByDayAndSleepByNightButTheWatchStaysUp()
        {
            Assert.AreEqual("Build", CampUtility.Choose("a", "Build", 80f, 80f, 60f, 0, 20f, 11f, false));
            Assert.AreEqual("Rest", CampUtility.Choose("a", "Build", 80f, 80f, 60f, 0, 20f, 23f, false));
            Assert.AreEqual("Guard", CampUtility.Choose("a", "Guard", 80f, 80f, 60f, 0, 20f, 23f, false));
            Assert.AreEqual("Build", CampUtility.Choose("a", "Build", 50f, 80f, 60f, 0, 20f, 11f, false), "a peckish builder finishes the shift");
            Assert.AreEqual("Cook", CampUtility.Choose("a", "Rest", 50f, 80f, 60f, 0, 20f, 11f, false), "a peckish idler heads for the fire");
        }

        [Test]
        public void TheIdleChatAndWanderAndKeepOnePickAllHour()
        {
            var seen = new HashSet<string>();
            foreach (string id in new[] { "s1", "s2", "s3", "s4", "s5" })
            {
                for (int h = 6; h < 22; h++)
                {
                    string early = CampUtility.Choose(id, "Rest", 80f, 80f, 60f, 0, 10f, h + 0.05f, true);
                    string late = CampUtility.Choose(id, "Rest", 80f, 80f, 60f, 0, 10f, h + 0.95f, true);
                    Assert.AreEqual(early, late, id + " at " + h);
                    seen.Add(early);
                }
            }
            CollectionAssert.Contains(seen, CampUtility.Chat);
            CollectionAssert.Contains(seen, CampUtility.Wander);
            Assert.AreNotEqual(CampUtility.Chat, CampUtility.Choose("s1", "Rest", 80f, 80f, 60f, 0, 10f, 12f, false), "no friend in the yard, no chat");
            Assert.AreEqual("Rest", CampUtility.Choose("s1", "Rest", 80f, 80f, 60f, 0, 10f, 2f, true), "nobody wanders at night");
        }

        [Test]
        public void TwoFriendsMeetInsteadOfChasingAndWandersStayInTheYard()
        {
            Assert.IsTrue(CampUtility.Hosts("s1", "s2"));
            Assert.IsFalse(CampUtility.Hosts("s2", "s1"));
            for (int h = 0; h < 24; h++)
            {
                CampUtility.WanderSpot("s3", h, out float x, out float z);
                float dx = x - CampUtility.YardX;
                float dz = z - CampUtility.YardZ;
                Assert.LessOrEqual(dx * dx + dz * dz, CampUtility.WanderReach * CampUtility.WanderReach + 0.01f);
            }
        }

        [Test]
        public void TheYardAsksTheScorerAndWanderersHaveALine()
        {
            string yard = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets/Scripts/Colony/CampPopulation.cs")).Replace("\r\n", "\n");
            StringAssert.Contains("CampUtility.Choose(survivor.id, survivor.task, survivor.hunger, survivor.thirst, survivor.morale, survivor.injury, survivor.fatigue, hour, friends[filled].Length > 0)", yard);
            StringAssert.Contains("CampUtility.WanderSpot(survivor.id, hour, out float roamX, out float roamZ);", yard);
            StringAssert.Contains("CampUtility.Hosts(survivor.id, friends[index - 1])", yard);
            Assert.AreEqual("Stretching my legs.", Loc.T("bark.wander", "en"));
            Assert.AreEqual("Estirando las piernas.", Loc.T("bark.wander", "es"));
        }
    }
}
