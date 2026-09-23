using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-58: modules make or draw power; a generator feeds lamps and turrets in placement order.</summary>
    public class PowerGridTests
    {
        private static PowerGrid.Plug P(string kind, bool ready = true) => new PowerGrid.Plug { Kind = kind, Ready = ready };

        [Test]
        public void GeneratorsMakeAndLampsAndTurretsDraw()
        {
            Assert.AreEqual(6, PowerGrid.CodePower("Generator"));
            Assert.AreEqual(-1, PowerGrid.CodePower("Lamp"));
            Assert.AreEqual(-2, PowerGrid.CodePower("Turret"));
            Assert.AreEqual(0, PowerGrid.CodePower("Barricade"));
            Assert.AreEqual(0, PowerGrid.CodePower("Farm"));
        }

        [Test]
        public void NoFuelMeansNoPowerAndTheSceneGeneratorStillMakesOne()
        {
            var plugs = new List<PowerGrid.Plug> { P("Lamp"), P("Turret") };
            Assert.AreEqual(0, PowerGrid.Supply(plugs, false));
            Assert.AreEqual(PowerGrid.GeneratorOutput, PowerGrid.Supply(plugs, true), "a scene generator with none placed");
            plugs.Add(P("Generator"));
            plugs.Add(P("Generator"));
            Assert.AreEqual(12, PowerGrid.Supply(plugs, true));
            plugs.Add(P("Generator", false));
            Assert.AreEqual(12, PowerGrid.Supply(plugs, true), "a site makes nothing");
            Assert.AreEqual(3, PowerGrid.Demand(plugs));
        }

        [Test]
        public void PowerGoesOutInPlacementOrderUntilItRunsOut()
        {
            var plugs = new List<PowerGrid.Plug>
            {
                P("Generator"), P("Turret"), P("Lamp"), P("Turret"), P("Lamp", false), P("Turret"), P("Lamp"), P("Barricade")
            };
            var fed = PowerGrid.Allot(plugs, true);
            CollectionAssert.AreEqual(new[] { true, true, true, true, false, false, true, true }, fed);
            Assert.AreEqual(6, PowerGrid.Used(plugs, true));

            var dark = PowerGrid.Allot(plugs, false);
            CollectionAssert.AreEqual(new[] { false, false, false, false, false, false, false, true }, dark, "walls stand without power");
            Assert.AreEqual(0, PowerGrid.Used(plugs, false));
        }

        [Test]
        public void TheYardAndTheRaidReadTheGrid()
        {
            string root = Directory.GetCurrentDirectory();
            string grid = File.ReadAllText(Path.Combine(root, "Assets/Scripts/Colony/GridBuilder.cs"));
            string raid = File.ReadAllText(Path.Combine(root, "Assets/Scripts/Colony/NightRaidController.cs"));
            string board = File.ReadAllText(Path.Combine(root, "Assets/Scripts/UI/OutpostInterface.cs"));
            StringAssert.Contains("bool on = i < fed.Length && fed[i];", grid);
            StringAssert.Contains("FedCount(\"Turret\")", raid);
            StringAssert.Contains("FedCount(\"Lamp\")", raid);
            StringAssert.Contains("modules[i].kind == \"Lamp\" && fed[i]", raid);
            StringAssert.Contains("Loc.T(\"camp.power\")", board);
        }
    }
}
