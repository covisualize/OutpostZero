using System.IO;
using NUnit.Framework;
using OutpostZero.Colony;

namespace OutpostZero.Tests.EditMode
{
    public class YardBubbleTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        private static float ShownShare(string id, float morale)
        {
            int up = 0;
            int steps = 2800;
            for (int i = 0; i < steps; i++)
                if (YardBubble.Up(id, i * 0.05f, morale)) up++;
            return up / (float)steps;
        }

        [Test]
        public void EachColonistSpeaksAQuarterOfTheCycleAndABreakdownSpeaksMore()
        {
            float calm = ShownShare("mate_a", 60f);
            Assert.AreEqual(YardBubble.Shown / YardBubble.Period, calm, 0.02f);
            Assert.Greater(ShownShare("mate_a", 5f), calm * 1.8f);
            Assert.IsFalse(YardBubble.Up("", 1f, 60f));
            Assert.IsFalse(YardBubble.Up("mate_a", -1f, 60f));
        }

        [Test]
        public void ColonistsAreOffsetSoTheCampDoesNotTalkInChorus()
        {
            bool differ = false;
            string[] ids = { "mate_a", "mate_b", "mate_c", "mate_d" };
            for (int t = 0; t < 280 && !differ; t++)
            {
                bool first = YardBubble.Up(ids[0], t * 0.05f, 60f);
                for (int i = 1; i < ids.Length; i++)
                    if (YardBubble.Up(ids[i], t * 0.05f, 60f) != first) differ = true;
            }
            Assert.IsTrue(differ);
            Assert.AreEqual(YardBubble.Up("mate_b", 3.2f, 60f), YardBubble.Up("mate_b", 3.2f, 60f));
        }

        [Test]
        public void YardBodiesCarryABarkBubbleThatLeavesWithThem()
        {
            string yard = Read("Assets/Scripts/Colony/CampPopulation.cs");
            StringAssert.Contains("bark = YardBark.Raise(body);", yard);
            StringAssert.Contains("OutpostZero.Shell.Loc.Bark(action, survivor.morale, survivor.fatigue)", yard);
            StringAssert.Contains("Destroy(bark.gameObject);", yard);
            StringAssert.Contains("barks.Clear();", yard);
            string bark = Read("Assets/Scripts/Colony/YardBark.cs");
            StringAssert.Contains("transform.rotation = cam.transform.rotation;", bark);
        }
    }
}
