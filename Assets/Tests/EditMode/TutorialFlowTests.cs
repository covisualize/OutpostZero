using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Core;
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
