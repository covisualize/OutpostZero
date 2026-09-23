using System.Collections.Generic;
using NUnit.Framework;
using OutpostZero.Core;
using OutpostZero.Graphics;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class VisionModeTests
    {
        [Test]
        public void TheSettingCyclesEveryPaletteOnceAndKeepsOldNumbers()
        {
            var seen = new HashSet<int>();
            int mode = 0;
            for (int i = 0; i < HudPalette.Count; i++)
            {
                Assert.IsTrue(seen.Add(mode), "mode " + mode + " came round twice");
                mode = HudPalette.Next(mode);
            }
            Assert.AreEqual(0, mode);
            Assert.AreEqual(HudPalette.Count, seen.Count);
            Assert.AreEqual(0, HudPalette.Next(99));
            Assert.AreEqual(2, HudPalette.Clamp(2));
            Assert.AreEqual(HudPalette.Tritan, HudPalette.Clamp(HudPalette.Tritan));
            Assert.AreEqual(0, HudPalette.Clamp(HudPalette.Count));
            Assert.AreEqual(0, HudPalette.Clamp(-1));
        }

        [Test]
        public void EveryPaletteHasAName()
        {
            var names = new HashSet<string>();
            for (int mode = 0; mode < HudPalette.Count; mode++)
            {
                string key = HudPalette.Name(mode);
                Assert.IsTrue(names.Add(key), key);
                Assert.AreNotEqual(key, Loc.T(key, "en"), key + " has no English text");
                Assert.AreNotEqual(key, Loc.T(key, "es"), key + " has no Spanish text");
            }
        }

        [Test]
        public void RedTealSplitsDangerFromSafetyWithoutBlueAgainstYellow()
        {
            var danger = HudPalette.Health(HudPalette.Tritan);
            var safe = HudPalette.Safe(HudPalette.Tritan);
            Assert.Greater(danger.r - safe.r, 0.5f);
            Assert.Greater(safe.g - danger.g, 0.3f);
            var quiet = HudPalette.Noise(HudPalette.Tritan, 0f);
            var loud = HudPalette.Noise(HudPalette.Tritan, 1f);
            Assert.Greater(loud.r - quiet.r, 0.5f);
            Assert.AreEqual(HudPalette.Noise(HudPalette.Tritan, 1f), HudPalette.Noise(HudPalette.Tritan, 3f));
            for (int mode = 0; mode < HudPalette.Count; mode++)
                Assert.AreNotEqual(HudPalette.Noise(mode, 0f), HudPalette.Noise(mode, 1f), "mode " + mode);
        }

        [Test]
        public void EnemiesTakeThePaletteColourAndTheOutlineHardensTheRim()
        {
            foreach (var zombie in new[] { "walker", "runner", "brute" })
            {
                Assert.AreEqual(CharacterLook.Eye(zombie).G, CharacterLook.Eye(zombie, 0).G);
                for (int mode = 1; mode < HudPalette.Count; mode++)
                {
                    var eye = CharacterLook.Eye(zombie, mode);
                    var enemy = CharacterLook.Enemy(mode);
                    Assert.AreEqual(enemy.R, eye.R);
                    Assert.AreEqual(enemy.G, eye.G);
                    Assert.AreEqual(enemy.B, eye.B);
                    CharacterLook.Rim(zombie, mode, false, out var rim, out _, out _);
                    Assert.AreEqual(enemy.B, rim.B);
                }
                CharacterLook.Rim(zombie, 0, false, out _, out float softAlpha, out float softPower);
                CharacterLook.Rim(zombie, 0, true, out _, out float hardAlpha, out float hardPower);
                Assert.AreEqual(1f, hardAlpha);
                Assert.Greater(hardAlpha, softAlpha);
                Assert.Less(hardPower, softPower);
                Assert.AreEqual(CharacterLook.OutlinePower, hardPower);
            }
            foreach (var person in new[] { "survivor", "merchant", "colonist" })
            {
                Assert.AreEqual(CharacterLook.Eye(person).R, CharacterLook.Eye(person, HudPalette.Tritan).R);
                CharacterLook.Rim(person, 1, true, out var rim, out float alpha, out float power);
                Assert.AreEqual(CharacterLook.RimSurvivor.R, rim.R);
                Assert.AreEqual(0.35f, alpha);
                Assert.AreEqual(3.2f, power);
            }
            Assert.AreNotEqual(CharacterLook.Enemy(1).B, CharacterLook.Enemy(HudPalette.Tritan).B);
        }

        [Test]
        public void PseudoLanguageWrapsEveryTableStringAndKeepsKeyTokens()
        {
            foreach (var key in Loc.Keys)
            {
                string english = Loc.Raw(key, "en");
                if (english.Length == 0) continue;
                string pseudo = Loc.Raw(key, PseudoLoc.Code);
                Assert.IsTrue(PseudoLoc.Wrapped(pseudo), key);
                Assert.Greater(pseudo.Length, english.Length, key);
            }
            string wrapped = PseudoLoc.Wrap("Press {key:Build} to build");
            StringAssert.Contains("{key:Build}", wrapped);
            StringAssert.DoesNotContain("Press", wrapped);
            StringAssert.StartsWith("[Préšš", wrapped);
            Assert.AreEqual("", PseudoLoc.Wrap(""));
            Assert.IsNull(PseudoLoc.Wrap(null));
            Assert.IsFalse(PseudoLoc.Wrapped("Plain text"));

            string caption = OutpostZero.Graphics.Presentation.Caption(NoiseType.ZombieScream, 0f, 5f, PseudoLoc.Code);
            StringAssert.DoesNotContain("Zombie", caption);
            StringAssert.DoesNotContain("north", caption);
            Assert.IsTrue(PseudoLoc.Wrapped(OutpostZero.Graphics.Presentation.Compass(-5f, 0f, PseudoLoc.Code)));
        }

        [Test]
        public void PseudoLanguageIsOnlyOfferedInDevBuilds()
        {
            Assert.AreEqual("es", PseudoLoc.Next("en", false));
            Assert.AreEqual("en", PseudoLoc.Next("es", false));
            Assert.AreEqual(PseudoLoc.Code, PseudoLoc.Next("es", true));
            Assert.AreEqual("en", PseudoLoc.Next(PseudoLoc.Code, true));
            Assert.AreEqual("en", PseudoLoc.Next(PseudoLoc.Code, false));
            Assert.AreEqual("en", PseudoLoc.Keep(PseudoLoc.Code, false));
            Assert.AreEqual(PseudoLoc.Code, PseudoLoc.Keep(PseudoLoc.Code, true));
            Assert.AreEqual("es", PseudoLoc.Keep("es", false));
            Assert.AreEqual("en", PseudoLoc.Keep(null, false));
            Assert.AreEqual("en", PseudoLoc.Keep("fr", true));
            Assert.IsTrue(PseudoLoc.Wrapped(PseudoLoc.Label(PseudoLoc.Code)));
        }

        [Test]
        public void TheOutlineAndRedTealModeSurviveARestart()
        {
            var snap = SettingsFile.Defaults();
            Assert.AreEqual(0, snap.outline);
            snap.outline = 1;
            snap.colorblind = HudPalette.Tritan;
            Assert.IsTrue(SettingsFile.TryFromJson(SettingsFile.ToJson(snap), out var back));
            Assert.AreEqual(1, back.outline);
            Assert.AreEqual(HudPalette.Tritan, back.colorblind);
            Assert.IsTrue(SettingsFile.TryFromJson("{\"shake\":1,\"colorblind\":2}", out var old));
            Assert.AreEqual(0, old.outline);
            Assert.AreEqual(2, HudPalette.Clamp(old.colorblind));
        }
    }
}
