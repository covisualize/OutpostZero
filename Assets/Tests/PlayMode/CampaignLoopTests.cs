using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using OutpostZero.Colony;
using OutpostZero.Core;
using OutpostZero.Expedition;
using OutpostZero.Shell;
using OutpostZero.UI;

namespace OutpostZero.Tests.PlayMode
{
    /// <summary>
    /// The M5 epic's loop in the real scene: New Game, three camp and expedition cycles, Save & Quit to the menu,
    /// then Continue. The state read back after Continue must match the state that was saved, field for field.
    /// </summary>
    public class CampaignLoopTests
    {
        private const int Cycles = 3;
        private string saves;

        [SetUp]
        public void PointSavesAtScratch()
        {
            saves = Path.Combine(Application.temporaryCachePath, "CampaignSaves");
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

        private static IEnumerator Until(System.Func<bool> done, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!done() && Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator ThreeCyclesThenSaveQuitAndContinueRestoresTheCamp()
        {
            yield return SceneManager.LoadSceneAsync("PrototypeArena", LoadSceneMode.Single);
            yield return Until(() => GameManager.Instance != null && SaveSystem.Instance != null, 10f);
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager never came up");
            Assert.IsNotNull(SaveSystem.Instance);

            gm.BeginNewOutpost("campaign", true);
            yield return Until(() => gm.CurrentState == GameState.CampManagement || gm.CurrentState == GameState.ExpeditionActive, 10f);
            int firstDay = WorldClock.Instance != null ? WorldClock.Instance.Day : 0;

            for (int cycle = 0; cycle < Cycles; cycle++)
            {
                if (gm.CurrentState != GameState.ExpeditionActive)
                {
                    yield return Until(() => gm.CurrentState == GameState.CampManagement, 10f);
                    Assert.AreEqual(GameState.CampManagement, gm.CurrentState, "cycle " + cycle + " starts in camp");
                    gm.BeginExpedition();
                }
                yield return Until(() => gm.CurrentState == GameState.ExpeditionActive, 10f);
                Assert.AreEqual(GameState.ExpeditionActive, gm.CurrentState, "cycle " + cycle + " reaches the street");
                yield return new WaitForSeconds(1f);
                gm.CompleteExpedition();
                yield return Until(() => gm.CurrentState == GameState.ExpeditionResults || gm.CurrentState == GameState.Victory, 10f);
                gm.EnterCamp();
                yield return Until(() => gm.CurrentState == GameState.CampManagement, 10f);
                Assert.AreEqual(GameState.CampManagement, gm.CurrentState, "cycle " + cycle + " comes home");
                WorldClock.Instance?.SleepUntilMorning();
                yield return null;
            }
            Assert.AreEqual(ExpeditionEnd.Extracted, gm.LastOutcome.end, "the last trip ended in an extraction");
            if (WorldClock.Instance != null) Assert.GreaterOrEqual(WorldClock.Instance.Day, firstDay + Cycles, "three nights passed");

            Assert.IsTrue(SaveSystem.CanSaveManually, "camp allows a manual save");
            Assert.IsTrue(SaveSystem.Instance.Save(), "Save & Quit writes the slot");
            var saved = SaveSystem.Instance.Capture();

            Assert.IsNotNull(SceneFlow.Instance);
            SceneFlow.Instance.Travel(FlowStep.MainMenu);
            yield return Until(() => gm.CurrentState == GameState.MainMenu && !SceneFlow.Instance.Busy, 20f);
            Assert.AreEqual(GameState.MainMenu, gm.CurrentState, "quit lands on the menu");

            Assert.IsTrue(SaveSystem.Instance.HasSave());
            Assert.IsTrue(SaveSystem.Instance.Load(), "Continue opens the newest save");
            var restored = SaveSystem.Instance.Capture();
            var diffs = SaveDiff.Compare(saved, restored);
            diffs.RemoveAll(path => path.Contains("savedAt") || path.Contains("thumbnail"));
            Assert.IsEmpty(diffs, "Continue restores: " + string.Join(", ", diffs));
            yield return Until(() => gm.CurrentState == GameState.CampManagement, 10f);
            Assert.AreEqual(GameState.CampManagement, gm.CurrentState, "Continue returns to camp");
        }
    }
}
