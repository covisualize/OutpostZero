using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Core;
using OutpostZero.Player;
using OutpostZero.Shell;
using UnityEngine.InputSystem;

namespace OutpostZero.Tests.EditMode
{
    public class SettingsFlowTests
    {
        [TearDown]
        public void Reset() => ControlBindings.ResetDefaults();

        [Test]
        public void RenderScaleFollowsTheTierUntilOverridden()
        {
            Assert.AreEqual(0.75f, PlayOptions.RenderScale(0, 0.75f), 0.0001f);
            Assert.AreEqual(0.5f, PlayOptions.RenderScale(1, 1f), 0.0001f);
            Assert.AreEqual(1f, PlayOptions.RenderScale(5, 0.75f), 0.0001f);
            Assert.AreEqual(0.9f, PlayOptions.RenderScale(99, 0.9f), 0.0001f);
            Assert.AreEqual(0, PlayOptions.NextScale(PlayOptions.Scales.Length - 1));
            Assert.AreEqual(1, PlayOptions.NextScale(-3));
            Assert.AreEqual("75%", PlayOptions.ScaleName(3, "en"));
            Assert.AreEqual("Auto", PlayOptions.ScaleName(0, "es"));
        }

        [Test]
        public void RenderScaleSurvivesTheFile()
        {
            var snap = SettingsFile.Defaults();
            snap.render = 4;
            Assert.IsTrue(SettingsFile.TryFromJson(SettingsFile.ToJson(snap), out var back));
            Assert.AreEqual(4, back.render);
            Assert.IsTrue(SettingsFile.TryFromJson("{\"shake\":1}", out var old));
            Assert.AreEqual(0, old.render);
        }

        [Test]
        public void ClosingWithChangesAsksFirst()
        {
            Assert.AreEqual(SettingsClose.Close, SettingsDraft.OnClose(false, false));
            Assert.AreEqual(SettingsClose.Close, SettingsDraft.OnClose(false, true));
            Assert.AreEqual(SettingsClose.Ask, SettingsDraft.OnClose(true, false));
            Assert.AreEqual(SettingsClose.StayOpen, SettingsDraft.OnClose(true, true));
            Assert.IsFalse(SettingsDraft.Dirty(null, "{}"));
            Assert.IsFalse(SettingsDraft.Dirty("{\"a\":1}", "{\"a\":1}"));
            Assert.IsTrue(SettingsDraft.Dirty("{\"a\":1}", "{\"a\":2}"));
        }

        [Test]
        public void ARebindMakesTheSettingsDirty()
        {
            var snap = SettingsFile.Defaults();
            snap.keys = ControlBindings.Pack();
            string opened = SettingsFile.ToJson(snap);
            Assert.IsTrue(ControlBindings.TryRebind(ControlBindings.Action.Crouch, Key.H));
            snap.keys = ControlBindings.Pack();
            Assert.IsTrue(SettingsDraft.Dirty(opened, SettingsFile.ToJson(snap)));
        }

        [Test]
        public void FocusLossMutesAndReturnRestores()
        {
            Assert.AreEqual(0f, SettingsDraft.Heard(0.8f, false));
            Assert.AreEqual(0.8f, SettingsDraft.Heard(0.8f, true), 0.0001f);
            Assert.AreEqual(1f, SettingsDraft.Heard(3f, true));
            Assert.AreEqual(0f, SettingsDraft.Heard(-1f, true));
        }

        [Test]
        public void RebindRefusesFixedAndTakenKeys()
        {
            Assert.AreEqual(ControlBindings.RebindResult.Reserved, ControlBindings.Check(ControlBindings.Action.Reload, Key.W));
            Assert.AreEqual(ControlBindings.RebindResult.Reserved, ControlBindings.Check(ControlBindings.Action.Reload, Key.Digit3));
            Assert.AreEqual(ControlBindings.RebindResult.Taken, ControlBindings.Check(ControlBindings.Action.Reload, Key.E));
            Assert.AreEqual((int)ControlBindings.Action.Interact, ControlBindings.Holder(ControlBindings.Action.Reload, Key.E));
            Assert.AreEqual(-1, ControlBindings.Holder(ControlBindings.Action.Interact, Key.E));
            Assert.AreEqual(ControlBindings.RebindResult.Bound, ControlBindings.Check(ControlBindings.Action.Interact, Key.E));
            Assert.AreEqual(ControlBindings.RebindResult.Invalid, ControlBindings.Check(ControlBindings.Action.Interact, Key.None));
            Assert.IsFalse(ControlBindings.TryRebind(ControlBindings.Action.Reload, Key.Space));
            Assert.AreEqual("R", ControlBindings.Label(ControlBindings.Action.Reload));
        }

        [Test]
        public void ASavedFixedKeyIsIgnored()
        {
            ControlBindings.Unpack((int)Key.W + ",999999,x");
            Assert.AreEqual("Esc", ControlBindings.Label(ControlBindings.Action.Pause));
            Assert.AreEqual("R", ControlBindings.Label(ControlBindings.Action.Reload));
        }

        [Test]
        public void KeyNamesReadLikeTheKeyboard()
        {
            Assert.AreEqual("Shift", ControlBindings.Name(Key.LeftShift));
            Assert.AreEqual("1", ControlBindings.Name(Key.Digit1));
            Assert.AreEqual("9", ControlBindings.Name(Key.Digit9));
            Assert.AreEqual("0", ControlBindings.Name(Key.Digit0));
            Assert.AreEqual("Tab", ControlBindings.Name(Key.Tab));
            Assert.AreEqual("Shift", ControlBindings.Label(ControlBindings.Action.Sprint));
        }

        [Test]
        public void RebindingUpdatesHudPromptsAndHints()
        {
            Assert.AreEqual("[E] Open", KeyPrompt.Interact("Open"));
            StringAssert.Contains("C crouch, Shift sprint", Loc.T("lesson.0", "en"));
            StringAssert.StartsWith("Tab opens", Loc.T("hint.pack", "en"));
            Assert.IsTrue(ControlBindings.TryRebind(ControlBindings.Action.Interact, Key.H));
            Assert.IsTrue(ControlBindings.TryRebind(ControlBindings.Action.Crouch, Key.X));
            Assert.IsTrue(ControlBindings.TryRebind(ControlBindings.Action.Inventory, Key.J));
            Assert.IsTrue(ControlBindings.TryRebind(ControlBindings.Action.Medkit, Key.K));
            Assert.AreEqual("[H] Open", KeyPrompt.Interact("Open"));
            StringAssert.Contains("X crouch", Loc.T("lesson.0", "en"));
            StringAssert.Contains("X agacha", Loc.T("lesson.0", "es"));
            StringAssert.StartsWith("J opens", Loc.T("hint.pack", "en"));
            StringAssert.StartsWith("J abre", Loc.T("lesson.4", "es"));
            StringAssert.Contains("K uses", Loc.T("codex.item.medkit.body", "en"));
            Assert.AreEqual("J opens the pack. Use what you are carrying.", Loc.Hint("hint.pack", ""));
        }

        [Test]
        public void FillLeavesUnknownTokensAlone()
        {
            Assert.AreEqual("", KeyPrompt.Fill(""));
            Assert.IsNull(KeyPrompt.Fill(null));
            Assert.AreEqual("no keys", KeyPrompt.Fill("no keys"));
            Assert.AreEqual("{key:Nope} x", KeyPrompt.Fill("{key:Nope} x"));
            Assert.AreEqual("{key:7}", KeyPrompt.Fill("{key:7}"));
            Assert.AreEqual("R and {key:Reload", KeyPrompt.Fill("{key:Reload} and {key:Reload"));
            Assert.AreEqual("{key:Reload}", KeyPrompt.Token(ControlBindings.Action.Reload));
        }

        [Test]
        public void KeyNamesLiveInTokensNotLetters()
        {
            foreach (var key in new[] { "lesson.0", "lesson.1", "lesson.4", "hint.fire", "hint.pack", "camp.build", "codex.item.medkit.body" })
            foreach (var language in LocCsv.Languages)
                StringAssert.Contains(KeyPrompt.Open, Loc.Raw(key, language), key + " " + language);
            StringAssert.Contains("{key:Build}", LocCsv.Export());
            var literal = new Regex(@"""\[[A-Z][a-z]*\]\s");
            string ui = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts", "UI");
            var hits = Directory.GetFiles(ui, "*.cs", SearchOption.AllDirectories).Where(f => literal.IsMatch(File.ReadAllText(f))).ToArray();
            CollectionAssert.IsEmpty(hits, "hard-coded key prompts");
        }

        [Test]
        public void RefusedRebindExplainsWhy()
        {
            Assert.AreEqual("E is already on Interact.", BindNote.For(ControlBindings.RebindResult.Taken, Key.E, (int)ControlBindings.Action.Interact, "en"));
            Assert.AreEqual("W es fijo y no se puede reasignar.", BindNote.For(ControlBindings.RebindResult.Reserved, Key.W, -1, "es"));
            Assert.AreEqual("", BindNote.For(ControlBindings.RebindResult.Bound, Key.H, -1, "en"));
            foreach (var key in new[] { "set.unsaved", "set.keep", "set.back", "set.render", "scale.auto", "bind.taken", "bind.reserved" })
                Assert.IsTrue(Loc.Has(key, "en") && Loc.Has(key, "es"), key);
        }
    }
}
