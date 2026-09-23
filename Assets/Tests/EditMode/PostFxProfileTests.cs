using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Graphics;
using UnityEngine;

namespace OutpostZero.Tests.EditMode
{
    public class PostFxProfileTests
    {
        private const string VolumeProfileScript = "d7fd9488000d3734a9e00ee676215985";

        private static readonly Dictionary<string, string> Scripts = new Dictionary<string, string>
        {
            { "Bloom", "0b2db86121404754db890f4c8dfe81b2" },
            { "Vignette", "899c54efeace73346a0a16faa3afe726" },
            { "Tonemapping", "97c23e3b12dc18c42a140437e53d3951" },
            { "ColorAdjustments", "66f335fb1ffd8684294ad653bf1c7564" },
            { "ShadowsMidtonesHighlights", "558a8e2b6826cf840aae193990ba9f2e" },
            { "LiftGammaGain", "5485954d14dfb9a4c8ead8edb0ded5b1" },
            { "ChromaticAberration", "81180773991d8724ab7f2d216912b564" },
            { "FilmGrain", "29fa0085f50d5e54f8144f766051a691" },
            { "MotionBlur", "ccf1aba9553839d41ae37dd52e9ebcce" },
            { "DepthOfField", "c01700fd266d6914ababb731e09af2eb" },
        };

        private sealed class Part
        {
            public bool Active;
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
        }

        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        /// <summary>The profile's components in list order, by name, with their overridden values.</summary>
        private static List<(string name, Part part)> Parts(string resource)
        {
            string text = Read("Assets/Resources/" + resource + ".asset");
            Assert.IsTrue(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Assets/Resources/" + resource + ".asset.meta")), resource + " meta");
            var blocks = new Dictionary<string, string>();
            foreach (Match m in Regex.Matches(text, "^--- !u!114 &(-?\\d+)\\n((?:(?!^--- ).*\\n)*)", RegexOptions.Multiline)) blocks[m.Groups[1].Value] = m.Groups[2].Value;
            Assert.IsTrue(blocks.ContainsKey("11400000"), resource + " root");
            string root = blocks["11400000"];
            StringAssert.Contains("guid: " + VolumeProfileScript + ",", root, resource + " is a VolumeProfile");
            StringAssert.Contains("m_Name: " + Path.GetFileName(resource) + "\n", root);
            var list = new List<(string, Part)>();
            foreach (Match m in Regex.Matches(root, "^  - \\{fileID: (-?\\d+)\\}$", RegexOptions.Multiline))
            {
                Assert.IsTrue(blocks.TryGetValue(m.Groups[1].Value, out var block), resource + " lists a missing component");
                string name = Regex.Match(block, "^  m_Name: (\\w+)$", RegexOptions.Multiline).Groups[1].Value;
                Assert.IsTrue(Scripts.ContainsKey(name), name);
                StringAssert.Contains("guid: " + Scripts[name] + ",", block, name + " points at its URP script");
                StringAssert.Contains("m_ObjectHideFlags: 3", block, name + " is a hidden sub-asset");
                var part = new Part { Active = Regex.IsMatch(block, "^  active: 1$", RegexOptions.Multiline) };
                foreach (Match p in Regex.Matches(block, "^  (\\w+):\\n    m_OverrideState: 1\\n    m_Value: (.*)$", RegexOptions.Multiline)) part.Values[p.Groups[1].Value] = p.Groups[2].Value.Trim();
                list.Add((name, part));
            }
            Assert.AreEqual(blocks.Count - 1, list.Count, resource + " has no stray sub-assets");
            return list;
        }

        private static float F(string value) => float.Parse(value, CultureInfo.InvariantCulture);

        private static Vector4 V(string value)
        {
            var m = Regex.Match(value, "\\{x: ([-\\d.e]+), y: ([-\\d.e]+), z: ([-\\d.e]+), w: ([-\\d.e]+)\\}");
            Assert.IsTrue(m.Success, value);
            return new Vector4(F(m.Groups[1].Value), F(m.Groups[2].Value), F(m.Groups[3].Value), F(m.Groups[4].Value));
        }

        private static Color C(string value)
        {
            var m = Regex.Match(value, "\\{r: ([-\\d.e]+), g: ([-\\d.e]+), b: ([-\\d.e]+), a: ([-\\d.e]+)\\}");
            Assert.IsTrue(m.Success, value);
            return new Color(F(m.Groups[1].Value), F(m.Groups[2].Value), F(m.Groups[3].Value), F(m.Groups[4].Value));
        }

        private static void Near(Vector4 want, Vector4 got, string label)
        {
            Assert.AreEqual(want.x, got.x, 1e-4f, label); Assert.AreEqual(want.y, got.y, 1e-4f, label);
            Assert.AreEqual(want.z, got.z, 1e-4f, label); Assert.AreEqual(want.w, got.w, 1e-4f, label);
        }

        [Test]
        public void TheCommittedPostProfileHoldsTheRigsBaseGrade()
        {
            var parts = Parts(PostFxRig.ProfilePath);
            CollectionAssert.AreEqual(new[] { "Bloom", "Vignette", "Tonemapping", "ColorAdjustments", "ShadowsMidtonesHighlights", "LiftGammaGain", "ChromaticAberration", "FilmGrain", "MotionBlur" },
                parts.ConvertAll(p => p.name));
            var by = new Dictionary<string, Part>();
            foreach (var (name, part) in parts) by[name] = part;

            Assert.AreEqual(PostFxRig.BloomIntensity, F(by["Bloom"].Values["intensity"]), 1e-4f);
            Assert.AreEqual(PostFxRig.BloomThreshold, F(by["Bloom"].Values["threshold"]), 1e-4f);
            Assert.AreEqual(PostFxRig.VignetteIntensity, F(by["Vignette"].Values["intensity"]), 1e-4f);
            Assert.AreEqual(PostFxRig.VignetteSmoothness, F(by["Vignette"].Values["smoothness"]), 1e-4f);
            Assert.AreEqual("2", by["Tonemapping"].Values["mode"], "ACES");
            Assert.AreEqual(PostFxRig.Exposure, F(by["ColorAdjustments"].Values["postExposure"]), 1e-4f);
            Assert.AreEqual(ScreenGrade.Contrast, F(by["ColorAdjustments"].Values["contrast"]), 1e-4f);
            Assert.AreEqual(ScreenGrade.BaseSaturation, F(by["ColorAdjustments"].Values["saturation"]), 1e-4f);
            Near((Vector4)PostFxRig.Filter, (Vector4)C(by["ColorAdjustments"].Values["colorFilter"]), "filter");
            Near(ScreenGrade.Shadows, V(by["ShadowsMidtonesHighlights"].Values["shadows"]), "shadows");
            Near(ScreenGrade.Midtones, V(by["ShadowsMidtonesHighlights"].Values["midtones"]), "midtones");
            Near(ScreenGrade.Highlights, V(by["ShadowsMidtonesHighlights"].Values["highlights"]), "highlights");
            Assert.IsEmpty(by["LiftGammaGain"].Values, "lift, gamma and gain are set by the night factor every frame");
            Assert.AreEqual(ScreenGrade.Aberration, F(by["ChromaticAberration"].Values["intensity"]), 1e-4f);
            Assert.AreEqual(PostFxRig.GrainIntensity, F(by["FilmGrain"].Values["intensity"]), 1e-4f);
            Assert.AreEqual("0", by["FilmGrain"].Values["type"], "Thin1");
            Assert.AreEqual(PostFxRig.BlurIntensity, F(by["MotionBlur"].Values["intensity"]), 1e-4f);

            foreach (var name in new[] { "Bloom", "Vignette", "Tonemapping", "ColorAdjustments", "ShadowsMidtonesHighlights", "LiftGammaGain", "ChromaticAberration" })
                Assert.IsTrue(by[name].Active, name + " is on");
            Assert.IsFalse(by["FilmGrain"].Active, "grain waits for a raid or the tier");
            Assert.IsFalse(by["MotionBlur"].Active, "blur waits for the setting or poison");
        }

        [Test]
        public void TheAimProfileHoldsTheGaussianDepthOfField()
        {
            var parts = Parts(PostFxRig.AimProfilePath);
            Assert.AreEqual(1, parts.Count);
            Assert.AreEqual("DepthOfField", parts[0].name);
            Assert.IsTrue(parts[0].part.Active);
            Assert.AreEqual("1", parts[0].part.Values["mode"], "Gaussian");
            Assert.AreEqual(PostFxRig.DepthStart, F(parts[0].part.Values["gaussianStart"]), 1e-4f);
            Assert.AreEqual(PostFxRig.DepthEnd, F(parts[0].part.Values["gaussianEnd"]), 1e-4f);
        }

        [Test]
        public void TheLookProfilesHoldEachLookAtFullStrength()
        {
            var sanctuary = Parts(GradeLayers.SanctuaryPath);
            Assert.AreEqual(1, sanctuary.Count);
            Assert.AreEqual("ColorAdjustments", sanctuary[0].name);
            Assert.AreEqual(1, sanctuary[0].part.Values.Count, "only the filter, so the base exposure and contrast pass through");
            Near((Vector4)GradeLayers.SanctuaryFilter(PostFxRig.Filter), (Vector4)C(sanctuary[0].part.Values["colorFilter"]), "sanctuary filter");

            var toxic = Parts(GradeLayers.ToxicPath);
            var byToxic = new Dictionary<string, Part>();
            foreach (var (name, part) in toxic) { Assert.IsTrue(part.Active, name); byToxic[name] = part; }
            CollectionAssert.AreEquivalent(new[] { "ColorAdjustments", "Vignette", "MotionBlur" }, byToxic.Keys);
            Near((Vector4)GradeLayers.ToxicFilter(PostFxRig.Filter), (Vector4)C(byToxic["ColorAdjustments"].Values["colorFilter"]), "toxic tint");
            Assert.AreEqual(GradeLayers.ToxicVignette(PostFxRig.VignetteIntensity), F(byToxic["Vignette"].Values["intensity"]), 1e-4f);
            Assert.AreEqual(PoisonVeil.Blur, F(byToxic["MotionBlur"].Values["intensity"]), 1e-4f, "the toxic blur");

            var damage = Parts(GradeLayers.DamagePath);
            var byDamage = new Dictionary<string, Part>();
            foreach (var (name, part) in damage) { Assert.IsTrue(part.Active, name); byDamage[name] = part; }
            CollectionAssert.AreEquivalent(new[] { "Vignette", "ColorAdjustments" }, byDamage.Keys);
            Assert.AreEqual(GradeLayers.DamageVignette(PostFxRig.VignetteIntensity), F(byDamage["Vignette"].Values["intensity"]), 1e-4f);
            Near((Vector4)GradeLayers.DamageEdge, (Vector4)C(byDamage["Vignette"].Values["color"]), "red edge");
            Assert.AreEqual(GradeLayers.DamageSaturation(0f), F(byDamage["ColorAdjustments"].Values["saturation"]), 1e-4f);
            Assert.AreEqual(1, byDamage["ColorAdjustments"].Values.Count, "only the drain, so the base filter passes through");
        }

        [Test]
        public void EachLookHasItsOwnVolumeAboveTheBase()
        {
            string rig = Read("Assets/Scripts/Graphics/PostFxRig.cs");
            StringAssert.Contains("Profile(sanctuary, GradeLayers.SanctuaryPath)", rig);
            StringAssert.Contains("Profile(toxic, GradeLayers.ToxicPath)", rig);
            StringAssert.Contains("Profile(damage, GradeLayers.DamagePath)", rig);
            StringAssert.Contains("damage.weight = hurt;", rig);
            Assert.IsFalse(rig.Contains("PoisonVeil.Tint("), "the base volume no longer carries the poison tint");
            Assert.IsFalse(rig.Contains("ScreenGrade.Warm("), "the base volume no longer carries the camp warmth");
            Assert.Less(20f, GradeLayers.ToxicPriority);
            Assert.Less(GradeLayers.ToxicPriority, GradeLayers.SanctuaryPriority);
            Assert.Less(GradeLayers.SanctuaryPriority, GradeLayers.DamagePriority);
        }

        [Test]
        public void TheStackedLooksMatchTheSingleVolumeGrade()
        {
            const float edge = 0.28f;
            for (float hurt = 0f; hurt <= 1f; hurt += 0.25f)
            {
                foreach (bool poisoned in new[] { false, true })
                {
                    float closed = GradeLayers.Blend(edge, GradeLayers.ToxicVignette(edge), poisoned ? 1f : 0f);
                    float stacked = GradeLayers.Blend(closed, GradeLayers.DamageVignette(closed), hurt);
                    Assert.AreEqual(ScreenGrade.Vignette(PoisonVeil.Shade(edge, poisoned), hurt), stacked, 1e-5f, "vignette");
                }
                float drained = GradeLayers.Blend(ScreenGrade.Saturation(0f, 0.3f), GradeLayers.DamageSaturation(0.3f), hurt);
                Assert.AreEqual(ScreenGrade.Saturation(hurt, 0.3f), drained, 1e-4f, "saturation");
                Near((Vector4)ScreenGrade.VignetteColor(hurt), (Vector4)GradeLayers.Blend(Color.black, GradeLayers.DamageEdge, hurt), "edge colour");
            }
            var filter = PostFxRig.Filter;
            var tinted = GradeLayers.Blend(filter, GradeLayers.ToxicFilter(filter), 1f);
            var both = GradeLayers.Blend(tinted, GradeLayers.SanctuaryFilter(tinted), 1f);
            PoisonVeil.Tint(true, filter.r, filter.g, filter.b, out float r, out float g, out float b);
            ScreenGrade.Warm(true, r, g, b, out r, out g, out b);
            Near(new Vector4(r, g, b, 1f), (Vector4)both, "camp and poison together");
        }

        [Test]
        public void LooksFadeRatherThanCut()
        {
            Assert.AreEqual(GradeLayers.FadeRate * 0.1f, GradeLayers.Fade(0f, true, 0.1f), 1e-5f);
            Assert.AreEqual(1f, GradeLayers.Fade(0.95f, true, 0.1f));
            Assert.AreEqual(0f, GradeLayers.Fade(0.05f, false, 0.1f));
            Assert.AreEqual(0.5f, GradeLayers.Fade(0.5f, true, -1f));
        }

        [Test]
        public void TheRigLoadsTheProfilesAndClonesThemPerVolume()
        {
            string rig = Read("Assets/Scripts/Graphics/PostFxRig.cs");
            StringAssert.Contains("Resources.Load<VolumeProfile>(path)", rig);
            StringAssert.Contains("return host.profile;", rig, "the per-frame overrides land on the volume's clone, never the asset");
            StringAssert.Contains("Profile(volume, ProfilePath)", rig);
            StringAssert.Contains("Profile(aimDepth, AimProfilePath)", rig);
            Assert.IsFalse(rig.Contains("volume.profile ="), "the rig never swaps in a profile it would have to destroy");
        }
    }
}
