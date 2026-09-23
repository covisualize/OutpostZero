using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Graphics;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Tests.PlayMode
{
    /// <summary>
    /// A sixty-second scripted firefight on the Medium tier: the leader shoots the nearest zombie with each
    /// weapon in turn while the crowd is held at 30, so muzzle flashes, impacts, decals, gore, and ragdolls all
    /// land in the sample. God mode keeps the leader standing. <see cref="PerfGate"/> judges p95 frame time,
    /// GC per frame, and draw calls; the report goes to the log and to Artifacts/Perf/perf-report.json, which CI uploads.
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

        public static string ReportPath()
        {
            string folder = System.Environment.GetEnvironmentVariable("OUTPOST_PERF_DIR");
            if (string.IsNullOrEmpty(folder)) folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, PerfGate.ReportFolder);
            return Path.Combine(folder, PerfGate.ReportFile);
        }

        [UnityTest, Category("Perf"), Timeout(300000)]
        public IEnumerator AScriptedFirefightOnMediumStaysInBudget()
        {
            yield return SceneManager.LoadSceneAsync("PrototypeArena", LoadSceneMode.Single);
            float wait = Time.realtimeSinceStartup + 10f;
            while (GameManager.Instance == null && Time.realtimeSinceStartup < wait) yield return null;
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager never came up");

            var settings = SettingsService.Instance;
            for (int i = 0; settings != null && settings.Quality != 1 && i < QualityProfile.Count; i++) settings.CycleQuality();
            Assert.IsTrue(settings == null || settings.Quality == 1, "could not select the Medium tier");

            if (gm.CurrentState == GameState.MainMenu || gm.CurrentState == GameState.GameOver) gm.BeginNewOutpost();
            if (gm.CurrentState == GameState.CampManagement) gm.BeginExpedition();
            wait = Time.realtimeSinceStartup + 10f;
            while (gm.CurrentState != GameState.ExpeditionActive && Time.realtimeSinceStartup < wait) yield return null;
            Assert.AreEqual(GameState.ExpeditionActive, gm.CurrentState, "never reached the street");

            var player = PlayerRegistry.Current;
            Assert.IsNotNull(player, "no leader in the street");
            Assert.Greater(player.WeaponCount, 0, "the leader carries nothing to shoot");

            var spawner = Object.FindFirstObjectByType<ZombieSpawner>();
            Assert.IsNotNull(spawner, "no zombie spawner in the street");
            spawner.ApplyCap(Mathf.Max(spawner.MaxAlive, PerfGate.TestZombies + 2));
            int spawned = 0;
            wait = Time.realtimeSinceStartup + 20f;
            while (ZombieAI.AliveCount() < PerfGate.TestZombies && Time.realtimeSinceStartup < wait)
            {
                int before = ZombieAI.AliveCount();
                spawner.SpawnZombies(FirefightPlan.Refill(before, PerfGate.TestZombies));
                yield return null;
                spawned += Mathf.Max(0, ZombieAI.AliveCount() - before);
            }
            Assert.GreaterOrEqual(ZombieAI.AliveCount(), PerfGate.TestZombies, "could not raise the crowd");

            float warm = Time.realtimeSinceStartup + FirefightPlan.WarmSeconds;
            while (Time.realtimeSinceStartup < warm) yield return null;

            int capacity = Mathf.CeilToInt(FirefightPlan.Seconds * 90f);
            var frames = new List<float>(capacity);
            var allocs = new List<long>(capacity);
            var draws = new List<int>(capacity);
            var passes = new List<int>(capacity);
            int fewest = int.MaxValue;
            int shots = 0;
            int startAlive = ZombieAI.AliveCount();
            spawned = 0;
            float start = Time.realtimeSinceStartup;
            float nextRefill = 0f;
            int slot = -1;

            using (var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame"))
            using (var drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
            using (var setPass = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count"))
            {
                while (!FirefightPlan.Done(Time.realtimeSinceStartup - start))
                {
                    float elapsed = Time.realtimeSinceStartup - start;
                    int wanted = FirefightPlan.SlotAt(elapsed, player.WeaponCount);
                    if (wanted != slot && player.WeaponAt(wanted) != null)
                    {
                        slot = wanted;
                        player.SelectWeapon(slot);
                    }
                    shots += Shoot(player);

                    if (elapsed >= nextRefill)
                    {
                        nextRefill = elapsed + FirefightPlan.RefillEvery;
                        int before = ZombieAI.AliveCount();
                        int missing = FirefightPlan.Refill(before, PerfGate.TestZombies);
                        if (missing > 0)
                        {
                            spawner.SpawnZombies(missing);
                            spawned += Mathf.Max(0, ZombieAI.AliveCount() - before);
                        }
                    }

                    yield return null;
                    frames.Add(Time.unscaledDeltaTime * 1000f);
                    if (gc.Valid) allocs.Add(gc.LastValue);
                    if (drawCalls.Valid) draws.Add((int)drawCalls.LastValue);
                    if (setPass.Valid) passes.Add((int)setPass.LastValue);
                    fewest = Mathf.Min(fewest, ZombieAI.AliveCount());
                    if (gm.CurrentState != GameState.ExpeditionActive) break;
                }
            }

            var report = PerfGate.Judge(frames, allocs, draws, passes, QualityProfile.For(1).FrameMs, fewest);
            report.Scenario = "firefight";
            report.Tier = QualityProfile.For(settings != null ? settings.Quality : 1).Name;
            report.Seconds = Time.realtimeSinceStartup - start;
            report.Shots = shots;
            report.Kills = Mathf.Max(0, startAlive + spawned - ZombieAI.AliveCount());
            string path = ReportPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, report.ToJson());
            Debug.Log("[Perf] " + report + " -> " + path);

            Assert.AreEqual(GameState.ExpeditionActive, gm.CurrentState, "the street ended during sampling");
            Assert.Greater(shots, 0, "the scripted leader never fired");
            Assert.IsTrue(report.Passed, report.ToString());
        }

        private static int Shoot(PlayerController player)
        {
            var weapon = player.ActiveWeapon;
            if (weapon == null) return 0;
            var target = ZombieAI.NearestAlive(player.transform.position);
            if (target == null) return 0;
            Vector3 aim = target.Position - player.transform.position;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.01f) return 0;
            aim.Normalize();
            player.transform.rotation = Quaternion.LookRotation(aim);

            if (weapon is FirearmWeapon gun)
            {
                int topUp = FirefightPlan.TopUp(gun.ReserveAmmo);
                if (topUp > 0) gun.AddReserveAmmo(topUp);
                if (gun.CurrentAmmo <= 0 && !gun.IsReloading) gun.TryStartReload();
            }
            return weapon.TryAttack(aim) ? 1 : 0;
        }
    }
}
