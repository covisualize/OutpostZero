using System.IO;
using NUnit.Framework;
using OutpostZero.UI;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-31: the objective world marker and its off-screen arrow.</summary>
    public class WaypointTests
    {
        private const float W = 1920f, H = 1080f;

        [Test]
        public void AnOnScreenTargetGetsAMarkerPointingDownAtIt()
        {
            Assert.IsTrue(EdgeArrow.Place(700f, 400f, false, W, H, out float x, out float y, out float angle));
            Assert.AreEqual(700f, x);
            Assert.AreEqual(400f, y);
            Assert.AreEqual(180f, angle);
        }

        [Test]
        public void AnOffScreenTargetPinsToTheEdgeAndPointsAtIt()
        {
            Assert.IsFalse(EdgeArrow.Place(4000f, 540f, false, W, H, out float x, out float y, out float angle));
            Assert.AreEqual(W - EdgeArrow.Margin, x, 0.01f, "right edge");
            Assert.AreEqual(540f, y, 0.01f);
            Assert.AreEqual(90f, angle, 0.01f, "points right");

            EdgeArrow.Place(960f, -900f, false, W, H, out x, out y, out angle);
            Assert.AreEqual(EdgeArrow.Margin, y, 0.01f, "top edge");
            Assert.AreEqual(0f, angle, 0.01f, "points up");

            EdgeArrow.Place(W / 2f - 3000f, H / 2f + 3000f, false, W, H, out x, out y, out angle);
            Assert.GreaterOrEqual(x, EdgeArrow.Margin - 0.01f);
            Assert.LessOrEqual(y, H - EdgeArrow.Margin + 0.01f);
            Assert.AreEqual(-135f, angle, 1f, "down and left");
        }

        [Test]
        public void ATargetBehindTheCameraFlipsToTheFarEdge()
        {
            Assert.IsFalse(EdgeArrow.Place(1100f, 300f, true, W, H, out float x, out float y, out float angle));
            Assert.Less(x, W * 0.5f, "mirrored to the left");
            Assert.AreEqual(H - EdgeArrow.Margin, y, 0.01f, "behind reads as below");
            Assert.Greater(System.Math.Abs(angle), 90f);
            Assert.IsFalse(EdgeArrow.Place(960f, 540f, true, W, H, out _, out y, out angle), "dead centre behind still pins");
            Assert.AreEqual(H - EdgeArrow.Margin, y, 0.01f);
            Assert.AreEqual(180f, System.Math.Abs(angle), 0.01f);
        }

        [Test]
        public void TheLabelReadsWholeMetres()
        {
            Assert.AreEqual(5, EdgeArrow.Metres(0f, 0f, 3f, 4f));
            Assert.AreEqual("24 m", EdgeArrow.Label(24, "en"));
            Assert.AreEqual("0 m", EdgeArrow.Label(-3, "es"));
        }

        [Test]
        public void TheControllerDrivesTheWaypointFromTheObjectives()
        {
            string controller = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts", "UI", "HudController.cs"));
            StringAssert.Contains("Waypoint();", controller);
            StringAssert.Contains("tracker.ReadyToExtract", controller);
            StringAssert.Contains("EdgeArrow.Place(spot.x, spot.y, behind,", controller);
            bool found = false;
            foreach (var node in HudTree.Nodes) if (node.Name == "waypoint-arrow" && node.Parent == "waypoint") found = true;
            Assert.IsTrue(found);
        }
    }
}
