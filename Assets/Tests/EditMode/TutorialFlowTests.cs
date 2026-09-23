using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class TutorialFlowTests
    {
        [TearDown]
        public void Reset()
        {
            InputGlyphs.Note(false, true);
            ControlBindings.ResetDefaults();
            PadBindings.ResetDefaults();
        }

        [Test]
        public void DayOneWalksTheCampInOrder()
        {
            var gates = TutorialTrack.Camp.Select(s => s.Gate).ToArray();
            CollectionAssert.AreEqual(new[] { "assign", TutorialTrack.Read, "barricade", "craft_bandage", "launch" }, gates);
            int index = TutorialTrack.Advance(TutorialTrack.Camp, 0, "barricade", out bool finished);
            Assert.AreEqual(0, index);
            Assert.IsFalse(finished);
            foreach (var gate in gates)
                index = TutorialTrack.Advance(TutorialTrack.Camp, index, gate, out finished);
            Assert.AreEqual(TutorialTrack.Camp.Length, index);
            Assert.IsTrue(finished);
            TutorialTrack.Advance(TutorialTrack.Camp, index, "anything", out finished);
            Assert.IsTrue(finished);
            TutorialTrack.Advance(null, 0, "assign", out finished);
            Assert.IsTrue(finished);
        }

        [Test]
        public void TheCampTrackOnlyShowsInCamp()
        {
            Assert.IsTrue(TutorialTrack.InCamp(GameState.CampManagement));
            Assert.IsFalse(TutorialTrack.InCamp(GameState.ExpeditionActive));
            Assert.IsFalse(TutorialTrack.InCamp(GameState.MainMenu));
        }

        [Test]
        public void EveryStepHasBothLanguagesAndItsFallbackIsTheEnglishLine()
        {
            foreach (var step in TutorialTrack.Camp)
            {
                Assert.IsTrue(Loc.Has(step.Key, "en") && Loc.Has(step.Key, "es"), step.Key);
                Assert.AreEqual(step.Fallback, Loc.Raw(step.Key, "en"), step.Key);
            }
            for (int i = 0; i < TutorialTrack.Steps.Length; i++)
                Assert.AreEqual(TutorialTrack.Steps[i], Loc.Raw("lesson." + i, "en"), "lesson." + i);
            Assert.IsTrue(Loc.Has("tut.day1", "es") && Loc.Has("tut.next", "es"));
        }

        [Test]
        public void EveryGateIsHeardSomewhere()
        {
            string scripts = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts");
            var heard = new Regex(@"CodexDirector\.Hear\(""([a-z_]+)""\)");
            var signals = Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories)
                .SelectMany(f => heard.Matches(File.ReadAllText(f)).Cast<Match>().Select(m => m.Groups[1].Value))
                .ToHashSet();
            foreach (var gate in TutorialTrack.Gates) Assert.IsTrue(signals.Contains(gate), gate);
            foreach (var step in TutorialTrack.Camp)
                if (step.Gate != TutorialTrack.Read) Assert.IsTrue(signals.Contains(step.Gate), step.Gate);
            foreach (var hint in CodexBook.Hints) Assert.IsTrue(signals.Contains(hint.Signal), hint.Id);
        }

        [Test]
        public void EveryCampStepPointsAtAControlThePanelRings()
        {
            var marks = TutorialTrack.Camp.Select(s => s.Mark).ToArray();
            CollectionAssert.AllItemsAreNotNull(marks);
            CollectionAssert.AllItemsAreUnique(marks);
            foreach (var mark in marks) Assert.IsNotEmpty(mark);
            string ui = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts", "UI", "OutpostInterface.cs"));
            var ringed = new Regex(@"Lit\([\s\S]*?TutorialMark\.(\w+)\)").Matches(ui).Cast<Match>().Select(m => m.Groups[1].Value).ToHashSet();
            var names = typeof(TutorialMark).GetFields()
                .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                .ToDictionary(f => (string)f.GetRawConstantValue(), f => f.Name);
            foreach (var mark in marks)
            {
                Assert.IsTrue(names.ContainsKey(mark), mark);
                Assert.IsTrue(ringed.Contains(names[mark]), "the camp panel never rings " + mark);
            }
        }

        [Test]
        public void TheRingDimsOnlyRowsWithNothingMarked()
        {
            Assert.IsTrue(TutorialMark.Lit(TutorialMark.Stores, TutorialMark.Stores));
            Assert.IsFalse(TutorialMark.Lit(TutorialMark.Stores, TutorialMark.Task));
            Assert.IsFalse(TutorialMark.Lit("", ""));
            Assert.AreEqual(1f, TutorialMark.Opacity(false, false, false));
            Assert.AreEqual(1f, TutorialMark.Opacity(true, true, false));
            Assert.AreEqual(1f, TutorialMark.Opacity(true, false, true));
            Assert.AreEqual(TutorialMark.Dim, TutorialMark.Opacity(true, false, false));
            Assert.Less(TutorialMark.Dim, 1f);
        }

        [Test]
        public void EveryCodexSubjectHasARenderedIcon()
        {
            string root = Directory.GetCurrentDirectory();
            string icons = File.ReadAllText(Path.Combine(root, "Assets", "Resources", CodexIcons.ResourcePath + ".asset"));
            string manifest = File.ReadAllText(Path.Combine(root, "BlenderScripts", "assets.manifest.json"));
            int pictured = 0;
            foreach (var entry in CodexBook.Entries)
            {
                string item = CodexBook.ItemOf(entry.Id);
                if (item.Length > 0)
                {
                    Assert.IsNotNull(ItemCatalog.Find(item), entry.Id);
                    pictured++;
                    continue;
                }
                if (!entry.Id.StartsWith("zombie.") && !entry.Id.StartsWith("module.") && !entry.Id.StartsWith("faction.")) continue;
                Assert.IsFalse(string.IsNullOrEmpty(entry.Model), entry.Id + " names no model");
                StringAssert.Contains("  - id: " + entry.Model + "\n    icon: {fileID: 2800000, guid: ", icons.Replace("\r\n", "\n"), entry.Id);
                var tagged = new Regex(@"""id"": """ + entry.Model + @""",[^}]*?""tags"": \[[^\]]*""codex""");
                Assert.IsTrue(tagged.IsMatch(manifest), entry.Model + " is not tagged codex, so its icon is an albedo crop");
                pictured++;
            }
            Assert.GreaterOrEqual(pictured, 8);
            Assert.AreEqual("medkit", CodexBook.ItemOf("item.medkit"));
            Assert.AreEqual("", CodexBook.ItemOf("zombie.walker"));
            Assert.AreEqual("", CodexBook.ItemOf(null));
        }

        [Test]
        public void HintsCanBeReplayedWithoutForgettingTheCodex()
        {
            Assert.IsTrue(CodexBook.TryHint("", "near", out _, out string packed));
            packed = CodexBook.Remember(packed, "zombie.walker", out _);
            packed = CodexBook.Remember(packed, TutorialTrack.CampDone, out _);
            Assert.IsFalse(CodexBook.TryHint(packed, "near", out _, out _));
            string replay = CodexBook.ForgetHints(packed);
            Assert.IsFalse(CodexBook.Has(replay, "hint.crouch"));
            Assert.IsTrue(CodexBook.Has(replay, "zombie.walker"));
            Assert.IsTrue(CodexBook.Has(replay, TutorialTrack.CampDone));
            Assert.IsTrue(CodexBook.TryHint(replay, "near", out string again, out _));
            Assert.IsNotEmpty(again);
            Assert.AreEqual("", CodexBook.ForgetHints(null));
            Assert.AreEqual("", CodexBook.ForgetHints("hint.move|hint.fire"));
        }

        [Test]
        public void NewGameCarriesTheSkipChoiceIntoTheRun()
        {
            string root = Directory.GetCurrentDirectory();
            string ui = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "UI", "OutpostInterface.cs"));
            StringAssert.Contains("BeginNewOutpost(chosen, skip)", ui);
            string manager = File.ReadAllText(Path.Combine(root, "Assets", "Scripts", "Core", "GameManager.cs"));
            var reset = manager.IndexOf("TutorialDirector.Instance?.SetFinished(false);", System.StringComparison.Ordinal);
            var skip = manager.IndexOf("if (skipTutorial) TutorialDirector.Instance?.Dismiss();", System.StringComparison.Ordinal);
            Assert.Greater(reset, 0);
            Assert.Greater(skip, reset, "the skip must come after the new-game reset or the reset undoes it");
            Assert.AreNotEqual(Loc.T("new.tut_on", "en"), Loc.T("new.tut_off", "en"));
        }

        [Test]
        public void HintsArriveWhenTheyAreNeeded()
        {
            string Signal(string id) => CodexBook.Hints.First(h => h.Id == id).Signal;
            Assert.AreEqual("near", Signal("hint.crouch"));
            Assert.AreEqual("empty", Signal("hint.reload"));
            Assert.AreEqual("dark", Signal("hint.flashlight"));
            Assert.IsTrue(ControlBindings.TryRebind(ControlBindings.Action.Crouch, UnityEngine.InputSystem.Key.H));
            StringAssert.Contains("H crouches", Loc.T("hint.crouch", "en"));
        }

        [Test]
        public void TheFirstExpeditionIsAQuietScriptedStreet()
        {
            Assert.IsTrue(TutorialRun.Applies(false, false));
            Assert.IsFalse(TutorialRun.Applies(true, false));
            Assert.IsFalse(TutorialRun.Applies(false, true));
            Assert.LessOrEqual(TutorialRun.Beats.Length, TutorialRun.Cap);
            Assert.LessOrEqual(TutorialRun.KillGoal, TutorialRun.Beats.Length);
            Assert.AreEqual(2, TutorialRun.Scrap(2));
            Assert.AreEqual(TutorialRun.ScrapGoal, TutorialRun.Scrap(9));
            Assert.IsFalse(TutorialRun.Close(TutorialRun.Near + 0.5f));
            Assert.IsTrue(TutorialRun.Close(TutorialRun.Near));
            Assert.IsFalse(TutorialRun.Point(TutorialRun.Beats.Length, 0f, 0f, 0f, 1f, out _, out _));
        }

        [Test]
        public void ScriptedBodiesStandAheadOfTheStartWhicheverWayItFaces()
        {
            Assert.IsTrue(TutorialRun.Point(0, 0f, 0f, 0f, 1f, out float x, out float z));
            Assert.AreEqual(-3f, x, 1e-4f);
            Assert.AreEqual(14f, z, 1e-4f);
            Assert.IsTrue(TutorialRun.Point(0, 10f, 5f, 2f, 0f, out x, out z));
            Assert.AreEqual(24f, x, 1e-4f);
            Assert.AreEqual(8f, z, 1e-4f);
            Assert.IsTrue(TutorialRun.Point(1, 0f, 0f, 0f, 0f, out x, out z));
            Assert.AreEqual(4f, x, 1e-4f);
            Assert.AreEqual(24f, z, 1e-4f);
            float previous = 0f;
            for (int i = 0; i < TutorialRun.Beats.Length; i++)
            {
                TutorialRun.Point(i, 0f, 0f, 0.6f, 0.8f, out x, out z);
                float reach = (float)System.Math.Sqrt(x * x + z * z);
                Assert.IsFalse(TutorialRun.Close(reach), "beat " + i + " starts beyond the crouch hint");
                Assert.Greater(reach, previous);
                previous = reach;
            }
        }

        [Test]
        public void DayOneIsRememberedInTheCodex()
        {
            string packed = CodexBook.Remember("", TutorialTrack.CampDone, out bool added);
            Assert.IsTrue(added);
            Assert.IsTrue(CodexBook.Has(packed, TutorialTrack.CampDone));
            Assert.IsFalse(CodexBook.Has("", TutorialTrack.CampDone));
        }

        [Test]
        public void PromptsNameThePadButtonWhileAPadIsInUse()
        {
            Assert.AreEqual("R", InputGlyphs.Label(ControlBindings.Action.Reload));
            InputGlyphs.Note(true, false);
            Assert.IsTrue(InputGlyphs.UsingPad);
            Assert.AreEqual("X", InputGlyphs.Label(ControlBindings.Action.Reload));
            Assert.AreEqual("[A] Open", KeyPrompt.Interact("Open"));
            StringAssert.Contains("X reloads", Loc.T("hint.fire", "en"));
            StringAssert.Contains("D-pad Down", Loc.T("day1.2", "en"));
            InputGlyphs.Note(false, false);
            Assert.IsTrue(InputGlyphs.UsingPad, "no input keeps the last device");
            InputGlyphs.Note(false, true);
            Assert.AreEqual("[E] Open", KeyPrompt.Interact("Open"));
            StringAssert.Contains("Press B, pick Barricade", Loc.T("day1.2", "en"));
        }

        [Test]
        public void PadNamesReadLikeTheController()
        {
            Assert.AreEqual("A", InputGlyphs.PadName("South"));
            Assert.AreEqual("RT", InputGlyphs.PadName("RightTrigger"));
            Assert.AreEqual("Menu", InputGlyphs.PadName("Start"));
            Assert.AreEqual("None", InputGlyphs.PadName(""));
            Assert.AreEqual("Weird", InputGlyphs.PadName("Weird"));
        }
    }
}
