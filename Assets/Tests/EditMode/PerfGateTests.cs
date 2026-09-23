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

        static List<int> Counts(int count, int value)
        {
            var list = new List<int>(count);
            for (int i = 0; i < count; i++) list.Add(value);
            return list;
        }

        [Test]
        public void TheBusiestFramesMustStayUnderThreeHundredDrawCalls()
        {
            var calm = PerfGate.Judge(Frames(600, 12f), null, Counts(600, 240), Counts(600, 60), 16.6f, 30);
            Assert.IsTrue(calm.Passed, calm.ToString());
            Assert.AreEqual(240, calm.DrawCallsP95);
            Assert.AreEqual(60, calm.SetPassP95);

            var busy = Counts(600, 240);
            for (int i = 0; i < 20; i++) busy[i] = 900;
            var spike = PerfGate.Judge(Frames(600, 12f), null, busy, null, 16.6f, 30);
            Assert.IsTrue(spike.Passed, "a few spawn frames above the budget are the tail, not the p95");
            Assert.AreEqual(900, spike.DrawCallsWorst);

            var heavy = PerfGate.Judge(Frames(600, 12f), null, Counts(600, PerfGate.DrawCallBudget + 1), null, 16.6f, 30);
            Assert.AreEqual(1, heavy.Failures.Count, heavy.ToString());
            StringAssert.StartsWith("draw calls p95 301", heavy.Failures[0]);
            Assert.AreEqual(300, PerfGate.DrawCallBudget);
        }

        [Test]
        public void TheReportIsJsonForTheCiArtifact()
        {
            var report = PerfGate.Judge(Frames(600, 20f), new List<long> { 2048, 4096 }, Counts(600, 180), Counts(600, 40), 16.6f, 30);
            report.Scenario = "firefight";
            report.Tier = "Medium";
            report.Seconds = 60f;
            report.Shots = 212;
            report.Kills = 41;
            string json = report.ToJson();
            StringAssert.Contains("\"scenario\": \"firefight\"", json);
            StringAssert.Contains("\"tier\": \"Medium\"", json);
            StringAssert.Contains("\"passed\": false", json);
            StringAssert.Contains("\"p95Ms\": 20.00", json);
            StringAssert.Contains("\"gcBytesPerFrame\": 3072", json);
            StringAssert.Contains("\"gcBytesWorstFrame\": 4096", json);
            StringAssert.Contains("\"drawCallsP95\": 180", json);
            StringAssert.Contains("\"shots\": 212", json);
            StringAssert.Contains("\"failures\": [\"median 20.0 ms over 16.6\"]", json);
            var previous = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
                StringAssert.Contains("\"budgetMs\": 16.60", report.ToJson(), "decimal dots on any locale");
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = previous;
            }
            Assert.AreEqual("Artifacts/Perf", PerfGate.ReportFolder);
            StringAssert.Contains("path: Artifacts/Perf", System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), ".github", "workflows", "unity-ci.yml")));
        }

        [Test]
        public void TheFirefightRunsSixtySecondsAndCyclesTheLoadout()
        {
            Assert.AreEqual(60f, FirefightPlan.Seconds);
            Assert.IsFalse(FirefightPlan.Done(59.9f));
            Assert.IsTrue(FirefightPlan.Done(60f));
            Assert.AreEqual(0, FirefightPlan.SlotAt(0f, 4));
            Assert.AreEqual(1, FirefightPlan.SlotAt(FirefightPlan.SwitchEvery + 0.1f, 4));
            Assert.AreEqual(0, FirefightPlan.SlotAt(FirefightPlan.SwitchEvery * 4 + 0.1f, 4));
            Assert.AreEqual(0, FirefightPlan.SlotAt(30f, 1));
            var seen = new HashSet<int>();
            for (float t = 0f; t < FirefightPlan.Seconds; t += 1f) seen.Add(FirefightPlan.SlotAt(t, 4));
            Assert.AreEqual(4, seen.Count, "every weapon fires inside the minute");
            Assert.AreEqual(5, FirefightPlan.Refill(25, 30));
            Assert.AreEqual(0, FirefightPlan.Refill(31, 30));
            Assert.AreEqual(FirefightPlan.ReserveTopUp, FirefightPlan.TopUp(0));
            Assert.AreEqual(0, FirefightPlan.TopUp(FirefightPlan.ReserveFloor));

            string test = System.IO.File.ReadAllText(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "Tests", "PlayMode", "PerfTests.cs"));
            StringAssert.Contains("\"Draw Calls Count\"", test);
            StringAssert.Contains("\"GC Allocated In Frame\"", test);
            StringAssert.Contains("File.WriteAllText(path, report.ToJson())", test);
            StringAssert.Contains("FirefightPlan.Done(", test);
            StringAssert.Contains("weapon.TryAttack(aim)", test);
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
