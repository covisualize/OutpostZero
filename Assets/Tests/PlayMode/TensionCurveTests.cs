using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using OutpostZero.AI;
using OutpostZero.Core;
using OutpostZero.Graphics;
using OutpostZero.Sensory;
using OutpostZero.Shell;

namespace OutpostZero.Tests.PlayMode
{
    /// <summary>
    /// PRO-28's pacing check: five minutes of street on Survivor with the leader shielded, two firefights
    /// (at 25 s and 170 s) and quiet in between. The tension curve is sampled every 5 s and written to
    /// Artifacts/Perf/tension-curve.csv, which CI uploads with the perf report.
    /// </summary>
    public class TensionCurveTests
    {
        private const float Scale = 5f;
        private const float Minutes = 5f;
        private const float Step = 5f;

        [TearDown]
        public void Restore()
        {
            DevCheats.SetGod(false);
            Time.timeScale = 1f;
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator PressureEbbsAndFlowsOverFiveMinutes()
        {
            yield return SceneManager.LoadSceneAsync("PrototypeArena", LoadSceneMode.Single);
            float wait = Time.realtimeSinceStartup + 10f;
            while (GameManager.Instance == null && Time.realtimeSinceStartup < wait) yield return null;
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager never came up");

            gm.BeginNewOutpost("tension", true);
            wait = Time.realtimeSinceStartup + 10f;
            while (gm.CurrentState != GameState.CampManagement && gm.CurrentState != GameState.ExpeditionActive && Time.realtimeSinceStartup < wait) yield return null;
            if (gm.CurrentState == GameState.CampManagement) gm.BeginExpedition();
            wait = Time.realtimeSinceStartup + 10f;
            while (gm.CurrentState != GameState.ExpeditionActive && Time.realtimeSinceStartup < wait) yield return null;
            Assert.AreEqual(GameState.ExpeditionActive, gm.CurrentState, "never reached the street");

            var director = HordeDirector.Instance;
            Assert.IsNotNull(director);
            var spawner = director.GetComponent<ZombieSpawner>();
            DevCheats.SetGod(true);
            Time.timeScale = Scale;

            var log = new TensionLog();
            float start = Time.time;
            float nextSample = 0f;
            bool firstFight = false, secondFight = false;
            while (Time.time - start < Minutes * 60f && gm.CurrentState == GameState.ExpeditionActive)
            {
                float t = Time.time - start;
                if (!firstFight && t >= 25f) { firstFight = true; Fight(); }
                if (!secondFight && t >= 170f) { secondFight = true; Fight(); }
                if (t >= nextSample)
                {
                    nextSample += Step;
                    log.Add(t, director.Tension, director.State, spawner != null ? spawner.Alive : 0);
                }
                yield return null;
            }
            Time.timeScale = 1f;

            string folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, PerfGate.ReportFolder);
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "tension-curve.csv");
            File.WriteAllText(path, log.Csv());
            Debug.Log($"[Tension] {log.Samples.Count} samples, {log.Min():0.0} to {log.Max():0.0}, {log.Peaks()} peaks -> {path}");

            Assert.GreaterOrEqual(log.Samples.Count, (int)(Minutes * 60f / Step) - 1, "the street ended early");
            Assert.GreaterOrEqual(log.Peaks(), 1, "the fights never pushed tension to a peak");
            Assert.IsTrue(log.Ebbs(), "tension never eased after a peak");
            Assert.Less(log.Min(), 40f, "the street never went quiet");
        }

        private static void Fight()
        {
            var player = PlayerRegistry.Current;
            if (player == null || NoiseManager.Instance == null) return;
            for (int i = 0; i < 8; i++)
            {
                NoiseManager.Instance.EmitNoise(player.transform.position, 30f, 1f, NoiseType.GunshotLoud, player.gameObject);
            }
        }
    }
}
