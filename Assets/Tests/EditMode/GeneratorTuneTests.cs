using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;
using OutpostZero.Shell;

namespace OutpostZero.Tests.EditMode
{
    public class GeneratorTuneTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        [Test]
        public void AGeneratorReachesTierTwoOnceTheJobIsDone()
        {
            Assert.AreEqual(1, GeneratorTune.Tier(1, 0));
            Assert.AreEqual(1, GeneratorTune.Tier(1, 2), "half the hours still run at tier 1");
            Assert.AreEqual(2, GeneratorTune.Tier(1, CraftGate.Done));
            Assert.AreEqual(2, GeneratorTune.Tier(2, 0));
            Assert.IsTrue(GeneratorTune.Raises("Generator"));
            Assert.IsTrue(GeneratorTune.Raises("Workbench"));
            Assert.IsFalse(GeneratorTune.Raises("Barricade"));
        }

        [Test]
        public void ABuilderWalksToAGeneratorBeingTuned()
        {
            var kinds = new[] { "Generator" };
            Assert.AreEqual(0, CampPost.Pick("Build", kinds, new[] { 0 }, new[] { 100 }, new[] { 2 }));
            Assert.AreEqual(-1, CampPost.Pick("Build", kinds, new[] { 0 }, new[] { 100 }, new[] { CraftGate.Done }));
            Assert.AreEqual(-1, CampPost.Pick("Build", new[] { "Lamp" }, new[] { 0 }, new[] { 100 }, new[] { 2 }));
        }

        [Test]
        public void TheBroadcastWaitsOnATierTwoGenerator()
        {
            string map = Read("Assets/Scripts/Shell/WorldMapService.cs");
            StringAssert.Contains("GridBuilder.Instance.GeneratorTier() >= 2", map);
            StringAssert.Contains("CampaignBoard.Ready(parts, GeneratorRaised, broadcastWon)", map);
            StringAssert.Contains("CampaignBoard.Won(parts, GeneratorRaised, broadcastWon)", map);
            string grid = Read("Assets/Scripts/Colony/GridBuilder.cs");
            StringAssert.Contains("Order(\"Generator\", GeneratorTune.Scrap, 0, GeneratorTune.Chemicals, GeneratorTune.Tape, \"camp.gen_raise\")", grid);
            StringAssert.Contains("if (!GeneratorTune.Raises(module.kind)", grid);
            string ui = Read("Assets/Scripts/UI/OutpostInterface.cs");
            StringAssert.Contains("GridBuilder.Instance?.OrderGenerator()", ui);
            StringAssert.Contains("key.Add(GridBuilder.Instance.GeneratorTier());", ui);
            StringAssert.Contains("tier-2 generator", Loc.T("camp.tower_needs", "en"));
            Assert.AreEqual("Generador T2", Loc.T("camp.gen_t2", "es"));
        }
    }
}
