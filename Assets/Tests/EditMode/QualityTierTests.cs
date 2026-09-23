using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Graphics;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>
    /// PRO-45: quality tiers backed by one URP asset each, SRP Batcher and instancing coverage, LOD and occlusion
    /// rules, zombie update budgeting, and texture import rules, checked against the committed assets.
    /// </summary>
    public class QualityTierTests
    {
        static string Root => Directory.GetCurrentDirectory();

        static string Read(params string[] parts) => File.ReadAllText(Path.Combine(Root, Path.Combine(parts)));

        static string GuidOf(string assetPath) => Regex.Match(File.ReadAllText(Path.Combine(Root, assetPath + ".meta")), @"guid: (\w+)").Groups[1].Value;

        static string Field(string yaml, string key) => Regex.Match(yaml, @"(?m)^\s*" + Regex.Escape(key) + @": (.*)$").Groups[1].Value.Trim();

        static List<string> QualityLevels()
        {
            string text = Read("ProjectSettings", "QualitySettings.asset");
            var blocks = new List<string>();
            string[] parts = text.Split(new[] { "  - serializedVersion:" }, System.StringSplitOptions.None);
            for (int i = 1; i < parts.Length; i++) blocks.Add(parts[i]);
            return blocks;
        }

        [Test]
        public void EachTierOwnsOneQualityLevelAndOneUrpAsset()
        {
            var levels = QualityLevels();
            Assert.AreEqual(QualityProfile.Count, levels.Count, "Low, Medium, High, Ultra");
            for (int i = 0; i < QualityProfile.Count; i++)
            {
                var tier = QualityProfile.For(i);
                Assert.AreEqual(tier.Name, Field(levels[i], "name"));
                StringAssert.Contains("guid: " + GuidOf(QualityProfile.AssetPath(i)), Field(levels[i], "customRenderPipeline"), tier.Name);
                Assert.AreEqual(tier.LodBias, float.Parse(Field(levels[i], "lodBias"), System.Globalization.CultureInfo.InvariantCulture), 0.001f, tier.Name);
                Assert.AreEqual("1", Field(levels[i], "streamingMipmapsActive"), tier.Name + " streams mips");
            }
            string quality = Read("ProjectSettings", "QualitySettings.asset");
            Assert.AreEqual("1", Field(quality, "m_CurrentQuality"), "the editor opens on Medium");
            Assert.AreEqual("1", Field(quality, "Standalone"), "desktop players default to Medium");
            Assert.AreEqual("0", Field(quality, "Android"), "phones default to Low");
            StringAssert.Contains(GuidOf(QualityProfile.AssetPath(1)), Read("ProjectSettings", "GraphicsSettings.asset"), "the default pipeline is Medium");
        }

        [Test]
        public void EachUrpAssetCarriesItsTierSettings()
        {
            var c = System.Globalization.CultureInfo.InvariantCulture;
            for (int i = 0; i < QualityProfile.Count; i++)
            {
                var tier = QualityProfile.For(i);
                string asset = Read(QualityProfile.AssetPath(i));
                Assert.AreEqual(QualityProfile.AssetName(i), Field(asset, "m_Name"));
                Assert.AreEqual(tier.ShadowDistance, float.Parse(Field(asset, "m_ShadowDistance"), c), 0.01f, tier.Name);
                Assert.AreEqual(tier.ShadowResolution.ToString(), Field(asset, "m_MainLightShadowmapResolution"), tier.Name);
                Assert.AreEqual(tier.Cascades.ToString(), Field(asset, "m_ShadowCascadeCount"), tier.Name);
                Assert.AreEqual(tier.Msaa.ToString(), Field(asset, "m_MSAA"), tier.Name);
                Assert.AreEqual(tier.RenderScale, float.Parse(Field(asset, "m_RenderScale"), c), 0.001f, tier.Name);
                Assert.AreEqual(tier.Hdr ? "1" : "0", Field(asset, "m_SupportsHDR"), tier.Name);
                Assert.AreEqual("1", Field(asset, "m_UseSRPBatcher"), tier.Name);
                Assert.AreEqual("1", Field(asset, "m_RequireDepthTexture"), tier.Name + " feeds decals, SSAO, and DoF");
                Assert.AreEqual(tier.Ssao ? "1" : "0", Field(asset, "m_PrefilteringModeScreenSpaceOcclusion"), tier.Name);
                StringAssert.Contains("guid: " + GuidOf(QualityProfile.RendererPath(i)), Regex.Match(asset, @"m_RendererDataList:\n  - (.*)").Groups[1].Value, tier.Name);
            }
        }

        [Test]
        public void TiersStepUpInCostAndLowDropsTheExpensivePasses()
        {
            for (int i = 1; i < QualityProfile.Count; i++)
            {
                var lower = QualityProfile.For(i - 1);
                var upper = QualityProfile.For(i);
                Assert.LessOrEqual(lower.ShadowDistance, upper.ShadowDistance);
                Assert.LessOrEqual(lower.ShadowResolution, upper.ShadowResolution);
                Assert.LessOrEqual(lower.Decals, upper.Decals);
                Assert.LessOrEqual(lower.Particles, upper.Particles);
                Assert.LessOrEqual(lower.Zombies, upper.Zombies);
                Assert.LessOrEqual(lower.LodBias, upper.LodBias);
            }
            var low = QualityProfile.For(0);
            Assert.IsFalse(low.Ssao);
            Assert.IsFalse(low.DepthOfField);
            Assert.IsFalse(low.Hdr);
            Assert.Less(low.RenderScale, 1f);
            Assert.AreEqual(QualityProfile.RendererLite, QualityProfile.RendererName(0));
            Assert.AreEqual(QualityProfile.RendererFull, QualityProfile.RendererName(1));
            Assert.IsTrue(QualityProfile.For(1).Ssao, "Medium keeps SSAO for the 1060 target");
            Assert.AreEqual(QualityProfile.AssetName(3), QualityProfile.AssetName(9), "out-of-range indices clamp");
        }

        [Test]
        public void OnlyTheFullRendererRunsSsaoAndBothKeepDecals()
        {
            string full = Read(QualityProfile.SettingsFolder.Replace('/', Path.DirectorySeparatorChar), QualityProfile.RendererFull + ".asset");
            string lite = Read(QualityProfile.SettingsFolder.Replace('/', Path.DirectorySeparatorChar), QualityProfile.RendererLite + ".asset");
            const string ssao = "f62c9c65cf3354c93be831c8bc075510";
            const string decal = "a1614fc811f8f184697d9bee70ab9fe5";
            StringAssert.Contains(ssao, full);
            StringAssert.DoesNotContain(ssao, lite);
            StringAssert.Contains(decal, full);
            StringAssert.Contains(decal, lite);
            foreach (string renderer in new[] { full, lite })
            {
                var ids = Regex.Matches(Regex.Match(renderer, @"m_RendererFeatures:\n((?:  - \{fileID: -?\d+\}\n)+)").Groups[1].Value, @"fileID: (-?\d+)");
                string map = Field(renderer, "m_RendererFeatureMap");
                Assert.AreEqual(ids.Count * 16, map.Length, "one 8-byte map entry per feature");
                for (int i = 0; i < ids.Count; i++)
                {
                    long id = long.Parse(ids[i].Groups[1].Value);
                    StringAssert.Contains("--- !u!114 &" + id + "\n", renderer, "feature object is in the file");
                    var bytes = System.BitConverter.GetBytes(id);
                    string hex = System.BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
                    Assert.AreEqual(hex, map.Substring(i * 16, 16), "map entry matches the little-endian file id");
                }
            }
            StringAssert.Contains("Ensure<ScreenSpaceAmbientOcclusion>(QualityProfile.SettingsFolder + \"/\" + QualityProfile.RendererFull", Read("Assets", "Scripts", "Editor", "RendererFeatureSetup.cs"));
        }

        [Test]
        public void SettingsSwitchTheQualityLevelAndCapLowCrowds()
        {
            string settings = Read("Assets", "Scripts", "Core", "SettingsService.cs");
            StringAssert.Contains("QualitySettings.SetQualityLevel(quality, true)", settings);
            StringAssert.Contains("QualitySettings.lodBias = tier.LodBias", settings);
            StringAssert.Contains("DifficultyProfile.AliveCap(quality)", settings);
            Assert.AreEqual(DifficultyProfile.LowTierAlive, DifficultyProfile.AliveCap(0));
            Assert.AreEqual(DifficultyProfile.LowTierAlive, QualityProfile.For(0).Zombies);
            Assert.AreEqual(32, DifficultyProfile.AliveCap(1));
            Assert.AreEqual(40, DifficultyProfile.AliveCap(3));
            Assert.LessOrEqual(DifficultyProfile.AliveCap(-2), DifficultyProfile.LowTierAlive);
        }

        [Test]
        public void EveryShaderIsSrpBatcherCompatible()
        {
            foreach (string path in Directory.GetFiles(Path.Combine(Root, "Assets", "Shaders"), "*.shader"))
            {
                string text = File.ReadAllText(path);
                string file = Path.GetFileName(path);
                var buffers = Regex.Matches(text, @"CBUFFER_START\(UnityPerMaterial\)([\s\S]*?)CBUFFER_END");
                Assert.GreaterOrEqual(buffers.Count, 1, file);
                for (int i = 1; i < buffers.Count; i++)
                    Assert.AreEqual(Regex.Replace(buffers[0].Groups[1].Value, @"\s+", " "), Regex.Replace(buffers[i].Groups[1].Value, @"\s+", " "), file + " passes share one layout");
                var block = Regex.Match(text, @"Properties\s*\{([\s\S]*?)\n    \}");
                Assert.IsTrue(block.Success, file + " Properties");
                foreach (Match property in Regex.Matches(block.Groups[1].Value, @"^\s*(?:\[[^\]]*\]\s*)*(_\w+)\s*\(""[^""]*"",\s*(\w+)", RegexOptions.Multiline))
                {
                    if (property.Groups[2].Value == "2D" || property.Groups[2].Value == "3D" || property.Groups[2].Value == "Cube") continue;
                    StringAssert.Contains(property.Groups[1].Value, buffers[0].Groups[1].Value, file + " " + property.Groups[1].Value);
                }
                if (Regex.IsMatch(text, "\"LightMode\" = \"ShadowCaster\""))
                    Assert.AreEqual(Regex.Matches(text, @"(?m)^\s*Pass\s*$").Count, Regex.Matches(text, "multi_compile_instancing").Count, file + " instances in every pass");
            }
        }

        [Test]
        public void RepeatedPropsAndDebrisInstance()
        {
            foreach (string path in Directory.GetFiles(Path.Combine(Root, "Assets", "Materials", "Baked"), "*.mat"))
                StringAssert.Contains("m_EnableInstancingVariants: 1", File.ReadAllText(path), Path.GetFileName(path));
            foreach (string path in Directory.GetFiles(Path.Combine(Root, "Assets", "Materials", "Library"), "*.mat"))
                StringAssert.Contains("m_EnableInstancingVariants: 1", File.ReadAllText(path), Path.GetFileName(path));
            StringAssert.Contains("material.enableInstancing = true", Read("Assets", "Scripts", "Editor", "FbxPrefabPostprocessor.cs"));
            StringAssert.Contains("enableInstancing = true", Read("Assets", "Scripts", "Expedition", "DebrisField.cs"));
        }

        [Test]
        public void SightChecksSpreadToEightZombiesAFrame()
        {
            Assert.AreEqual(1, QualityProfile.SightGroups(0));
            Assert.AreEqual(1, QualityProfile.SightGroups(8));
            Assert.AreEqual(2, QualityProfile.SightGroups(9));
            Assert.AreEqual(4, QualityProfile.SightGroups(32));
            Assert.AreEqual(5, QualityProfile.SightGroups(40));
            foreach (int alive in new[] { 16, 32, 40 })
            {
                int groups = QualityProfile.SightGroups(alive);
                for (int frame = 0; frame < groups; frame++)
                {
                    int due = 0;
                    for (int token = 1; token <= alive; token++)
                        if (QualityProfile.SightDue(token, frame, groups)) due++;
                    Assert.LessOrEqual(due, QualityProfile.SightPerFrame, alive + " alive, frame " + frame);
                }
                for (int token = 1; token <= alive; token++)
                {
                    int looks = 0;
                    for (int frame = 0; frame < groups; frame++)
                        if (QualityProfile.SightDue(token, frame, groups)) looks++;
                    Assert.AreEqual(1, looks, "each zombie looks once per sweep");
                }
            }
            Assert.IsTrue(QualityProfile.SightDue(7, 3, 1), "a small crowd looks every frame");
            string ai = Read("Assets", "Scripts", "AI", "ZombieAI.cs");
            StringAssert.Contains("SightDue(sightToken, Time.frameCount, SightGroupsThisFrame())", ai);
            StringAssert.Contains("LowQualityObstacleAvoidance", ai);
            StringAssert.Contains("AnimatorCullingMode.CullUpdateTransforms", Read("Assets", "Scripts", "AI", "ZombieMotion.cs"));
        }

        [Test]
        public void LodBandsSwitchAtTwentyEightAndCullAtSixtyMetres()
        {
            float size = 10f;
            float[] heights = LodBands.Heights(size, 2);
            Assert.AreEqual(2, heights.Length);
            Assert.Greater(heights[0], heights[1]);
            Assert.AreEqual(QualityProfile.LodSwitch, LodBands.Distance(size, heights[0]), 0.01f);
            Assert.AreEqual(QualityProfile.LodCull, LodBands.Distance(size, heights[1]), 0.01f);
            Assert.AreEqual(QualityProfile.LodCull, LodBands.Distance(4f, LodBands.Heights(4f, 1)[0]), 0.01f);
            float[] three = LodBands.Heights(6f, 3);
            Assert.Greater(three[0], three[1]);
            Assert.Greater(three[1], three[2]);
            Assert.AreEqual(1f, LodBands.ScreenHeight(0f, 10f));
            Assert.AreEqual(1f, LodBands.ScreenHeight(500f, 1f), "never above the whole screen");
            Assert.AreEqual(size * 0.5f / (40f * Mathf.Tan(27.5f * Mathf.Deg2Rad)), LodBands.ScreenHeight(size, 40f), 1e-5f, "Unity's relative-height formula");

            string post = Read("Assets", "Scripts", "Editor", "FbxPrefabPostprocessor.cs");
            StringAssert.Contains("OnPostprocessModel", post);
            StringAssert.Contains("LodBands.Heights(group.size, lods.Length)", post);
            StringAssert.Contains("Decimated copies named <name>_LOD1", Read("BlenderScripts", "pipeline.py"));
        }

        [Test]
        public void OcclusionBakesWallsNotClutter()
        {
            Assert.IsTrue(OcclusionPlan.Occludes(new Vector3(8f, 4f, 0.3f)), "wall");
            Assert.IsTrue(OcclusionPlan.Occludes(new Vector3(10f, 9f, 12f)), "building");
            Assert.IsFalse(OcclusionPlan.Occludes(new Vector3(1f, 1f, 1f)), "crate");
            Assert.IsFalse(OcclusionPlan.Occludes(new Vector3(0.3f, 5f, 0.3f)), "lamp post");
            Assert.IsFalse(OcclusionPlan.Occludes(new Vector3(4f, 0.2f, 0.4f)), "curb");
            string builder = Read("Assets", "Scripts", "Editor", "PrototypeSceneBuilder.cs");
            int save = builder.IndexOf("EditorSceneManager.SaveScene(scene, ScenePath);");
            int bake = builder.IndexOf("OcclusionBake.Run()");
            Assert.Greater(bake, save, "the bake needs a saved scene path");
            Assert.Greater(builder.IndexOf("EditorSceneManager.SaveScene(scene, ScenePath);", bake), bake, "and the scene is saved again with the data");
            string bakeSource = Read("Assets", "Scripts", "Editor", "OcclusionBake.cs");
            StringAssert.Contains("StaticOcclusionCulling.Compute()", bakeSource);
            StringAssert.Contains("OccluderStatic", bakeSource);
        }

        [TestCase("Assets/Models/Props/Prop_Dumpster_Albedo.png", TextureRole.Color)]
        [TestCase("Assets/Models/Props/Prop_Dumpster_Normal.png", TextureRole.Normal)]
        [TestCase("Assets/Models/Props/Prop_Dumpster_Mask.png", TextureRole.Linear)]
        [TestCase("Assets/Models/Props/Prop_Dumpster_AO.png", TextureRole.Linear)]
        [TestCase("Assets/Models/Props/Prop_Dumpster_Icon.png", TextureRole.Screen)]
        [TestCase("Assets/Textures/Decals/DecalAtlas.png", TextureRole.Screen)]
        [TestCase("Assets\\Materials\\Library\\Textures\\Asphalt_Normal.png", TextureRole.Normal)]
        public void TextureRolesComeFromTheSuffix(string path, TextureRole role)
        {
            Assert.AreEqual(role, TextureRules.RoleOf(path));
            Assert.IsTrue(TextureRules.Governs(path));
        }

        [Test]
        public void TextureFormatsMatchTheEditorEnum()
        {
            Assert.AreEqual((int)UnityEditor.TextureImporterFormat.BC7, TextureRules.BC7);
            Assert.AreEqual((int)UnityEditor.TextureImporterFormat.BC5, TextureRules.BC5);
            Assert.AreEqual((int)UnityEditor.TextureImporterFormat.ASTC_4x4, TextureRules.Astc4x4);
            Assert.AreEqual((int)UnityEditor.TextureImporterFormat.ASTC_6x6, TextureRules.Astc6x6);
            Assert.AreEqual(TextureRules.BC5, TextureRules.Desktop(TextureRole.Normal));
            Assert.AreEqual(TextureRules.BC7, TextureRules.Desktop(TextureRole.Color));
            Assert.AreEqual(TextureRules.Astc6x6, TextureRules.Mobile(TextureRole.Linear));
            Assert.IsFalse(TextureRules.Streams(TextureRole.Screen));
            Assert.IsTrue(TextureRules.Streams(TextureRole.Color));
            Assert.IsFalse(TextureRules.Governs("Assets/UI/Resources/HUD.uss"));
            Assert.IsFalse(TextureRules.Governs("Packages/foo/bar.png"));
        }

        [Test]
        public void EveryCommittedTextureIsCompressedMippedAndStreamedByRole()
        {
            int checkedCount = 0;
            foreach (string folder in new[] { "Models", "Materials", "Textures" })
            {
                foreach (string meta in Directory.GetFiles(Path.Combine(Root, "Assets", folder), "*.png.meta", SearchOption.AllDirectories))
                {
                    string relative = "Assets/" + meta.Substring(Path.Combine(Root, "Assets").Length + 1).Replace('\\', '/');
                    string png = relative.Substring(0, relative.Length - ".meta".Length);
                    string text = File.ReadAllText(meta);
                    var role = TextureRules.RoleOf(png);
                    Assert.AreEqual("1", Field(text, "enableMipMap"), png);
                    Assert.AreEqual(TextureRules.Streams(role) ? "1" : "0", Field(text, "streamingMipmaps"), png);
                    AssertPlatform(text, "Standalone", TextureRules.Desktop(role), png);
                    AssertPlatform(text, "Android", TextureRules.Mobile(role), png);
                    AssertPlatform(text, "iPhone", TextureRules.Mobile(role), png);
                    checkedCount++;
                }
            }
            Assert.Greater(checkedCount, 500);
        }

        static void AssertPlatform(string meta, string target, int format, string png)
        {
            var entry = Regex.Match(meta, @"buildTarget: " + target + @"\n(?:    .*\n)*?    textureFormat: (-?\d+)\n(?:    .*\n)*?    overridden: (\d)");
            Assert.IsTrue(entry.Success, png + " has a " + target + " override");
            Assert.AreEqual(format.ToString(), entry.Groups[1].Value, png + " " + target);
            Assert.AreEqual("1", entry.Groups[2].Value, png + " " + target + " overridden");
        }

        [Test]
        public void ImportPolicyHoldsHandDroppedTexturesToTheRules()
        {
            string policy = Read("Assets", "Scripts", "Editor", "TextureImportPolicy.cs");
            StringAssert.Contains("OnPreprocessTexture", policy);
            StringAssert.Contains("TextureRules.Governs(assetPath)", policy);
            StringAssert.Contains("importer.mipmapEnabled = true", policy);
            StringAssert.Contains("importer.streamingMipmaps = TextureRules.Streams(role)", policy);
            StringAssert.Contains("\"Standalone\"", policy);
            StringAssert.Contains("\"Android\", \"iPhone\"", policy);
        }
    }
}
