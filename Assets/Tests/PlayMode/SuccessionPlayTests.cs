using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Expedition;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Tests.PlayMode
{
    /// <summary>
    /// The PRO-60 loop in the real scene: the leader falls on the street, a survivor takes over, the camp
    /// wakes with one fewer, and the body with the old leader's pack is waiting on the next trip there.
    /// </summary>
    public class SuccessionPlayTests
    {
        private string saves;
        private bool wasMerciful;

        [SetUp]
        public void PointSavesAtScratch()
        {
            saves = Path.Combine(Application.temporaryCachePath, "SuccessionSaves");
            if (Directory.Exists(saves)) Directory.Delete(saves, true);
            Directory.CreateDirectory(saves);
            SaveSystem.RootOverride = saves;
        }

        [TearDown]
        public void RestoreSaves()
        {
            if (SettingsService.Instance != null) SettingsService.Instance.SetMerciful(wasMerciful);
            SaveSystem.RootOverride = null;
            Time.timeScale = 1f;
        }

        private static IEnumerator Until(System.Func<bool> done, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!done() && Time.realtimeSinceStartup < end) yield return null;
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator ALostLeaderHandsOverAndLeavesTheirPackOnTheStreet()
        {
            yield return SceneManager.LoadSceneAsync("PrototypeArena", LoadSceneMode.Single);
            yield return Until(() => GameManager.Instance != null, 10f);
            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "GameManager never came up");
            Assert.IsNotNull(SettingsService.Instance);
            wasMerciful = SettingsService.Instance.Merciful;
            SettingsService.Instance.SetMerciful(false);

            gm.BeginNewOutpost("succession", true);
            yield return Until(() => gm.CurrentState == GameState.CampManagement || gm.CurrentState == GameState.ExpeditionActive, 10f);
            if (gm.CurrentState == GameState.CampManagement) gm.BeginExpedition();
            yield return Until(() => gm.CurrentState == GameState.ExpeditionActive, 10f);
            Assert.AreEqual(GameState.ExpeditionActive, gm.CurrentState, "never reached the street");

            var roster = SurvivorRoster.Instance;
            Assert.IsNotNull(roster);
            int living = roster.LivingCount();
            Assert.Greater(living, 1, "a fresh outpost has someone to take over");
            string fallen = roster.Leader.id;
            string heir = roster.SuggestedHeir().id;
            Assert.AreNotEqual(fallen, heir);
            string district = WorldMapService.Instance.Current.id;

            var player = PlayerRegistry.Current;
            Assert.IsNotNull(player);
            var pack = player.GetComponent<PlayerInventory>();
            var record = ItemCatalog.Find("bandage");
            Assert.IsTrue(pack.TryAddItem(record.Id, record.DisplayName, record.Category, 2, record.Weight));
            var health = player.GetComponent<HealthSystem>();
            health.TakeDamage(10000f, player.transform.position + Vector3.up, Vector3.forward, null);
            yield return Until(() => gm.CurrentState == GameState.SuccessionScreen, 10f);
            Assert.AreEqual(GameState.SuccessionScreen, gm.CurrentState, "the leader's death opens succession");
            Assert.AreEqual(living - 1, roster.LivingCount());
            Assert.AreEqual(0, pack.Items.Count, "the pack stays with the body");
            var body = Object.FindFirstObjectByType<FallenGear>();
            Assert.IsNotNull(body, "the body lies where the leader fell");

            gm.AcceptSuccessor(heir);
            yield return Until(() => gm.CurrentState == GameState.CampManagement, 10f);
            Assert.AreEqual(GameState.CampManagement, gm.CurrentState);
            Assert.AreEqual(heir, roster.Leader.id, "the chosen survivor leads");
            Assert.AreEqual(living - 1, roster.LivingCount(), "the camp carries on one short");
            Assert.AreEqual(1, roster.Memorials.Count);
            Assert.AreEqual(ExpeditionEnd.Succession, gm.LastOutcome.end);
            Assert.AreEqual(fallen, gm.LastOutcome.leaderId);

            Assert.IsTrue(WorldMapService.Instance.Select(district) || WorldMapService.Instance.Current.id == district);
            gm.BeginExpedition();
            yield return Until(() => gm.CurrentState == GameState.ExpeditionActive, 10f);
            Assert.AreEqual(GameState.ExpeditionActive, gm.CurrentState, "back out to the same street");
            yield return null;
            body = Object.FindFirstObjectByType<FallenGear>();
            Assert.IsNotNull(body, "the old leader's body is still there on re-entry");

            var carrier = PlayerRegistry.Current.GetComponent<PlayerInventory>();
            Assert.IsTrue(body.CanInteract(carrier));
            body.Interact(carrier);
            yield return null;
            bool bandages = false;
            foreach (var item in carrier.Items)
                if (item != null && item.ItemId == "bandage" && item.Quantity >= 2) bandages = true;
            Assert.IsTrue(bandages, "the fallen leader's gear comes back");
            Assert.IsNull(Object.FindFirstObjectByType<FallenGear>(), "a recovered body is gone");
        }
    }
}
