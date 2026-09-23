using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Core;
using OutpostZero.Shell;
using OutpostZero.UI;

namespace OutpostZero.Tests.EditMode
{
    public class HudTests
    {
        private static string PathOf(params string[] parts)
        {
            var all = new List<string> { Directory.GetCurrentDirectory() };
            all.AddRange(parts);
            return Path.Combine(all.ToArray());
        }

        private static string Read(params string[] parts) => File.ReadAllText(PathOf(parts)).Replace("\r\n", "\n");

        private static HashSet<string> Names() => new HashSet<string>(HudTree.Nodes.Select(n => n.Name)) { HudTree.Root };

        [Test]
        public void TheUxmlIsTheTree()
        {
            string path = PathOf("Assets", "UI", "Resources", "HUD.uxml");
            if (Environment.GetEnvironmentVariable("HUD_WRITE") == "1") File.WriteAllText(path, HudTree.Uxml());
            Assert.AreEqual(HudTree.Uxml(), Read("Assets", "UI", "Resources", "HUD.uxml"), "run the tests with HUD_WRITE=1 to regenerate HUD.uxml");
            StringAssert.Contains("<Style src=\"HUD.uss\" />", HudTree.Uxml());
        }

        [Test]
        public void TheTreeIsWellFormed()
        {
            var seen = new HashSet<string> { HudTree.Root };
            foreach (var node in HudTree.Nodes)
            {
                Assert.IsTrue(seen.Contains(node.Parent), node.Name + " comes before its parent " + node.Parent);
                Assert.IsTrue(seen.Add(node.Name), node.Name + " is named twice");
                Assert.IsFalse(string.IsNullOrEmpty(node.Classes), node.Name + " has no class");
            }
        }

        [Test]
        public void EveryClassIsStyled()
        {
            string uss = Read("Assets", "UI", "Resources", "HUD.uss");
            var styled = new HashSet<string>(Regex.Matches(uss, @"\.([a-z][a-z0-9-]*)").Cast<Match>().Select(m => m.Groups[1].Value));
            foreach (var node in HudTree.Nodes)
            {
                foreach (var cls in node.Classes.Split(' '))
                {
                    if (cls == "hud-caption" || cls.Length == 0) continue;
                    Assert.IsTrue(styled.Contains(cls), "HUD.uss has no rule for ." + cls + " on " + node.Name);
                }
            }
            string controller = Read("Assets", "Scripts", "UI", "HudController.cs");
            foreach (Match m in Regex.Matches(controller, @"""(hud-[a-z0-9-]+)"""))
                Assert.IsTrue(styled.Contains(m.Groups[1].Value), "the controller toggles ." + m.Groups[1].Value + " but HUD.uss never styles it");
            StringAssert.Contains("--hud-ink", uss);
            StringAssert.Contains("var(--hud-panel)", uss);
        }

        [Test]
        public void EveryNameTheControllerQueriesIsInTheTree()
        {
            var names = Names();
            string controller = Read("Assets", "Scripts", "UI", "HudController.cs");
            var asked = Regex.Matches(controller, @"\b[EL]\(""([a-z0-9-]+)""\)").Cast<Match>().Select(m => m.Groups[1].Value).ToList();
            Assert.Greater(asked.Count, 30);
            foreach (var name in asked) Assert.IsTrue(names.Contains(name), name + " is queried but not in HudTree");
            foreach (var edge in HudTree.Edges) Assert.IsTrue(names.Contains("edge-" + edge) && names.Contains("hit-" + edge), edge);
            foreach (var point in HudTree.Cardinals) Assert.IsTrue(names.Contains("compass-" + point), point);
            foreach (var need in HudTree.Needs)
                Assert.IsTrue(names.Contains("need-" + need + "-fill") && names.Contains("need-" + need + "-label"), need);
            foreach (var flag in HudTree.Status) Assert.IsTrue(names.Contains("status-" + flag), flag);
            for (int i = 0; i < HudTree.Slots; i++)
            {
                Assert.IsTrue(names.Contains(HudTree.SlotIcon(i)) && names.Contains(HudTree.SlotKey(i)) && names.Contains(HudTree.SlotLabel(i)), "slot " + i);
                Assert.IsTrue(names.Contains(HudTree.WheelName(i)), "wheel " + i);
            }
            for (int i = 0; i < HudTree.Toasts; i++) Assert.IsTrue(names.Contains(HudTree.ToastName(i)));
            Assert.AreEqual(OutpostZero.Combat.WeaponWheel.Slots, HudTree.Slots, "one HUD slot per wheel sector");
        }

        [Test]
        public void TheIssueElementsAreAllPresent()
        {
            var names = Names();
            string[] wanted =
            {
                "health-fill", "health-ghost", "stamina-fill", "needs", "status", "ammo-mag", "ammo-reserve", "reload-radial",
                "slots", "wheel", "noise-fill", "exposure-fill", "compass-strip", "compass-poi", "compass-gate", "prompt",
                "hit-marker", "kill-feed", "vignette", "toasts", "tension-beat", "subtitle", "tutorial", "popups"
            };
            foreach (var name in wanted) Assert.IsTrue(names.Contains(name), name);
            Assert.AreEqual(HudTree.Kind.Radial, HudTree.Nodes.First(n => n.Name == "reload-radial").Kind);
            Assert.AreEqual(HudTree.Kind.Picture, HudTree.Nodes.First(n => n.Name == HudTree.SlotIcon(0)).Kind);
        }

        [TestCase(1280, 720)]
        [TestCase(1920, 1080)]
        [TestCase(2560, 1440)]
        [TestCase(2560, 1080)]
        [TestCase(3440, 1440)]
        public void TheHudFitsAndReadsAtTheTestedResolutions(int width, int height)
        {
            Assert.IsTrue(HudFit.Fits(width, height, 1f), width + "x" + height);
            Assert.IsTrue(HudFit.Fits(width, height, PlayOptions.UiScaleMax), width + "x" + height + " at the largest interface size");
            Assert.IsTrue(HudFit.Readable(height, 1f, 1f), width + "x" + height + " smallest text " + HudFit.Pixels(HudTree.SmallestFont(), 1f, height, 1f) + "px");
            Assert.AreEqual(height / 1080f, HudFit.Scale(height, 1f), 0.0001f, "the panel matches height so ultrawide keeps element sizes");
        }

        [Test]
        public void UltrawideGetsMoreRoomNotBiggerElements()
        {
            Assert.AreEqual(1920f, HudFit.LogicalWidth(1920, 1080, 1f), 0.01f);
            Assert.AreEqual(2560f, HudFit.LogicalWidth(2560, 1080, 1f), 0.01f);
            Assert.AreEqual(HudFit.Scale(1080, 1f), HudFit.Scale(1080, 1f));
            Assert.AreEqual(HudFit.Pixels(1f, 1f, 1080, 1f), HudFit.Pixels(1f, 1f, 1080, 1f));
            Assert.AreEqual(HudFit.BaseFont, HudFit.Pixels(1f, 1f, 1080, 1f), 0.001f);
            Assert.IsFalse(HudFit.Fits(1024, 768, PlayOptions.UiScaleMax), "a 4:3 panel at the largest size is the known tight case");
        }

        [Test]
        public void ThePanelsScaleWithScreenHeight()
        {
            foreach (var file in new[] { "HudController.cs", "OutpostInterface.cs" })
            {
                string source = Read("Assets", "Scripts", "UI", file);
                StringAssert.Contains("PanelScaleMode.ScaleWithScreenSize", source, file);
                StringAssert.Contains("PanelScreenMatchMode.MatchWidthOrHeight", source, file);
                StringAssert.Contains("match = 1f", source, file);
                StringAssert.Contains("PanelScale.Track(panel)", source, file);
            }
        }

        [Test]
        public void ToastsStackNewestFirstAndExpireOldestFirst()
        {
            var stack = new ToastStack(3);
            stack.Push("a", 0f);
            stack.Push("b", 0.5f);
            stack.Push("c", 1f);
            Assert.AreEqual(3, stack.Count);
            Assert.AreEqual("c", stack.Line(0));
            Assert.AreEqual("a", stack.Line(2));
            stack.Push("d", 1.2f);
            Assert.AreEqual(3, stack.Count, "the stack is capped");
            Assert.AreEqual("d", stack.Line(0));
            Assert.AreEqual("b", stack.Line(2), "the oldest fell off");
            int version = stack.Version;
            stack.Push("d", 1.3f);
            Assert.AreEqual(version, stack.Version, "a repeat refreshes instead of stacking");
            Assert.IsFalse(stack.Prune(ToastStack.Life));
            Assert.IsTrue(stack.Prune(0.5f + ToastStack.Life + 0.01f));
            Assert.AreEqual(2, stack.Count);
            Assert.AreEqual("c", stack.Line(1));
            Assert.AreEqual(1f, stack.Alpha(0, 1.3f));
            Assert.Less(stack.Alpha(0, 1.3f + ToastStack.Life - 0.1f), 1f);
            Assert.AreEqual(0f, stack.Alpha(5, 0f));
            stack.Push(null, 2f);
            stack.Push("", 2f);
            Assert.AreEqual(2, stack.Count);
            stack.Clear();
            Assert.AreEqual(0, stack.Count);
            Assert.IsNull(stack.Line(0));
        }

        [Test]
        public void TheVignetteLightsTheEdgeTheHitCameFrom()
        {
            Assert.AreEqual(1f, DamageEdges.Weight(0, 0f), 0.001f, "a hit from ahead lights the top");
            Assert.AreEqual(0f, DamageEdges.Weight(2, 0f), 0.001f);
            Assert.AreEqual(1f, DamageEdges.Weight(1, 90f), 0.001f);
            Assert.AreEqual(1f, DamageEdges.Weight(3, -90f), 0.001f);
            Assert.AreEqual(1f, DamageEdges.Weight(2, 180f), 0.001f);
            Assert.AreEqual(1f, DamageEdges.Weight(2, -180f), 0.001f);
            float split = DamageEdges.Weight(0, 45f);
            Assert.AreEqual(split, DamageEdges.Weight(1, 45f), 0.001f, "a diagonal splits across both edges");
            Assert.AreEqual(0f, DamageEdges.Weight(3, 45f));
            Assert.AreEqual(1f, DamageEdges.Fade(0f, 1f));
            Assert.AreEqual(0f, DamageEdges.Fade(DamageEdges.Hold, 1f));
            Assert.Less(DamageEdges.Fade(0.5f, 1f), DamageEdges.Fade(0.2f, 1f));
            Assert.AreEqual(0f, DamageEdges.Fade(-0.1f, 1f));
            Assert.AreEqual(0.35f, DamageEdges.Strength(0.01f, 100f), 0.01f, "a scratch still shows");
            Assert.AreEqual(1f, DamageEdges.Strength(40f, 100f));
            Assert.AreEqual(0f, DamageEdges.Strength(0f, 100f));
        }

        [Test]
        public void TheCompassPlacesCardinalsAndPinsMarkersBehind()
        {
            Assert.AreEqual(0f, CompassMarks.Cardinal(0, 0f), 0.001f);
            Assert.AreEqual(90f, CompassMarks.Cardinal(1, 0f), 0.001f);
            Assert.AreEqual(-90f, CompassMarks.Cardinal(0, 90f), 0.001f, "facing east, north sits at the left end");
            Assert.IsTrue(CompassMarks.Place(45f, 200f, out float x));
            Assert.AreEqual(100f, x, 0.001f);
            Assert.IsFalse(CompassMarks.Place(150f, 200f, out x));
            Assert.AreEqual(200f, x, "behind to the right pins right");
            Assert.IsFalse(CompassMarks.Place(-150f, 200f, out x));
            Assert.AreEqual(-200f, x);
            Assert.AreEqual(1f, CompassMarks.Fade(0f));
            Assert.AreEqual(0f, CompassMarks.Fade(90f));
            Assert.Greater(CompassMarks.Fade(30f), CompassMarks.Fade(60f));
        }

        [Test]
        public void TheHeartbeatOnlyBeatsAtPeakAndStaysSubtle()
        {
            for (float t = 0f; t < 3f; t += 0.01f) Assert.AreEqual(1f, Heartbeat.Scale(t, false));
            float most = 0f;
            float least = 2f;
            for (float t = 0f; t < 3f; t += 0.005f)
            {
                float s = Heartbeat.Scale(t, true);
                most = Math.Max(most, s);
                least = Math.Min(least, s);
            }
            Assert.AreEqual(1f + Heartbeat.Swell, most, 0.01f);
            Assert.AreEqual(1f, least, 0.01f, "it rests between beats");
            Assert.LessOrEqual(Heartbeat.Swell, 0.4f);
            Assert.Greater(Heartbeat.Pulse(0.2f, true), 0.5f, "the second beat of the pair");
            Assert.Less(Heartbeat.Pulse(0.5f, true), 0.05f);
        }

        [Test]
        public void HitMarkersFlashAndKillsHoldLonger()
        {
            Assert.AreEqual(1f, HitPip.Alpha(0f, false));
            Assert.AreEqual(0f, HitPip.Alpha(HitPip.HitLife, false));
            Assert.Greater(HitPip.Alpha(HitPip.HitLife, true), 0f);
            Assert.Greater(HitPip.KillLife, HitPip.HitLife);
            Assert.Greater(HitPip.Spread(0.1f, true), HitPip.Spread(0.1f, false));
            Assert.Greater(HitPip.Spread(0.1f, false), HitPip.Spread(0f, false));
        }

        [Test]
        public void TheReloadRingSweepsAndRedrawsInSteps()
        {
            Assert.AreEqual(0f, ReloadArc.Sweep(0f));
            Assert.AreEqual(180f, ReloadArc.Sweep(0.5f), 0.001f);
            Assert.AreEqual(360f, ReloadArc.Sweep(2f));
            Assert.IsFalse(ReloadArc.Moved(180f, 0.502f));
            Assert.IsTrue(ReloadArc.Moved(180f, 0.52f));
        }

        [Test]
        public void NumbersAreFormattedOnce()
        {
            Assert.AreEqual("0", HudNumbers.Of(-4));
            Assert.AreEqual("42", HudNumbers.Of(42));
            Assert.AreSame(HudNumbers.Of(42), HudNumbers.Of(42), "a repeated value reuses its string");
            Assert.AreEqual("1234", HudNumbers.Of(1234));
        }

        [Test]
        public void UiKeysMoveWhenAnyInputMoves()
        {
            UiKey Make(int a, string b, bool c)
            {
                var key = new UiKey();
                key.Add(a);
                key.Add(b);
                key.Add(c);
                return key;
            }
            Assert.AreEqual(Make(3, "walker", true).Value, Make(3, "walker", true).Value);
            Assert.AreNotEqual(Make(3, "walker", true).Value, Make(4, "walker", true).Value);
            Assert.AreNotEqual(Make(3, "walker", true).Value, Make(3, "walkers", true).Value);
            Assert.AreNotEqual(Make(3, "walker", true).Value, Make(3, "walker", false).Value);
            Assert.AreNotEqual(Make(3, null, true).Value, Make(3, "", true).Value);
            var empty = new UiKey();
            Assert.AreEqual(empty.Value, new UiKey().Value);
            var a = new UiKey();
            a.Add("ab");
            a.Add("c");
            var b = new UiKey();
            b.Add("a");
            b.Add("bc");
            Assert.AreNotEqual(a.Value, b.Value, "string lengths keep neighbours apart");
        }

        [Test]
        public void TheMenuRefreshIsQuietDuringPlay()
        {
            Assert.IsTrue(OutpostInterface.MenuQuiet(GameState.ExpeditionActive));
            Assert.IsTrue(OutpostInterface.MenuQuiet(GameState.RaidActive));
            Assert.IsTrue(OutpostInterface.MenuQuiet(GameState.CampManagement));
            Assert.IsFalse(OutpostInterface.MenuQuiet(GameState.Paused));
            Assert.IsFalse(OutpostInterface.MenuQuiet(GameState.MainMenu));
            string ui = Read("Assets", "Scripts", "UI", "OutpostInterface.cs");
            Assert.IsFalse(ui.Contains("StringBuilder"), "the camp and pack keys hash instead of building strings");
            StringAssert.Contains("CampKey()", ui);
            StringAssert.Contains("PackKey()", ui);
            Assert.IsFalse(ui.Contains("vitals"), "the HUD moved to HudController");
            Assert.IsFalse(ui.Contains("DrawPopups"), "damage numbers use the HUD's label pool");
        }

        [Test]
        public void TheControllerAvoidsPerFrameStrings()
        {
            string controller = Read("Assets", "Scripts", "UI", "HudController.cs");
            Assert.IsFalse(controller.Contains("StringBuilder"));
            Assert.IsFalse(controller.Contains("string.Format"));
            Assert.IsFalse(controller.Contains("OnGUI"));
            Assert.IsFalse(controller.Contains("foreach (var popup"), "popups iterate by index so no enumerator boxes");
            StringAssert.Contains("HudNumbers.Of(", controller);
            StringAssert.Contains("Resources.Load<VisualTreeAsset>(ResourcePath)", controller);
            StringAssert.Contains("HudTree.Nodes", controller, "the code fallback builds the same tree");
        }

        [Test]
        public void NoRuntimeCodeDrawsWithImgui()
        {
            string scripts = PathOf("Assets", "Scripts");
            foreach (var file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                string rel = file.Substring(scripts.Length + 1).Replace('\\', '/');
                if (rel.StartsWith("Editor/")) continue;
                string text = File.ReadAllText(file);
                Assert.IsFalse(Regex.IsMatch(text, @"\bOnGUI\s*\("), rel + " still draws with OnGUI");
                Assert.IsFalse(Regex.IsMatch(text, @"\bGUILayout\."), rel + " still uses GUILayout");
            }
            Assert.IsFalse(File.Exists(PathOf("Assets", "Scripts", "UI", "SurvivalHUD.cs")));
            Assert.IsFalse(File.Exists(PathOf("Assets", "Scripts", "UI", "UitkHud.cs")));
            StringAssert.Contains("Add<HudController>(camera.gameObject)", Read("Assets", "Scripts", "Core", "GameSystemsInstaller.cs"));
        }

        [Test]
        public void EveryIconedWeaponPointsAtItsRenderedIcon()
        {
            var weapons = new Dictionary<string, string>
            {
                { "Pistol_9mm", "Weapon_Pistol_9mm_Icon.png" },
                { "Shotgun_Pump", "Weapon_Shotgun_Pump_Icon.png" },
                { "Rifle_Assault", "Weapon_AssaultRifle_Icon.png" },
                { "Machete", "Weapon_Machete_Icon.png" }
            };
            foreach (var pair in weapons)
            {
                string asset = Read("Assets", "Data", "Weapons", pair.Key + ".asset");
                string meta = Read("Assets", "Models", "Weapons", pair.Value + ".meta");
                string guid = Regex.Match(meta, @"guid: ([0-9a-f]{32})").Groups[1].Value;
                StringAssert.Contains("  icon: {fileID: 2800000, guid: " + guid + ", type: 3}", asset, pair.Key);
            }
        }

        [Test]
        public void TheNoiseMeterIsLabelledInEveryLanguage()
        {
            Assert.AreEqual("Noise", Loc.T("hud.noise", "en"));
            Assert.AreEqual("Ruido", Loc.T("hud.noise", "es"));
        }
    }
}
