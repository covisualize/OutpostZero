using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Expedition;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-31: composable expedition objectives, authored as ExpeditionDefinition assets per district.</summary>
    public class ObjectiveBoardTests
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

        private static string Field(string asset, string name)
        {
            string value = Regex.Match(asset, "^  " + name + ": ?(.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
            if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'') value = value.Substring(1, value.Length - 2).Replace("''", "'");
            return value;
        }

        private static List<string> Listed(string asset, string field)
        {
            var block = Regex.Match(asset, "^  " + field + ":\\n((?:  - .*\\n)*)", RegexOptions.Multiline);
            Assert.IsTrue(block.Success, field + " list");
            var guids = new List<string>();
            foreach (Match m in Regex.Matches(block.Groups[1].Value, "guid: (\\w+), type: 2")) guids.Add(m.Groups[1].Value);
            return guids;
        }

        private static ObjectiveBoard Sample()
        {
            return new ObjectiveBoard(new[]
            {
                new ObjectiveSpec("meds", ObjectiveKind.Collect, "Medical", 2, true, 4, "", "Find meds"),
                new ObjectiveSpec("nest", ObjectiveKind.ClearNest, "nest", 3, false, 0, "", "Clear the nest"),
                new ObjectiveSpec("gate", ObjectiveKind.Reach, "", 1, true, 2, "", "Reach anywhere"),
            });
        }

        [TearDown]
        public void Reset()
        {
            ObjectivePlan.Clear();
        }

        [Test]
        public void NotesAdvanceOnlyMatchingObjectivesAndCapAtTheCount()
        {
            var board = Sample();
            Assert.AreEqual(0, board.Note(ObjectiveKind.Collect, "FoodWater", 5).Count);
            Assert.AreEqual(0, board.Tally);
            Assert.AreEqual(0, board.Note(ObjectiveKind.Collect, "medical", 1).Count, "categories match without case");
            var finished = board.Note(ObjectiveKind.Collect, "Medical", 5);
            Assert.AreEqual(1, finished.Count);
            Assert.AreEqual("meds", finished[0].Id);
            Assert.AreEqual(2, board.Progress(0));
            Assert.AreEqual(0, board.Note(ObjectiveKind.Collect, "Medical", 1).Count, "a done objective does not finish twice");
            Assert.AreEqual(1, board.Note(ObjectiveKind.Reach, "poi", 1).Count, "an empty target matches any spot");
        }

        [Test]
        public void ExtractionWaitsOnRequiredObjectivesOnlyAndRewardsCountDoneOnes()
        {
            var board = Sample();
            board.Note(ObjectiveKind.Collect, "Medical", 2);
            Assert.IsFalse(board.RequiredDone);
            Assert.AreEqual(4, board.BonusReward);
            board.Note(ObjectiveKind.ClearNest, "nest", 3);
            Assert.IsTrue(board.RequiredDone);
            Assert.AreEqual(2, board.DoneCount);
            Assert.IsTrue(new ObjectiveBoard(null).RequiredDone);
        }

        [Test]
        public void WaivingDropsOpenObjectivesButKeepsDoneOnes()
        {
            var board = new ObjectiveBoard(new[]
            {
                new ObjectiveSpec("a", ObjectiveKind.Rescue, "rescue_mall", 1, false, 0, "", "A"),
                new ObjectiveSpec("b", ObjectiveKind.Rescue, "rescue_police", 1, true, 3, "", "B"),
                new ObjectiveSpec("c", ObjectiveKind.Retrieve, "poi", 1, true, 1, "", "C"),
                new ObjectiveSpec("c", ObjectiveKind.Retrieve, "poi", 1, true, 1, "", "duplicate id"),
            });
            Assert.AreEqual(3, board.Count, "duplicate ids are dropped");
            board.Note(ObjectiveKind.Rescue, "rescue_police", 1);
            Assert.AreEqual(1, board.Waive(ObjectiveKind.Rescue, "rescue_mall"));
            Assert.IsTrue(board.RequiredDone, "a waived required objective no longer holds up extraction");
            Assert.AreEqual(0, board.Waive(ObjectiveKind.Rescue, ""), "done objectives stay");
            Assert.AreEqual(1, board.Waive(ObjectiveKind.Retrieve, ""));
            Assert.AreEqual(1, board.Count);
            Assert.AreEqual(3, board.BonusReward);
        }

        [Test]
        public void LinesMarkBonusAndProgress()
        {
            var board = Sample();
            board.Note(ObjectiveKind.ClearNest, "nest", 1);
            Assert.AreEqual("[ ] Bonus: Find meds 0/2\n[ ] Clear the nest 1/3\n[ ] Bonus: Reach anywhere", board.Lines("en"));
            board.Note(ObjectiveKind.Collect, "Medical", 2);
            Assert.AreEqual("[x] Extra: Find meds 2/2", board.Line(0, "es"));
            Assert.AreEqual("Objective done: Find meds  +4 scrap", ObjectiveBoard.Finished(board.Spec(0), "en"));
        }

        [Test]
        public void ThePrototypeDistrictCarriesThreeObjectivesOfDifferentKinds()
        {
            var specs = ObjectivePlan.For(ObjectivePlan.Prototype);
            Assert.AreEqual(3, specs.Length);
            var kinds = new HashSet<ObjectiveKind>();
            foreach (var spec in specs) kinds.Add(spec.Kind);
            Assert.AreEqual(3, kinds.Count);
            Assert.AreEqual(0, ObjectivePlan.For("nowhere").Length);
            Assert.AreEqual(0, ObjectivePlan.For(null).Length);
        }

        [Test]
        public void ABookOverridesThePlanAndAnEmptyBookFallsBack()
        {
            ObjectivePlan.Use(new[]
            {
                new KeyValuePair<string, ObjectiveSpec[]>("mall", new[] { new ObjectiveSpec("m", ObjectiveKind.Reach, "atrium", 1, false, 0, "", "Reach the atrium") }),
            });
            Assert.IsTrue(ObjectivePlan.FromAsset);
            Assert.AreEqual(1, ObjectivePlan.For("mall").Length);
            Assert.AreEqual(0, ObjectivePlan.For(ObjectivePlan.Prototype).Length);
            ObjectivePlan.Use(new[] { new KeyValuePair<string, ObjectiveSpec[]>("", new ObjectiveSpec[0]) });
            Assert.IsFalse(ObjectivePlan.FromAsset);
            Assert.AreEqual(3, ObjectivePlan.For(ObjectivePlan.Prototype).Length);
        }

        [Test]
        public void TheBookListsEachExpeditionAndItsObjectivesAsAuthored()
        {
            string book = Read("Assets/Resources/" + ExpeditionBook.ResourcePath + ".asset");
            StringAssert.Contains(Guid("Assets/Scripts/Expedition/ExpeditionBook.cs.meta"), book);
            var expeditions = Listed(book, "expeditions");
            Assert.AreEqual(ObjectivePlan.Code.Length, expeditions.Count);
            string expeditionScript = Guid("Assets/Scripts/Expedition/ExpeditionDefinition.cs.meta");
            string objectiveScript = Guid("Assets/Scripts/Expedition/ObjectiveDefinition.cs.meta");
            for (int i = 0; i < ObjectivePlan.Code.Length; i++)
            {
                var pair = ObjectivePlan.Code[i];
                string path = "Assets/Data/Expeditions/" + pair.Key + ".asset";
                Assert.AreEqual(expeditions[i], Guid(path + ".meta"), path + " is listed out of order");
                string expedition = Read(path);
                StringAssert.Contains(expeditionScript, expedition, path);
                Assert.AreEqual(pair.Key, Field(expedition, "district"), path);
                var objectives = Listed(expedition, "objectives");
                Assert.AreEqual(pair.Value.Length, objectives.Count, path);
                for (int j = 0; j < pair.Value.Length; j++)
                {
                    var spec = pair.Value[j];
                    string objectivePath = "Assets/Data/Expeditions/Objectives/" + spec.Id.Replace(".", "_") + ".asset";
                    Assert.AreEqual(objectives[j], Guid(objectivePath + ".meta"), objectivePath);
                    string asset = Read(objectivePath);
                    StringAssert.Contains(objectiveScript, asset, objectivePath);
                    Assert.AreEqual(spec.Id, Field(asset, "id"));
                    Assert.AreEqual(((int)spec.Kind).ToString(), Field(asset, "kind"), objectivePath);
                    Assert.AreEqual(spec.Target, Field(asset, "target"), objectivePath);
                    Assert.AreEqual(spec.Count.ToString(), Field(asset, "count"), objectivePath);
                    Assert.AreEqual(spec.Bonus ? "1" : "0", Field(asset, "bonus"), objectivePath);
                    Assert.AreEqual(spec.Reward.ToString(), Field(asset, "reward"), objectivePath);
                    Assert.AreEqual(spec.Key, Field(asset, "titleKey"), objectivePath);
                    Assert.AreEqual(spec.Fallback, Field(asset, "fallback"), objectivePath);
                }
            }
        }

        [Test]
        public void EveryObjectiveReadsFromTheStringTable()
        {
            foreach (var pair in ObjectivePlan.Code)
                foreach (var spec in pair.Value)
                {
                    Assert.AreEqual(spec.Fallback, Loc.Raw(spec.Key, "en"), spec.Key);
                    Assert.AreNotEqual(spec.Key, Loc.Raw(spec.Key, "es"), spec.Key + " has no Spanish line");
                }
        }

        [Test]
        public void ResultsCountObjectivesAndPayOnlyWhenTheLeaderExtracts()
        {
            var board = Sample();
            board.Note(ObjectiveKind.Collect, "Medical", 2);
            var home = ResultsSheet.Scored(new ExpeditionOutcome { end = ExpeditionEnd.Extracted }, board);
            Assert.AreEqual(1, home.objectivesDone);
            Assert.AreEqual(3, home.objectivesTotal);
            Assert.AreEqual(4, home.bonusScrap);
            Assert.AreEqual("Objectives: 1/3  +4 bonus scrap", ResultsSheet.ObjectivesLine(home, "en"));
            var lost = ResultsSheet.Scored(new ExpeditionOutcome { end = ExpeditionEnd.Succession }, board);
            Assert.AreEqual(0, lost.bonusScrap);
            Assert.AreEqual("Objetivos: 1/3", ResultsSheet.ObjectivesLine(lost, "es"));
            Assert.AreEqual("", ResultsSheet.ObjectivesLine(ResultsSheet.Scored(home, null), "en"));
        }
    }
}
