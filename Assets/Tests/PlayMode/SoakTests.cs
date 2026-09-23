using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using OutpostZero.AI;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Shell;

namespace OutpostZero.Tests.PlayMode
{
    /// <summary>
    /// Scripted play at 4x time scale: camp, street, fights, extraction, sleep, deaths and succession,
    /// with a save and load every two minutes. Any logged error or exception fails the run, and managed
    /// memory may not grow more than 10 % past the two-minute warm-up. -soakMinutes N (or OUTPOST_SOAK_MINUTES) shortens it; the default is 30.
    /// </summary>
    public class SoakTests
    {
        private const float Scale = 4f;
        private const float StreetSeconds = 45f;
        private const float WarmUp = 120f;
        private const float SaveEvery = 120f;

        private string saves;

        [SetUp]
        public void PointSavesAtScratch()
        {
            saves = Path.Combine(Application.temporaryCachePath, "SoakSaves");
            if (Directory.Exists(saves)) Directory.Delete(saves, true);
            Directory.CreateDirectory(saves);
            SaveSystem.RootOverride = saves;
        }

        [TearDown]
        public void RestoreSaves()
        {
            SaveSystem.RootOverride = null;
            Time.timeScale = 1f;
        }

        /// <summary>-soakMinutes N on the command line, then OUTPOST_SOAK_MINUTES, then 30.</summary>
        public static float Minutes(string[] args, string environment)
        {
            for (int i = 0; args != null && i + 1 < args.Length; i++)
            {
                if (args[i] == "-soakMinutes" && Parse(args[i + 1], out float fromArgs)) return fromArgs;
            }
            return Parse(environment, out float fromEnvironment) ? fromEnvironment : 30f;
        }

        private static bool Parse(string text, out float value)
        {
            return float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value) && value > 0f;
        }

        [UnityTest, Category("Soak"), Timeout(3900000)]
        public IEnumerator ScriptedPlayStaysCleanAndFlat()
        {
            float minutes = Minutes(Environment.GetCommandLineArgs(), Environment.GetEnvironmentVariable("OUTPOST_SOAK_MINUTES"));
            yield return SceneManager.LoadSceneAsync("PrototypeArena", LoadSceneMode.Single);
            float wait = Time.realtimeSinceStartup + 10f;
            while (GameManager.Instance == null && Time.realtimeSinceStartup < wait) yield return null;
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager never came up");

            float start = Time.realtimeSinceStartup;
            float end = start + minutes * 60f;
            float nextSave = start + SaveEvery;
            float streetUntil = 0f;
            float nextSpawn = 0f;
            float nextHit = 0f;
            long baseline = 0;
            long peak = 0;
            int expeditions = 0, extractions = 0, deaths = 0, days = 0, saved = 0;

            while (Time.realtimeSinceStartup < end)
            {
                float now = Time.realtimeSinceStartup;
                switch (gm.CurrentState)
                {
                    case GameState.MainMenu:
                    case GameState.GameOver:
                        gm.BeginNewOutpost();
                        break;
                    case GameState.Paused:
                        gm.TogglePause();
                        break;
                    case GameState.CampManagement:
                        WorldClock.Instance?.SleepUntilMorning();
                        days++;
                        gm.BeginExpedition();
                        if (gm.CurrentState == GameState.ExpeditionActive)
                        {
                            expeditions++;
                            streetUntil = now + StreetSeconds;
                        }
                        break;
                    case GameState.ExpeditionActive:
                    case GameState.RaidActive:
                        Time.timeScale = Scale;
                        if (now >= nextSpawn)
                        {
                            nextSpawn = now + 5f;
                            UnityEngine.Object.FindFirstObjectByType<ZombieSpawner>()?.SpawnZombies(2);
                        }
                        if (now >= nextHit)
                        {
                            nextHit = now + 1f;
                            HitOne();
                        }
                        if (gm.CurrentState == GameState.ExpeditionActive && now >= streetUntil)
                        {
                            gm.CompleteExpedition();
                            extractions++;
                        }
                        break;
                    case GameState.ExpeditionResults:
                    case GameState.Victory:
                        gm.EnterCamp();
                        break;
                    case GameState.SuccessionScreen:
                        deaths++;
                        gm.AcceptSuccessor(NextLeader());
                        break;
                }

                if (now >= nextSave && SaveSystem.Instance != null && gm.CurrentState == GameState.CampManagement)
                {
                    nextSave = now + SaveEvery;
                    Assert.IsTrue(SaveSystem.Instance.Save(false), "autosave failed");
                    Assert.IsTrue(SaveSystem.Instance.Load(), "load after save failed");
                    saved++;
                }

                if (baseline == 0 && now - start >= Mathf.Min(WarmUp, minutes * 60f * 0.25f))
                {
                    baseline = Managed();
                    peak = baseline;
                }
                else if (baseline > 0 && Time.frameCount % 600 == 0)
                {
                    peak = Math.Max(peak, Managed());
                }
                yield return null;
            }

            Time.timeScale = 1f;
            long final = Managed();
            Debug.Log($"[Soak] {minutes} min: {expeditions} expeditions, {extractions} extractions, {deaths} deaths, {days} days, {saved} save/load round trips; managed {baseline / 1048576f:0.0} MB -> {final / 1048576f:0.0} MB (peak {peak / 1048576f:0.0})");
            Assert.Greater(expeditions, 0, "never reached the street");
            Assert.Greater(baseline, 0L);
            Assert.LessOrEqual(final, (long)(baseline * 1.10), "managed memory grew more than 10 %");
        }

        private static long Managed()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return Profiler.GetMonoUsedSizeLong();
        }

        private static void HitOne()
        {
            foreach (var health in UnityEngine.Object.FindObjectsByType<HealthSystem>(FindObjectsSortMode.None))
            {
                if (health == null || health.IsDead || health.gameObject.layer != GameLayers.Enemy) continue;
                health.TakeDamage(40f, health.transform.position + Vector3.up, Vector3.forward, null);
                return;
            }
        }

        private static string NextLeader()
        {
            var roster = SurvivorRoster.Instance;
            if (roster == null) return "";
            foreach (var survivor in roster.Survivors)
            {
                if (survivor.alive && !survivor.leader) return survivor.id;
            }
            return "";
        }
    }
}
