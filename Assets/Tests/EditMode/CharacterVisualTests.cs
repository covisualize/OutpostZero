using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Graphics;

namespace OutpostZero.Tests.EditMode
{
    public class CharacterVisualTests
    {
        private static string Read(params string[] parts)
        {
            var all = new List<string> { Directory.GetCurrentDirectory() };
            all.AddRange(parts);
            return File.ReadAllText(Path.Combine(all.ToArray())).Replace("\r\n", "\n");
        }

        [Test]
        public void ThreeWalkersSideBySideLookDifferent()
        {
            var tints = new HashSet<string>();
            var splats = new HashSet<float>();
            for (int seed = 2; seed <= 4; seed++)
            {
                var tint = CharacterLook.Clothing(seed);
                tints.Add(tint.R.ToString("F3") + tint.G.ToString("F3") + tint.B.ToString("F3"));
                splats.Add(CharacterLook.GoreSeed(seed));
            }
            Assert.AreEqual(3, tints.Count, "clothing");
            Assert.AreEqual(3, splats.Count, "gore mask placement");
        }

        [Test]
        public void ClothingShiftStaysWithinEightPercentOfItsOutfit()
        {
            for (int seed = 0; seed < 600; seed += 3)
            {
                var tint = CharacterLook.Clothing(seed);
                Assert.That(tint.R, Is.InRange(0.919f, 1.081f), "seed " + seed);
                Assert.That(tint.B, Is.InRange(0.919f, 1.081f), "seed " + seed);
            }
        }

        [Test]
        public void GoreSeedsSpreadAcrossTheirRange()
        {
            float low = 100f, high = 0f;
            for (int seed = 1; seed < 200; seed++)
            {
                float value = CharacterLook.GoreSeed(seed);
                Assert.That(value, Is.InRange(0f, 100f));
                if (value < low) low = value;
                if (value > high) high = value;
            }
            Assert.Less(low, 10f);
            Assert.Greater(high, 90f);
        }

        [Test]
        public void TheGoreVariantSoaksTheBodyUnderFortyPercent()
        {
            float fresh = CharacterLook.GoreAmount("walker", false, 1);
            float hurt = CharacterLook.GoreAmount("walker", true, 1);
            Assert.Greater(fresh, 0f, "zombies always carry some blood");
            Assert.Greater(hurt, fresh * 2f);
            Assert.AreEqual(0f, CharacterLook.GoreAmount("survivor", false, 1));
            Assert.Greater(CharacterLook.GoreAmount("survivor", true, 1), 0f);
            Assert.AreEqual(0f, CharacterLook.GoreAmount("runner", true, 0), "gore off");
            Assert.Greater(CharacterLook.GoreAmount("brute", true, 2), hurt);
            Assert.LessOrEqual(CharacterLook.GoreAmount("brute", true, 2), 1f);
            Assert.IsTrue(CharacterLook.Wounded(39f, 100f, 1));
            Assert.IsFalse(CharacterLook.Wounded(41f, 100f, 1));
        }

        [Test]
        public void ZombieEyesAreHdrForBloomAndColouredPerArchetype()
        {
            var walker = CharacterLook.Eye("walker");
            var runner = CharacterLook.Eye("runner");
            var brute = CharacterLook.Eye("brute");
            Assert.AreEqual(2.5f, CharacterLook.Strength("walker"));
            Assert.AreEqual(2.5f, CharacterLook.Strength("runner"));
            Assert.Less(CharacterLook.Strength("brute"), CharacterLook.Strength("walker"), "brute is dim");
            Assert.Greater(walker.G * CharacterLook.Strength("walker"), 1f, "walker eyes pass the bloom threshold");
            Assert.Greater(walker.G, walker.R);
            Assert.Greater(walker.R, walker.B);
            Assert.Greater(runner.R, runner.G * 4f, "runner red");
            Assert.Greater(brute.R, brute.G);
            Assert.Greater(brute.G, brute.B);
            Assert.Less(CharacterLook.Strength("merchant"), 1f, "NPC eyes do not glow");
        }

        [Test]
        public void CharacterShaderCarriesAPerInstanceGoreMask()
        {
            string text = Read("Assets", "Shaders", "OutpostTriplanarRim.shader");
            var buffer = Regex.Match(text, @"CBUFFER_START\(UnityPerMaterial\)(.*?)CBUFFER_END", RegexOptions.Singleline);
            Assert.IsTrue(buffer.Success);
            foreach (var name in new[] { "_Gore;", "_GoreSeed;", "_GoreColor;" })
                StringAssert.Contains(name, buffer.Groups[1].Value, name);
            StringAssert.Contains("_Gore (\"Gore\", Range(0, 1))", text);
            StringAssert.Contains("GoreMask(input.positionOS)", text);
            StringAssert.Contains("float3 positionOS : TEXCOORD6;", text);
        }

        [Test]
        public void VarietyUsesTheGoreMaskAndLeavesTheMeltToCorpseMelt()
        {
            string variety = Read("Assets", "Scripts", "Graphics", "CharacterVariety.cs");
            StringAssert.Contains("\"_Gore\"", variety);
            StringAssert.Contains("\"_GoreSeed\"", variety);
            StringAssert.DoesNotContain("_Dissolve", variety);
            StringAssert.DoesNotContain("\"_Emission\", wounded", variety);
            StringAssert.Contains("melt.Clear()", Read("Assets", "Scripts", "AI", "ZombieAI.cs"));
        }

        [Test]
        public void BlobShadowAndAimStripeAreSoftAtlasDecals()
        {
            Assert.AreEqual(1, DecalAtlas.Count(DecalAtlas.Blob));
            Assert.AreEqual(1, DecalAtlas.Count(DecalAtlas.Aim));
            Assert.Less(DecalAtlas.First(DecalAtlas.Aim), DecalAtlas.Columns * DecalAtlas.Rows);
            string variety = Read("Assets", "Scripts", "Graphics", "CharacterVariety.cs");
            StringAssert.Contains("DecalAtlas.Blob", variety);
            StringAssert.Contains("DecalAtlas.Aim", variety);
            StringAssert.DoesNotContain("PrimitiveType.Cube", variety);
            Assert.Less(CharacterVariety.AimOpacity, CharacterVariety.BlobOpacity, "the aim stripe stays faint");
            string atlas = Read("BlenderScripts", "decal_atlas.py");
            StringAssert.Contains("(\"blob\", 1),", atlas);
            StringAssert.Contains("(\"aim\", 1),", atlas);
        }

        [Test]
        public void NpcsAndThePlayerGetTheSameTreatment()
        {
            string installer = Read("Assets", "Scripts", "Core", "GameSystemsInstaller.cs");
            StringAssert.Contains("Bind(\"survivor\", true)", installer);
            StringAssert.Contains("Bind(\"merchant\", false)", installer);
            StringAssert.Contains("Bind(\"colonist\", false)", installer);
            Assert.AreEqual("merchant", CharacterLook.RoleOf("Merchant_NPC"));
            Assert.AreEqual("colonist", CharacterLook.RoleOf("Colonist_03"));
        }
    }
}
