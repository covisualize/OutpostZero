using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;

namespace OutpostZero.Tests.EditMode
{
    public class CampPickTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        [Test]
        public void TheNearestColonistInsideReachIsPicked()
        {
            var xs = new[] { 0f, 3f, 3.5f };
            var zs = new[] { 0f, 0f, 0f };
            Assert.AreEqual(2, CampPick.Nearest(3.4f, 0.2f, xs, zs, CampPick.Reach));
            Assert.AreEqual(0, CampPick.Nearest(0.5f, 0.5f, xs, zs, CampPick.Reach));
            Assert.AreEqual(-1, CampPick.Nearest(10f, 10f, xs, zs, CampPick.Reach));
            Assert.AreEqual(-1, CampPick.Nearest(0f, 0f, null, zs, CampPick.Reach));
        }

        [Test]
        public void TheCardNamesAColonistsTaskAndAModulesState()
        {
            var mate = new Survivor { id = "a", displayName = "Rosa", task = "Guard", morale = 60f };
            string card = CampPick.Mate(mate, "en");
            StringAssert.StartsWith("Rosa", card);
            StringAssert.Contains("Guard", card);
            Assert.AreEqual("", CampPick.Mate(null, "en"));

            var site = new PlacedModule { kind = "Cot", site = 1, hours = 2, integrity = 100 };
            StringAssert.Contains("site 2/", CampPick.Module(site, "en"));
            var worn = new PlacedModule { kind = "Barricade", integrity = 40 };
            StringAssert.Contains("40%", CampPick.Module(worn, "en"));
            StringAssert.Contains("needs repair", CampPick.Module(worn, "en"));
            var broken = new PlacedModule { kind = "Barricade", integrity = 0 };
            StringAssert.Contains("wrecked", CampPick.Module(broken, "en"));
            Assert.AreEqual("", CampPick.Module(null, "en"));
        }

        [Test]
        public void ClicksOutsideBuildModeSelectAndTheBoardShowsThePick()
        {
            string select = Read("Assets/Scripts/Colony/CampSelect.cs");
            StringAssert.Contains("!building && Player.ExpeditionInput.BuildPlacePressed", select);
            StringAssert.Contains("GridBuilder.Under(GridBuilder.Instance.Placed", select);
            StringAssert.Contains("gameObject.AddComponent<CampSelect>()", Read("Assets/Scripts/Colony/CampPopulation.cs"));
            string board = Read("Assets/Scripts/UI/OutpostInterface.cs");
            StringAssert.Contains("Loc.T(\"pick.title\") + \" \" + picked", board);
            StringAssert.Contains("CampSelect.Instance.SurvivorId == survivor.id", board);
            StringAssert.Contains("key.Add(CampSelect.Instance.Version);", board);
        }
    }
}
