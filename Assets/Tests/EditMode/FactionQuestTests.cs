using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Expedition;

namespace OutpostZero.Tests.EditMode
{
    public class FactionQuestTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        private static string Guid(string metaRelative)
        {
            return Regex.Match(Read(metaRelative), "guid: (\\w+)").Groups[1].Value;
        }

        private static string Field(string asset, string name)
        {
            return Regex.Match(asset, "^  " + name + ": ?(.*)$", RegexOptions.Multiline).Groups[1].Value.Trim();
        }

        [Test]
        public void TheThreeQuestsAreObjectivesAndOnlyTheFieldOnesJoinARun()
        {
            var clinic = FactionQuest.Code("clinic");
            Assert.AreEqual(ObjectiveKind.Collect, clinic.Kind);
            Assert.AreEqual("Medical", clinic.Target);
            Assert.AreEqual(10, clinic.Count, "the issue asks for 10 meds");
            Assert.AreEqual(ObjectiveKind.ClearNest, FactionQuest.Code("farmers").Kind);
            Assert.AreEqual(ObjectiveKind.Escort, FactionQuest.Code("caravan").Kind);
            Assert.IsFalse(FactionQuest.Has(FactionQuest.Code("militia")));
            Assert.AreEqual("dressing", FactionQuest.CodePrint("clinic"), "the Clinic pays a blueprint");

            var quiet = FactionQuest.Open("", false);
            Assert.AreEqual(1, quiet.Count);
            Assert.AreEqual("quest.farmers", quiet[0].Id);
            Assert.IsTrue(quiet[0].Bonus, "a quest never holds up extraction");

            var visit = FactionQuest.Open("", true);
            CollectionAssert.AreEqual(new[] { "quest.caravan", "quest.farmers" }, new[] { visit[0].Id, visit[1].Id });
            var left = FactionQuest.Open("farmers", true);
            Assert.AreEqual(1, left.Count);
            Assert.AreEqual("quest.caravan", left[0].Id);
            Assert.AreEqual(0, FactionQuest.Open("caravan,farmers", true).Count);
        }

        [Test]
        public void ADoneQuestOnTheBoardIsPaidOnceAndOrdinaryObjectivesAreNot()
        {
            var specs = new List<ObjectiveSpec>(ObjectivePlan.For(ObjectivePlan.Prototype));
            specs.AddRange(FactionQuest.Open("", true));
            var board = new ObjectiveBoard(specs);
            Assert.IsTrue(board.Wants(ObjectiveKind.Escort));
            Assert.AreEqual(0, FactionQuest.Earned(board, "").Count);

            board.Note(ObjectiveKind.ClearNest, "nest", 5);
            CollectionAssert.AreEqual(new[] { "farmers" }, FactionQuest.Earned(board, ""), "the nest kills count for the stalls and the Farmers alike");
            board.Note(ObjectiveKind.Escort, FactionQuest.Porter, 1);
            CollectionAssert.AreEqual(new[] { "caravan", "farmers" }, FactionQuest.Earned(board, ""));
            CollectionAssert.AreEqual(new[] { "caravan" }, FactionQuest.Earned(board, "farmers"), "a paid quest is not paid again");

            Assert.AreEqual("", FactionQuest.FactionOf("am.nest"));
            Assert.AreEqual("", FactionQuest.FactionOf("quest.bogus"));
            Assert.AreEqual("clinic", FactionQuest.FactionOf("quest.clinic"));

            var fell = new ObjectiveBoard(FactionQuest.Open("", true));
            Assert.AreEqual(1, fell.Waive(ObjectiveKind.Escort, FactionQuest.Porter), "a fallen porter drops the escort");
            Assert.IsFalse(fell.Wants(ObjectiveKind.Escort));
        }

        [Test]
        public void MedsCountLooseMedicalItemsAndKitsAndTheHandInTakesLooseItemsFirst()
        {
            var medical = new List<KeyValuePair<string, int>>
            {
                new KeyValuePair<string, int>("bandage", 4),
                new KeyValuePair<string, int>("antibiotics", 2)
            };
            Assert.AreEqual(9, FactionQuest.Meds(3, medical));
            var taken = new List<KeyValuePair<string, int>>();
            Assert.IsTrue(FactionQuest.Split(8, 3, medical, taken, out int kits));
            Assert.AreEqual(2, kits);
            Assert.AreEqual(2, taken.Count);
            Assert.AreEqual(4, taken[0].Value);
            Assert.AreEqual(2, taken[1].Value);

            Assert.IsTrue(FactionQuest.Split(3, 3, medical, taken, out kits));
            Assert.AreEqual(0, kits);
            Assert.AreEqual(1, taken.Count);
            Assert.AreEqual(3, taken[0].Value);

            Assert.IsFalse(FactionQuest.Split(10, 3, medical, taken, out kits), "a short pack hands in nothing");
            Assert.AreEqual(0, taken.Count);
            Assert.AreEqual(0, kits);
        }

        [Test]
        public void EachFactionAssetLinksItsQuestObjectiveAndABookQuestOverridesTheCode()
        {
            string script = Guid("Assets/Scripts/Expedition/ObjectiveDefinition.cs.meta");
            foreach (string id in CaravanBook.BuiltInIds)
            {
                string faction = Read("Assets/Data/Factions/" + id + ".asset");
                var spec = FactionQuest.Code(id);
                string link = Field(faction, "quest");
                if (!FactionQuest.Has(spec))
                {
                    Assert.AreEqual("{fileID: 0}", link, id);
                    Assert.IsFalse(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Assets/Data/Factions/Quests/" + id + ".asset")), id);
                    continue;
                }
                string path = "Assets/Data/Factions/Quests/" + id + ".asset";
                Assert.AreEqual("{fileID: 11400000, guid: " + Guid(path + ".meta") + ", type: 2}", link, id);
                string quest = Read(path);
                StringAssert.Contains("guid: " + script, quest, id);
                Assert.AreEqual(spec.Id, Field(quest, "id"), id);
                Assert.AreEqual(((int)spec.Kind).ToString(), Field(quest, "kind"), id);
                Assert.AreEqual(spec.Target, Field(quest, "target"), id);
                Assert.AreEqual(spec.Count.ToString(), Field(quest, "count"), id);
                Assert.AreEqual("1", Field(quest, "bonus"), id);
                Assert.AreEqual(spec.Key, Field(quest, "titleKey"), id);
                Assert.AreEqual("'" + spec.Fallback + "'", Field(quest, "fallback"), id);
            }

            try
            {
                var rows = FactionTable.BuiltInRows();
                rows.Find(r => r.Id == "farmers").Quest = new ObjectiveSpec("x", ObjectiveKind.ClearNest, "nest", 9, false, 0, "", "Nine");
                rows.Find(r => r.Id == "caravan").Quest = default;
                FactionTable.Use(rows);
                var open = FactionQuest.Open("", true);
                Assert.AreEqual(1, open.Count, "a book faction with no quest asks for nothing");
                Assert.AreEqual("quest.farmers", open[0].Id, "the board id always names the faction");
                Assert.AreEqual(9, open[0].Count);
                Assert.IsTrue(open[0].Bonus);
            }
            finally
            {
                FactionTable.Clear();
            }
        }

        [Test]
        public void RunsCarryTheQuestsThePorterWalksAndExtractionPays()
        {
            string tracker = Read("Assets/Scripts/Expedition/ObjectiveTracker.cs");
            StringAssert.Contains("specs.AddRange(trade.FieldQuests());", tracker);
            StringAssert.Contains("NoteExtracted(ObjectiveTracker.Instance != null ? ObjectiveTracker.Instance.Board : null)", Read("Assets/Scripts/Core/GameManager.cs"));
            string dressing = Read("Assets/Scripts/Expedition/DistrictDressing.cs");
            StringAssert.Contains("board.Wants(ObjectiveKind.Escort)) RaisePorter(", dressing);
            StringAssert.Contains("AddComponent<CaravanPorter>()", dressing);
            string porter = Read("Assets/Scripts/Expedition/CaravanPorter.cs");
            StringAssert.Contains("Note(ObjectiveKind.Escort, FactionQuest.Porter, 1)", porter);
            StringAssert.Contains("Waive(ObjectiveKind.Escort, FactionQuest.Porter)", porter);
            StringAssert.DoesNotContain("NoteDistrictCleared", Read("Assets/Scripts/Shell/WorldMapService.cs"));
            StringAssert.Contains("inventory.TrySpendMeds(wanted)", Read("Assets/Scripts/Colony/FactionTrade.cs"));
            Assert.AreEqual("Walk me to the gate", StallVoice.Porter(false, "en"));
            Assert.AreEqual("El porteador de la caravana cayó", StallVoice.PorterFell("es"));
            Assert.AreEqual("The Clinic wants 10 meds", StallVoice.Quest("clinic", false, "en"));
        }
    }
}
