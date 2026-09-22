using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using OutpostZero.AI;
using OutpostZero.Core;
using OutpostZero.Graphics;
using OutpostZero.Shell;

namespace OutpostZero.Tests.PlayMode
{
    /// <summary>
    /// A street with 30 live zombies on the Medium tier, sampled for 600 frames after a warm-up. God mode keeps
    /// the leader alive so the crowd keeps chasing and attacking for the whole sample.
    /// <see cref="PerfGate"/> decides pass or fail; the report is logged either way so CI keeps the numbers.
    /// </summary>
    public class PerfTests
    {
        [SetUp]
        public void KeepTheLeaderStanding()
        {
            DevCheats.SetGod(true);
        }

        [TearDown]
        public void Restore()
        {
            DevCheats.SetGod(false);
            Time.timeScale = 1f;
        }

        [UnityTest, Category("Perf"), Timeout(300000)]
        public IEnumerator AStreetWithThirtyZombiesStaysInBudget()
        {
            yield return SceneManager.LoadSceneAsync("PrototypeArena", LoadSceneMode.Single);
            float wait = Time.realtimeSinceStartup + 10f;
            while (GameManager.Instance == null && Time.realtimeSinceStartup < wait) yield return null;
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager never came up");

            if (gm.CurrentState == GameState.MainMenu || gm.CurrentState == GameState.GameOver) gm.BeginNewOutpost();
            if (gm.CurrentState == GameState.CampManagement) gm.BeginExpedition();
            wait = Time.realtimeSinceStartup + 10f;
            while (gm.CurrentState != GameState.ExpeditionActive && Time.realtimeSinceStartup < wait) yield return null;
            Assert.AreEqual(GameState.ExpeditionActive, gm.CurrentState, "never reached the street");

            var spawner = Object.FindFirstObjectByType<ZombieSpawner>();
            Assert.IsNotNull(spawner, "no zombie spawner in the street");
            spawner.ApplyCap(Mathf.Max(spawner.MaxAlive, PerfGate.TestZombies + 2));
            wait = Time.realtimeSinceStartup + 20f;
            while (ZombieAI.AliveCount() < PerfGate.TestZombies && Time.realtimeSinceStartup < wait)
            {
                spawner.SpawnZombies(PerfGate.TestZombies - ZombieAI.AliveCount());
                yield return null;
            }
            Assert.GreaterOrEqual(ZombieAI.AliveCount(), PerfGate.TestZombies, "could not raise the crowd");

            float warm = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < warm) yield return null;

            var frames = new List<float>(PerfGate.SampleFrames);
            var allocs = new List<long>(PerfGate.SampleFrames);
            int fewest = int.MaxValue;
            using (var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
            {
                for (int i = 0; i < PerfGate.SampleFrames; i++)
                {
                    yield return null;
                    frames.Add(Time.unscaledDeltaTime * 1000f);
                    if (gc.Valid) allocs.Add(gc.LastValue);
                    fewest = Mathf.Min(fewest, ZombieAI.AliveCount());
                    if (gm.CurrentState != GameState.ExpeditionActive) break;
                }
            }

            var report = PerfGate.Judge(frames, allocs, QualityProfile.For(1).FrameMs, fewest);
            Debug.Log("[Perf] " + report);
            Assert.AreEqual(GameState.ExpeditionActive, gm.CurrentState, "the street ended during sampling");
            Assert.IsTrue(report.Passed, report.ToString());
        }
    }
}
