using System.Collections.Generic;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Expedition;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class ResultsSheetTests
    {
        static ExpeditionOutcome Out(ExpeditionEnd end, int kills, int scrap) =>
            new ExpeditionOutcome { end = end, kills = kills, killGoal = 5, scrap = scrap, scrapGoal = 20, loadout = new[] { "bandage", "pistol" } };

        [Test]
        public void AnExtractionTrainsTheLeaderForEachQuotaMet()
        {
            ResultsSheet.Earned(Out(ExpeditionEnd.Extracted, 6, 25), out int combat, out int scavenge);
            Assert.AreEqual(1, combat);
            Assert.AreEqual(1, scavenge);
            ResultsSheet.Earned(Out(ExpeditionEnd.Extracted, 4, 25), out combat, out scavenge);
            Assert.AreEqual(0, combat);
            Assert.AreEqual(1, scavenge);
            ResultsSheet.Earned(Out(ExpeditionEnd.Dragged, 9, 90), out combat, out scavenge);
            Assert.AreEqual(0, combat + scavenge);
            ResultsSheet.Earned(Out(ExpeditionEnd.Succession, 9, 90), out combat, out scavenge);
            Assert.AreEqual(0, combat + scavenge);

            var leader = new Survivor { alive = true, combat = 2, combatXp = 0, scavenge = 5, scavengeXp = 1 };
            var trained = ResultsSheet.Trained(Out(ExpeditionEnd.Victory, 5, 20), leader);
            Assert.AreEqual(3, leader.combat);
            Assert.AreEqual(0, leader.combatXp);
            Assert.AreEqual(5, leader.scavenge);
            Assert.AreEqual(2, leader.scavengeXp);
            Assert.AreEqual(1, trained.combatShifts);
            Assert.AreEqual(3, trained.combatLevel);
            Assert.AreEqual(5, trained.scavengeLevel);
            StringAssert.Contains("+1 (3)", ResultsSheet.PracticeLine(trained, "en"));

            var none = ResultsSheet.Trained(Out(ExpeditionEnd.Extracted, 0, 0), leader);
            Assert.AreEqual(Loc.T("result.practice", "en") + " " + Loc.T("result.practice_none", "en"), ResultsSheet.PracticeLine(none, "en"));
            Assert.AreEqual(0, ResultsSheet.Trained(Out(ExpeditionEnd.Extracted, 9, 90), null).combatShifts);
        }

        [Test]
        public void OnlyWhatDidNotGoOutCountsAsBroughtHome()
        {
            var pack = new[]
            {
                new KeyValuePair<string, int>("pistol", 1),
                new KeyValuePair<string, int>("bandage", 3),
                new KeyValuePair<string, int>("canned_food", 1),
                new KeyValuePair<string, int>("canned_food", 2),
                new KeyValuePair<string, int>("antibiotics", 1),
                new KeyValuePair<string, int>("", 4),
                new KeyValuePair<string, int>("cloth", 0)
            };
            var brought = ResultsSheet.Brought(new[] { "bandage", "pistol" }, pack);
            Assert.AreEqual(2, brought.Count);
            Assert.AreEqual("antibiotics", brought[0].Key);
            Assert.AreEqual("canned_food", brought[1].Key);
            Assert.AreEqual(3, brought[1].Value);
            string line = ResultsSheet.BroughtLine(brought, "en");
            StringAssert.StartsWith(Loc.T("result.brought", "en"), line);
            StringAssert.Contains(Loc.Item("canned_food", "en") + " x3", line);
            StringAssert.Contains(Loc.T("result.brought_none", "es"), ResultsSheet.BroughtLine(ResultsSheet.Brought(null, null), "es"));
        }

        [Test]
        public void TheConditionLineNamesHealthBleedingAndWounds()
        {
            Assert.AreEqual("Mara: " + Loc.T("result.health", "en") + " 100%, " + Loc.T("result.unhurt", "en"),
                ResultsSheet.ConditionLine("Mara", 100f, 100f, false, 0, "en"));
            string hurt = ResultsSheet.ConditionLine("Mara", 32f, 80f, true, 2, "en");
            StringAssert.Contains("40%", hurt);
            StringAssert.Contains(Loc.T("hud.bleeding", "en").ToLowerInvariant(), hurt);
            StringAssert.Contains(WoundCard.Line(2, "en"), hurt);
            StringAssert.DoesNotContain(Loc.T("result.unhurt", "en"), hurt);
            StringAssert.Contains("0%", ResultsSheet.ConditionLine("", -5f, 0f, false, 0, "es"));
        }

        [Test]
        public void EveryResultsLineHasSpanish()
        {
            foreach (var key in new[] { "result.brought", "result.brought_none", "result.health", "result.unhurt", "result.practice", "result.practice_none" })
                Assert.AreNotEqual(key, Loc.Raw(key, "es"), key);
        }
    }
}
