using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Graphics;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>
    /// PRO-58: modules claim whole metres of ground, turn with the facing, and placement refuses overlaps and fence crossings.
    /// </summary>
    public class ModuleFootprintTests
    {
        static string Root => Directory.GetCurrentDirectory();

        [Test]
        public void FootprintsRoundTheTableSizeUpToWholeMetres()
        {
            ModuleFootprint.Cells("Farm", out int wide, out int deep);
            Assert.AreEqual(3, wide);
            Assert.AreEqual(3, deep);
            ModuleFootprint.Cells("Cot", out wide, out deep);
            Assert.AreEqual(2, wide);
            Assert.AreEqual(1, deep);
            ModuleFootprint.Cells("Barricade", out wide, out deep);
            Assert.AreEqual(2, wide);
            Assert.AreEqual(1, deep);
            ModuleFootprint.Cells("Turret", out wide, out deep);
            Assert.AreEqual(1, wide);
            Assert.AreEqual(1, deep);
        }

        [Test]
        public void AQuarterTurnSwapsTheSpan()
        {
            ModuleFootprint.Span("Cot", 90, out int alongX, out int alongZ);
            Assert.AreEqual(1, alongX);
            Assert.AreEqual(2, alongZ);
            ModuleFootprint.Span("Cot", 270, out alongX, out alongZ);
            Assert.AreEqual(1, alongX);
            ModuleFootprint.Span("Cot", -90, out alongX, out alongZ);
            Assert.AreEqual(1, alongX);
            ModuleFootprint.Span("Cot", 180, out alongX, out alongZ);
            Assert.AreEqual(2, alongX);
            Assert.AreEqual(1, alongZ);
        }

        [Test]
        public void BigModulesCrowdTheirNeighboursButWallsRunEndToEnd()
        {
            var yard = new List<PlacedModule> { new PlacedModule { kind = "Farm", x = 0f, z = 0f } };
            Assert.AreEqual(BuildGhost.Verdict.Taken, BuildGhost.Check(yard, "Cot", 0, 2f, 0f, 0, 0), "a cot laid long runs into the farm");
            Assert.AreEqual(BuildGhost.Verdict.Ok, BuildGhost.Check(yard, "Cot", 90, 2f, 0f, 0, 0), "turned, it only touches the edge");
            Assert.AreEqual(BuildGhost.Verdict.Ok, BuildGhost.Check(yard, "Turret", 0, 2f, 0f, 0, 0));
            Assert.AreEqual(BuildGhost.Verdict.Taken, BuildGhost.Check(yard, "Turret", 0, 0f, 0f, 0, 0));

            var wall = new List<PlacedModule>
            {
                new PlacedModule { kind = "Barricade", x = 0f, z = 10f },
                new PlacedModule { kind = "Barricade", x = 2f, z = 10f }
            };
            Assert.AreEqual(BuildGhost.Verdict.Ok, BuildGhost.Check(wall, "Barricade", 0, 4f, 10f, 0, 0));
            Assert.AreEqual(BuildGhost.Verdict.Ok, BuildGhost.Check(wall, "Barricade", 90, 6f, 10f, 0, 0), "a corner post turns off the run");
            Assert.AreEqual(BuildGhost.Verdict.Ok, BuildGhost.Check(wall, "Barricade", 0, 0f, 12f, 0, 0), "a second rank behind it");
            Assert.IsFalse(GridBuilder.Occupied(wall, 4f, 10f));
        }

        [Test]
        public void TheWholeFootprintHasToBeInsideTheFence()
        {
            float edge = BuildGhost.Snap(MapRim.Half - 1.4f, 2f);
            Assert.IsTrue(MapRim.Inside(edge, 0f));
            Assert.AreEqual(BuildGhost.Verdict.Ok, BuildGhost.Check(null, "Turret", 0, edge, 0f, 0, 0));
            Assert.AreEqual(BuildGhost.Verdict.Outside, BuildGhost.Check(null, "Farm", 0, edge, 0f, 0, 0), "the farm's far rows would cross the wire");
            Assert.AreEqual(BuildGhost.Verdict.Short, BuildGhost.Check(null, "Turret", 0, 0f, 0f, 9, 3));
        }

        [Test]
        public void ADemolishClickAnywhereOnTheFootprintFindsTheModule()
        {
            var farm = new PlacedModule { kind = "Farm", x = 0f, z = 0f };
            var turret = new PlacedModule { kind = "Turret", x = 2f, z = 0f };
            var yard = new List<PlacedModule> { farm, turret };
            Assert.AreSame(farm, GridBuilder.Under(yard, 0f, 0f, 0.3f, 0.2f));
            Assert.AreSame(turret, GridBuilder.Under(yard, 2f, 0f, 1.2f, 0f), "the anchor on the snapped cell wins");
            Assert.AreSame(farm, GridBuilder.Under(yard, 0f, 2f, 1.2f, 1.3f), "off-anchor, the farm's footprint holds the point");
            Assert.IsNull(GridBuilder.Under(yard, 0f, 4f, 0f, 3.6f));
            Assert.IsNull(GridBuilder.Under(null, 0f, 0f, 0f, 0f));
        }

        [Test]
        public void TheGhostAndThePlacementBothReadTheTurnedFootprint()
        {
            string source = File.ReadAllText(Path.Combine(Root, "Assets/Scripts/Colony/GridBuilder.cs"));
            StringAssert.Contains("BuildGhost.Check(placed, selected, facing, x, z", source);
            StringAssert.Contains("ModuleFootprint.Of(kind.ToString(), facing, x, z)", source);
            StringAssert.Contains("ModuleFootprint.Clashes(placed, area)", source);
            StringAssert.Contains("Under(placed, x, z, worldX, worldZ)", source);
        }
    }
}
