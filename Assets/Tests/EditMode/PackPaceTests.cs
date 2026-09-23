using System.IO;
using NUnit.Framework;
using OutpostZero.Items;

namespace OutpostZero.Tests.EditMode
{
    public class PackPaceTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        [Test]
        public void ThePackStopsTheStreetExceptOnNightmare()
        {
            Assert.AreEqual(0f, PackView.Pace(1));
            Assert.AreEqual(0f, PackView.Pace(2));
            Assert.AreEqual(0.15f, PackView.Pace(3), 0.0001f);
        }

        [Test]
        public void TheShellSlowsTheStreetAndTheGunHoldsWhileThePackShows()
        {
            string shell = Read("Assets/Scripts/UI/GameShellUI.cs");
            StringAssert.Contains("PackView.Showing = inventoryOpen;", shell);
            StringAssert.Contains("PackView.Pace(DifficultyProfile.Active)", shell);
            StringAssert.Contains("if (street) Time.timeScale = 1f;", shell);
            StringAssert.Contains("PackView.Showing ||", Read("Assets/Scripts/Player/PlayerController.cs"));
        }
    }
}
