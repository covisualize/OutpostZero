using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-58: a perimeter integrity score from wall coverage feeds raid difficulty.</summary>
    public class PerimeterTests
    {
        private static Perimeter.Wall At(string approach, int integrity, bool ready = true)
        {
            RaidPlan.AnchorOf(approach, out float x, out float z);
            return new Perimeter.Wall { X = x + 1f, Z = z, Integrity = integrity, Ready = ready };
        }

        [Test]
        public void TwoWholeBarricadesFillASide()
        {
            Assert.AreEqual(0, Perimeter.Side("gate", new List<Perimeter.Wall>()));
            Assert.AreEqual(50, Perimeter.Side("gate", new[] { At("gate", 100) }));
            Assert.AreEqual(100, Perimeter.Side("gate", new[] { At("gate", 100), At("gate", 100), At("gate", 100) }));
            Assert.AreEqual(25, Perimeter.Side("gate", new[] { At("gate", 50) }));
        }

        [Test]
        public void SitesBrokenWallsAndFarWallsDoNotCount()
        {
            var walls = new[] { At("gate", 100, false), At("gate", 0), At("yard", 100) };
            Assert.AreEqual(0, Perimeter.Side("gate", walls));
            Assert.AreEqual(50, Perimeter.Side("yard", walls));
        }

        [Test]
        public void TheScoreAveragesTheFourSidesAndNamesTheWeakest()
        {
            var walls = new List<Perimeter.Wall> { At("gate", 100), At("gate", 100), At("alley", 100), At("alley", 100), At("yard", 100), At("yard", 100) };
            Assert.AreEqual(75, Perimeter.Score(walls));
            Assert.AreEqual("fence", Perimeter.Weakest(walls));
            walls.Add(At("fence", 100));
            walls.Add(At("fence", 100));
            Assert.AreEqual(100, Perimeter.Score(walls));
            Assert.AreEqual("gate", Perimeter.Weakest(new List<Perimeter.Wall>()));
        }

        [Test]
        public void AGappyWallDrawsMoreAndACoveredSideTakesLess()
        {
            Assert.AreEqual(12, Perimeter.Crowd(10, 0));
            Assert.AreEqual(10, Perimeter.Crowd(10, 50));
            Assert.AreEqual(8, Perimeter.Crowd(10, 100));
            Assert.AreEqual(3, Perimeter.Crowd(4, 100));
            Assert.AreEqual(8, Perimeter.Pressure(8, 0));
            Assert.AreEqual(7, Perimeter.Pressure(8, 50));
            Assert.AreEqual(6, Perimeter.Pressure(8, 100));
            Assert.AreEqual(1, Perimeter.Pressure(1, 100));
        }

        [Test]
        public void TheRaidAndTheBoardReadTheScore()
        {
            string root = Directory.GetCurrentDirectory();
            string raid = File.ReadAllText(Path.Combine(root, "Assets/Scripts/Colony/NightRaidController.cs"));
            string board = File.ReadAllText(Path.Combine(root, "Assets/Scripts/UI/OutpostInterface.cs"));
            StringAssert.Contains("Perimeter.Crowd(", raid);
            StringAssert.Contains("Perimeter.Pressure(pressure, Perimeter.Side(approach", raid);
            StringAssert.Contains("Loc.T(\"camp.perimeter\")", board);
        }
    }
}
