using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Core;
using OutpostZero.EditorTools;
using OutpostZero.Expedition;
using OutpostZero.Graphics;
using OutpostZero.Player;

namespace OutpostZero.Tests.EditMode
{
    public class MaterialLibraryTests
    {
        private static string Root => Directory.GetCurrentDirectory();

        private static string Path(params string[] parts)
        {
            var all = new List<string> { Root };
            all.AddRange(parts);
            return System.IO.Path.Combine(all.ToArray());
        }

        private static string Read(params string[] parts)
        {
            return File.ReadAllText(Path(parts)).Replace("\r\n", "\n");
        }

        private static string GuidOf(string assetPath)
        {
            var match = Regex.Match(File.ReadAllText(Path(assetPath + ".meta")), @"guid: ([0-9a-f]{32})");
            Assert.IsTrue(match.Success, assetPath);
            return match.Groups[1].Value;
        }

        private static IEnumerable<SurfaceFamily> Families()
        {
            foreach (SurfaceFamily family in Enum.GetValues(typeof(SurfaceFamily)))
                if (family != SurfaceFamily.None) yield return family;
        }

        [Test]
        public void BlenderMaterialNamesMapOntoFamilies()
        {
            Assert.AreEqual(SurfaceFamily.Rubber, MaterialLibrary.FamilyFor("Mat_Car_TireRubber"));
            Assert.AreEqual(SurfaceFamily.Glass, MaterialLibrary.FamilyFor("Mat_Window_Glass"));
            Assert.AreEqual(SurfaceFamily.Asphalt, MaterialLibrary.FamilyFor("Mat_Road_Asphalt"));
            Assert.AreEqual(SurfaceFamily.BrickRed, MaterialLibrary.FamilyFor("Mat_Brick_Red"));
            Assert.AreEqual(SurfaceFamily.BrickGrey, MaterialLibrary.FamilyFor("Mat_Ruin_Rubble"));
            Assert.AreEqual(SurfaceFamily.MetalRusted, MaterialLibrary.FamilyFor("Mat_Barrel_Rust"));
            Assert.AreEqual(SurfaceFamily.MetalPainted, MaterialLibrary.FamilyFor("Mat_Steel_Frame"));
            Assert.AreEqual(SurfaceFamily.Plywood, MaterialLibrary.FamilyFor("Mat_Wood_Pallet"));
            Assert.AreEqual(SurfaceFamily.TarpFabric, MaterialLibrary.FamilyFor("Mat_Tarp_Blue"));
            Assert.AreEqual(SurfaceFamily.Cloth, MaterialLibrary.FamilyFor("Mat_Jacket_Denim"));
            Assert.AreEqual(SurfaceFamily.RotFlesh, MaterialLibrary.FamilyFor("Mat_Zombie_Flesh"));
            Assert.AreEqual(SurfaceFamily.ConcreteCracked, MaterialLibrary.FamilyFor("Mat_Concrete_Jersey"));
        }

        [Test]
        public void FacesEyesAndPrintedMarksKeepTheirAuthoredColour()
        {
            foreach (var name in new[] { "Mat_Zombie_Eye", "Mat_Survivor_Skin", "Mat_Hazard_Stripes", "Mat_Crate_Label", "Mat_Fire_Embers" })
                Assert.AreEqual(SurfaceFamily.None, MaterialLibrary.FamilyFor(name), name);
            Assert.AreEqual(SurfaceFamily.None, MaterialLibrary.FamilyFor(""));
            Assert.AreEqual(SurfaceFamily.None, MaterialLibrary.FamilyFor("Mat_Unknown_Thing"));
        }

        [Test]
        public void TokensSplitOnSeparatorsDigitsAndCamelCase()
        {
            CollectionAssert.AreEqual(new[] { "car", "tire", "rubber" }, MaterialLibrary.Tokens("Mat_Car_TireRubber"));
            CollectionAssert.AreEqual(new[] { "crate", "wood" }, MaterialLibrary.Tokens("Crate (1).Wood"));
            CollectionAssert.AreEqual(new[] { "ibeam" }, MaterialLibrary.Tokens("IBeam"));
            Assert.IsEmpty(MaterialLibrary.Tokens(null));
        }

        [Test]
        public void UnknownPrimitivesReadAsCrackedConcrete()
        {
            Assert.AreEqual(SurfaceFamily.ConcreteCracked, MaterialLibrary.Guess("Cube"));
            Assert.AreEqual(SurfaceFamily.Asphalt, MaterialLibrary.Guess("Ground"));
            Assert.AreEqual(SurfaceFamily.BrickRed, MaterialLibrary.Guess("DistrictWall_3"));
        }

        [Test]
        public void GreysLeaveTheTextureAloneAndColoursCastTheirHue()
        {
            Assert.AreEqual(Color.white, MaterialLibrary.TintFor(new Color(0.3f, 0.3f, 0.32f)));
            var red = MaterialLibrary.TintFor(new Color(0.6f, 0.1f, 0.1f));
            Assert.AreEqual(1f, red.r, 1e-4f);
            Assert.Less(red.g, 0.6f);
            Assert.Greater(red.g, 0.4f);
            Assert.AreEqual(red.g, red.b, 1e-4f);
        }

        [Test]
        public void OnlyUnitysBuiltInGreyCountsAsDefault()
        {
            Assert.IsTrue(MaterialLibrary.IsDefaultName("Default-Material"));
            Assert.IsTrue(MaterialLibrary.IsDefaultName("Default-Material (Instance)"));
            Assert.IsTrue(MaterialLibrary.IsDefaultName("Lit"));
            Assert.IsTrue(MaterialLibrary.IsDefaultName(null));
            Assert.IsFalse(MaterialLibrary.IsDefaultName("MT_Asphalt"));
            Assert.IsFalse(MaterialLibrary.IsDefaultName("Kit_floor"));
        }

        [Test]
        public void SurfaceTagsAndNamesPickTheSafetyNetFamily()
        {
            Assert.AreEqual(SurfaceFamily.MetalPainted, UrpMaterialPass.Pick(SurfaceKind.Metal, "Cube"));
            Assert.AreEqual(SurfaceFamily.Plywood, UrpMaterialPass.Pick(SurfaceKind.Wood, "Road"));
            Assert.AreEqual(SurfaceFamily.Asphalt, UrpMaterialPass.Pick(SurfaceKind.Default, "Road"));
            Assert.AreEqual(SurfaceFamily.ConcreteCracked, UrpMaterialPass.Pick(SurfaceKind.Water, "Cube"));
            Assert.AreEqual(SurfaceFamily.None, MaterialLibrary.FromSurface(SurfaceKind.Water));
        }

        [Test]
        public void KitBoxesPreferTheirPieceWordsOverTheSurfaceTag()
        {
            Assert.AreEqual(SurfaceFamily.Plywood, KitStructure.KitFamily("plywood_door", SurfaceKind.Concrete));
            Assert.AreEqual(SurfaceFamily.MetalPainted, KitStructure.KitFamily("xyz", SurfaceKind.Metal));
            Assert.AreEqual(SurfaceFamily.ConcreteCracked, KitStructure.KitFamily("xyz", SurfaceKind.Default));
        }

        [Test]
        public void RuntimeStandInsGetASurfaceFamily()
        {
            Assert.AreEqual(SurfaceFamily.Rubber, StreetDetail.FamilyFor("tyre"));
            Assert.AreEqual(SurfaceFamily.Glass, StreetDetail.FamilyFor("bottle"));
            Assert.AreEqual(SurfaceFamily.None, StreetDetail.FamilyFor("stripe"));
            Assert.AreEqual(SurfaceFamily.None, StreetDetail.FamilyFor("bulb"));
            Assert.AreEqual(SurfaceFamily.Plywood, GridBuilder.FamilyFor("Workbench"));
            Assert.AreEqual(SurfaceFamily.MetalRusted, GridBuilder.FamilyFor("Turret"));
            Assert.AreEqual(SurfaceFamily.None, GridBuilder.FamilyFor("Campfire"));
            Assert.AreEqual(SurfaceFamily.MetalRusted, LootPickup.FamilyFor(LootKind.Scrap));
            Assert.AreEqual(SurfaceFamily.Plywood, LootPickup.FamilyFor(LootKind.Ammo9mm));
        }

        [Test]
        public void RimsReadByFaction()
        {
            Assert.AreEqual(SurfaceRim.Zombie, SurfaceRim.For("Zombie_Walker"));
            Assert.AreEqual(SurfaceRim.Survivor, SurfaceRim.For("Survivor_Scout"));
            Assert.AreEqual(SurfaceRim.Survivor, SurfaceRim.For("Colonist_Survivor"));
            Assert.AreEqual(SurfaceRim.Prop, SurfaceRim.For("Kit_floor"));
            Assert.Greater(SurfaceRim.Survivor.b, SurfaceRim.Survivor.r);
            Assert.Greater(SurfaceRim.Zombie.g, SurfaceRim.Zombie.b);
            Assert.Less(SurfaceRim.Prop.a, SurfaceRim.Survivor.a);
        }

        [Test]
        public void BlenderFbxNumbersBecomeUrpValues()
        {
            Assert.AreEqual(0.3f, ImportedSurface.Smoothness(3f), 1e-5f);
            Assert.AreEqual(1f, ImportedSurface.Smoothness(40f));
            Assert.AreEqual(0f, ImportedSurface.Metallic(-1f));
            Assert.AreEqual(0.8f, ImportedSurface.Metallic(0.8f), 1e-5f);
            Assert.IsTrue(ImportedSurface.Glows(0.5f, 0f, 0f));
            Assert.IsFalse(ImportedSurface.Glows(0f, 0f, 0f));
        }

        [Test]
        public void HitFlashPeaksAtTheHitAndFadesOut()
        {
            Assert.AreEqual(1f, HitGlowCurve.Value(0f), 1e-5f);
            Assert.Less(HitGlowCurve.Value(HitGlowCurve.Length * 0.5f), 0.3f);
            Assert.AreEqual(0f, HitGlowCurve.Value(HitGlowCurve.Length));
            Assert.AreEqual(0f, HitGlowCurve.Value(-0.1f));
        }

        [Test]
        public void EveryFamilyShipsTexturesAndMaterials()
        {
            foreach (var family in Families())
            {
                foreach (var map in new[] { "Albedo", "Normal", "Mask" })
                    Assert.IsTrue(File.Exists(Path(MaterialLibrary.Folder, "Textures", family + "_" + map + ".png")), family + " " + map);
                Assert.IsTrue(File.Exists(Path(MaterialLibrary.LitPath(family))), family + " lit");
                bool triplanar = File.Exists(Path(MaterialLibrary.TriplanarPath(family)));
                Assert.AreEqual(family != SurfaceFamily.Glass, triplanar, family + " triplanar");
            }
        }

        [Test]
        public void LibraryAssetPointsAtEveryMaterial()
        {
            string asset = Read("Assets", "Resources", "MaterialLibrary.asset");
            StringAssert.Contains("guid: " + GuidOf("Assets/Scripts/Graphics/MaterialLibrary.cs"), asset);
            foreach (var family in Families())
            {
                StringAssert.Contains("- family: " + (int)family + "\n", asset);
                StringAssert.Contains("guid: " + GuidOf(MaterialLibrary.LitPath(family)), asset, family + " lit");
                if (family != SurfaceFamily.Glass)
                    StringAssert.Contains("guid: " + GuidOf(MaterialLibrary.TriplanarPath(family)), asset, family + " triplanar");
            }
        }

        [Test]
        public void LitMaterialsUseUrpLitWithAllMaps()
        {
            foreach (var family in Families())
            {
                string mat = Read(MaterialLibrary.LitPath(family));
                StringAssert.Contains("guid: 933532a4fcc9baf4fa0491de14d08ed7", mat, family.ToString());
                StringAssert.Contains("_NORMALMAP", mat, family.ToString());
                StringAssert.Contains("_METALLICSPECGLOSSMAP", mat, family.ToString());
                StringAssert.Contains("_OCCLUSIONMAP", mat, family.ToString());
            }
        }

        [Test]
        public void TriplanarMaterialsUseTheEnvironmentShader()
        {
            string shader = GuidOf("Assets/Shaders/OutpostEnvironment.shader");
            foreach (var family in Families())
            {
                if (family == SurfaceFamily.Glass) continue;
                StringAssert.Contains("guid: " + shader, Read(MaterialLibrary.TriplanarPath(family)), family.ToString());
            }
        }

        [TestCase("OutpostEnvironment.shader")]
        [TestCase("OutpostTriplanarRim.shader")]
        public void ShadersAreSrpBatcherReadyWithEveryPass(string file)
        {
            string text = Read("Assets", "Shaders", file);
            foreach (var pass in new[] { "UniversalForward", "ShadowCaster", "DepthOnly", "DepthNormals" })
                StringAssert.Contains("\"LightMode\" = \"" + pass + "\"", text, pass);
            var buffer = Regex.Match(text, @"CBUFFER_START\(UnityPerMaterial\)([\s\S]*?)CBUFFER_END");
            Assert.IsTrue(buffer.Success, "UnityPerMaterial");
            var block = Regex.Match(text, @"Properties\s*\{([\s\S]*?)\n    \}");
            Assert.IsTrue(block.Success, "Properties");
            foreach (Match property in Regex.Matches(block.Groups[1].Value, @"^\s*(?:\[[^\]]*\]\s*)*(_\w+)\s*\(", RegexOptions.Multiline))
            {
                string name = property.Groups[1].Value;
                if (Regex.IsMatch(block.Groups[1].Value, @"(?:\[[^\]]*\]\s*)*" + name + @"\s*\(""[^""]*"",\s*2D\)")) continue;
                StringAssert.Contains(name, buffer.Groups[1].Value, file + " " + name);
            }
        }

        [Test]
        public void CharacterShaderKeepsTheBakedPropertyNames()
        {
            string text = Read("Assets", "Shaders", "OutpostTriplanarRim.shader");
            foreach (var name in new[] { "_BaseMap", "_BumpMap", "_MaskMap", "_OcclusionMap", "_HasMaps", "_RimColor", "_RimPower", "_Tint", "_HitFlash", "_Dissolve" })
                StringAssert.Contains(name, text, name);
        }

        [Test]
        public void FlatPlaceholderMaterialsAreGone()
        {
            Assert.IsEmpty(Directory.GetFiles(Path("Assets", "Materials"), "Mat_*.mat"));
            string builder = Read("Assets", "Scripts", "Editor", "PrototypeSceneBuilder.cs");
            StringAssert.DoesNotContain("GetOrCreateMaterial", builder);
            StringAssert.Contains("SurfaceFamily.Asphalt", builder);
            StringAssert.Contains("guid: " + GuidOf(MaterialLibrary.TriplanarPath(SurfaceFamily.Asphalt)), Read("Assets", "Scenes", "PrototypeArena.unity"));
        }

        [Test]
        public void BakedCharacterMaterialsCarryFactionRims()
        {
            string zombie = Directory.GetFiles(Path("Assets", "Materials", "Baked"), "Zombie_*.mat")[0];
            StringAssert.Contains("_RimColor: {r: 0.55, g: 0.85, b: 0.25, a: 0.6}", File.ReadAllText(zombie));
            StringAssert.Contains("_RimColor: {r: 0.3, g: 0.85, b: 1, a: 0.5}", Read("Assets", "Materials", "Baked", "Colonist_Survivor.mat"));
        }
    }
}
