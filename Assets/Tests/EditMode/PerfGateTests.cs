using System.Collections.Generic;
using NUnit.Framework;
using OutpostZero.Graphics;

namespace OutpostZero.Tests.EditMode
{
    public class PerfGateTests
    {
        static List<float> Frames(int count, float ms)
        {
            var list = new List<float>(count);
            for (int i = 0; i < count; i++) list.Add(ms);
            return list;
        }

        [Test]
        public void SteadyPlayInsideTheBudgetPasses()
        {
            var report = PerfGate.Judge(Frames(600, 12f), new List<long> { 0, 1024, 2048 }, 16.6f, 30);
            Assert.IsTrue(report.Passed, report.ToString());
            Assert.AreEqual(12f, report.MedianMs);
            Assert.AreEqual(12f, report.P95Ms);
            Assert.AreEqual(1024.0, report.AllocPerFrame);
            StringAssert.Contains("30 zombies", report.ToString());
        }

        [Test]
        public void EachThresholdFailsOnItsOwn()
        {
            Assert.That(PerfGate.Judge(Frames(600, 20f), null, 16.6f, 30).Failures[0], Does.StartWith("median"));

            var tail = Frames(600, 10f);
            for (int i = 0; i < 60; i++) tail[i] = 40f;
            var slow = PerfGate.Judge(tail, null, 16.6f, 30);
            Assert.AreEqual(1, slow.Failures.Count, slow.ToString());
            StringAssert.StartsWith("p95", slow.Failures[0]);

            var hitch = Frames(600, 10f);
            hitch[300] = 400f;
            var spike = PerfGate.Judge(hitch, null, 16.6f, 30);
            Assert.AreEqual(1, spike.Failures.Count, spike.ToString());
            StringAssert.StartsWith("hitch", spike.Failures[0]);

            var heavy = PerfGate.Judge(Frames(600, 10f), new List<long> { 64 * 1024, 64 * 1024 }, 16.6f, 30);
            Assert.AreEqual(1, heavy.Failures.Count, heavy.ToString());
            StringAssert.Contains("KB GC per frame", heavy.Failures[0]);

            var brief = PerfGate.Judge(Frames(10, 10f), null, 16.6f, 30);
            Assert.IsFalse(brief.Passed);
            StringAssert.StartsWith("only 10 frames", brief.Failures[0]);
            Assert.IsFalse(PerfGate.Judge(null, null, 16.6f, 0).Passed);
        }

        [Test]
        public void PercentilesUseTheNearestRank()
        {
            var sorted = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            Assert.AreEqual(5f, PerfGate.Percentile(sorted, 0.5f));
            Assert.AreEqual(10f, PerfGate.Percentile(sorted, 0.95f));
            Assert.AreEqual(1f, PerfGate.Percentile(sorted, 0f));
            Assert.AreEqual(0f, PerfGate.Percentile(new float[0], 0.5f));
        }

        [Test]
        public void TheBudgetPollsNoSceneSearchesPerFrame()
        {
            string source = System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "Scripts", "Graphics", "PerfBudget.cs"));
            StringAssert.DoesNotContain("FindObjectsByType", source);
            StringAssert.Contains("ZombieAI.AliveCount()", source);
            Assert.AreEqual(0, OutpostZero.AI.ZombieAI.AliveCount());
        }
    }
}
