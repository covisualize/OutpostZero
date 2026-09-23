using System.IO;
using NUnit.Framework;
using OutpostZero.Expedition;

namespace OutpostZero.Tests.EditMode
{
    public class KitVariantTests
    {
        private static string Read(string rel)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), rel)).Replace("\r\n", "\n");
        }

        [Test]
        public void APieceTakesItsOwnVariantOnlyWhenItOffersIt()
        {
            var wall = new KitPiece { id = "wall_plain", surface = "concrete", variants = new[] { "brick", "concrete", "plaster" } };
            var shelf = new KitPiece { id = "shelf", surface = "metal", variants = new[] { "metal" } };
            Assert.AreEqual("plaster", KitPlan.VariantFor(wall, "plaster", "brick"));
            Assert.AreEqual("brick", KitPlan.VariantFor(wall, "", "brick"));
            Assert.AreEqual("brick", KitPlan.VariantFor(wall, "marble", "brick"));
            Assert.AreEqual("concrete", KitPlan.VariantFor(shelf, "plaster", "concrete"));
            Assert.AreEqual("brick", KitPlan.VariantFor(null, "plaster", "brick"));
            Assert.AreNotEqual(KitPlan.Tint(wall, "plaster"), KitPlan.Tint(wall, "brick"));
        }

        [Test]
        public void TheSceneNameCarriesTheVariantBothWays()
        {
            Assert.AreEqual("Kit_wall_plain", KitPlan.HostName("wall_plain", ""));
            Assert.AreEqual("Kit_wall_plain|plaster", KitPlan.HostName("wall_plain", "plaster"));
            Assert.IsTrue(KitPlan.ReadHost("Kit_wall_plain|plaster", out string id, out string look));
            Assert.AreEqual("wall_plain", id);
            Assert.AreEqual("plaster", look);
            Assert.IsTrue(KitPlan.ReadHost("Kit_corner", out id, out look));
            Assert.AreEqual("corner", id);
            Assert.AreEqual("", look);
            Assert.IsFalse(KitPlan.ReadHost("KitMesh", out _, out _));
            Assert.IsFalse(KitPlan.ReadHost("Kit_", out _, out _));
        }

        [Test]
        public void TheAssemblerPicksAndExportsVariantsAndTheRuntimeHonoursThem()
        {
            string assembler = Read("Assets/Scripts/Editor/KitAssembler.cs");
            StringAssert.Contains("new DropdownField(\"Variant\"", assembler);
            StringAssert.Contains("placement.variant = look;", assembler);
            StringAssert.Contains("KitPlan.HostName(placement.id, placement.variant)", assembler);
            string runtime = Read("Assets/Scripts/Expedition/KitStructure.cs");
            StringAssert.Contains("KitPlan.VariantFor(piece, placement.variant, variant)", runtime);
            StringAssert.Contains("KitPlan.Shade(piece, look)", runtime);
        }
    }
}
