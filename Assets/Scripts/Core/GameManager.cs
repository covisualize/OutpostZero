using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using OutpostZero.Colony;
using OutpostZero.Expedition;
using OutpostZero.Graphics;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.ExpeditionActive;
        public GameState CurrentState => currentState;

        [Header("Expedition Stats")]
        [SerializeField] private float expeditionTimer = 0f;
        [SerializeField] private int zombiesKilled = 0;
        [SerializeField] private int lifetimeKills = 0;
        [SerializeField] private int scrapLooted = 0;

        private GameState resumeState = GameState.ExpeditionActive;
        private Scene gameplayScene;
        private KillTape killTape;

        public string KillFeed => KillTape.Show(killTape.Text(), null);

        public float ExpeditionTime => expeditionTimer;
        public int ZombiesKilled => zombiesKilled;
        public int LifetimeKills => lifetimeKills;
        public int ScrapLooted => scrapLooted;
        public string LastStreet { get; private set; } = "";

        public event Action<GameState> OnGameStateChanged;
        public event Action<int> OnZombiesKilledChanged;
        public event Action<int> OnScrapLootedChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            gameplayScene = gameObject.scene;
            Instance = this;
            DontDestroyOnLoad(gameObject);
            PlayabilityBootstrap.Apply(gameplayScene);
            GameSystemsInstaller.Install(gameplayScene);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid() || scene.name == "DontDestroyOnLoad") return;
            if (Instance != this) return;
            gameplayScene = scene;
            PlayabilityBootstrap.Apply(scene);
            GameSystemsInstaller.Install(scene);
        }

        private void Update()
        {
            if (currentState == GameState.ExpeditionActive || currentState == GameState.RaidActive)
            {
                expeditionTimer += Time.deltaTime;
            }

            if (ExpeditionInput.PausePressed)
            {
                TogglePause();
            }
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            if (newState == GameState.Victory || newState == GameState.GameOver) RunArchive.NoteCurrent(newState == GameState.Victory);
            bool frozen = newState == GameState.Paused
                || newState == GameState.SuccessionScreen
                || newState == GameState.GameOver
                || newState == GameState.MainMenu
                || newState == GameState.ExpeditionResults
                || newState == GameState.Victory;
            Time.timeScale = frozen ? 0f : 1f;
            OnGameStateChanged?.Invoke(currentState);
        }

        public void TogglePause()
        {
            if (currentState == GameState.Paused)
            {
                SetState(resumeState);
                return;
            }

            if (currentState == GameState.ExpeditionActive || currentState == GameState.RaidActive || currentState == GameState.CampManagement)
            {
                resumeState = currentState;
                SetState(GameState.Paused);
            }
        }

        public void EnterCamp()
        {
            var needs = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<SurvivalNeeds>() : null;
            if (needs != null) SurvivorRoster.Instance?.CopyLeaderNeeds(needs.Hunger, needs.Thirst, needs.Fatigue);
            SetState(GameState.CampManagement);
            WeatherController.Instance?.SetDistrict("");
            SaveSystem.Instance?.Save(false);
        }

        public void BeginExpedition()
        {
            if (WorldMapService.Instance != null && WorldMapService.Instance.Current != null && WorldMapService.Instance.Current.cleared && !WorldMapService.Instance.Endless)
            {
                GameplayFeedback.Toast(GateLine.District(null));
                return;
            }
            if (currentState == GameState.CampManagement) WorldMapService.Instance?.SpendTravel();
            var needs = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<SurvivalNeeds>() : null;
            if (needs != null && SurvivorRoster.Instance != null && SurvivorRoster.Instance.ReadLeaderNeeds(out float hunger, out float thirst))
            {
                float fatigue = needs.Fatigue;
                if (SurvivorRoster.Instance.LeaderFatigue(out float carried)) fatigue = BodyCarry.Carry(carried, needs.Fatigue, true);
                needs.Apply(hunger, thirst, fatigue);
            }
            zombiesKilled = 0;
            scrapLooted = 0;
            expeditionTimer = 0f;
            killTape = default;
            OnZombiesKilledChanged?.Invoke(zombiesKilled);
            OnScrapLootedChanged?.Invoke(scrapLooted);
            ObjectiveTracker.Instance?.ResetProgress();
            WorldMapService.Instance?.ApplyOpening();
            SetState(GameState.ExpeditionActive);
        }

        public void RecordZombieKill(string archetypeId = null)
        {
            zombiesKilled++;
            lifetimeKills++;
            killTape.Note(KillTape.Name(archetypeId));
            PlayerRegistry.Current?.GetComponent<StatusEffectController>()?.ApplyAdrenaline(Affliction.AdrenalineSeconds);
            OnZombiesKilledChanged?.Invoke(zombiesKilled);
            if (!string.IsNullOrEmpty(archetypeId)) CodexDirector.Instance?.Unlock("zombie." + archetypeId);
        }

        public void AddScrap(int amount)
        {
            scrapLooted += amount;
            OnScrapLootedChanged?.Invoke(scrapLooted);
        }

        public void CompleteExpedition()
        {
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            inventory?.DepositScrapToColony();
            LastStreet = WorldMapService.Instance != null && WorldMapService.Instance.Current != null
                ? WorldMapService.Instance.Current.id
                : "";
            WorldMapService.Instance?.ClearCurrent();
            ObjectiveTracker.Instance?.MarkExtracted();
            bool won = WorldMapService.Instance != null && WorldMapService.Instance.CampaignWon && !WorldMapService.Instance.Endless;
            SurvivorRoster.Instance?.RewardReturn();
            BringHomeBite();
            FactionTrade.Instance?.NoteExtracted();
            SetState(won ? GameState.Victory : GameState.ExpeditionResults);
            SaveSystem.Instance?.Save(false);
            AudioManager.Instance?.Sting("extract");
            GameplayFeedback.Toast(won ? GateLine.Broadcast(null) : GateLine.Extracted(null));
        }

        public void TriggerPlayerDeath()
        {
            bool merciful = SettingsService.Instance != null && SettingsService.Instance.Merciful;
            if (merciful && SurvivorRoster.Instance != null && SurvivorRoster.Instance.WoundLeader())
            {
                BringHomeBite();
                BringToCamp(false);
                GameplayFeedback.Toast(GateLine.Drag(null));
                SaveSystem.Instance?.Save(false);
                return;
            }
            Vector3 corpse = PlayerRegistry.Current != null ? PlayerRegistry.Current.transform.position : Vector3.zero;
            var effects = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<StatusEffectController>() : null;
            string cause = effects != null && effects.IsInfected ? "infection" : "killed";
            bool successor = SurvivorRoster.Instance == null || SurvivorRoster.Instance.MarkLeaderDead(corpse, cause);
            AudioManager.Instance?.Sting("death");
            SetState(successor ? GameState.SuccessionScreen : GameState.GameOver);
            SaveSystem.Instance?.Save(false);
        }

        public void AcceptSuccessor(string survivorId)
        {
            var next = SurvivorRoster.Instance != null ? SurvivorRoster.Instance.Promote(survivorId) : null;
            BringToCamp(true);
            GameplayFeedback.Toast(next != null ? StreetAsk.Takes(next.displayName, null) : StreetAsk.Back(null));
            SaveSystem.Instance?.Save(false);
        }

        private static void BringHomeBite()
        {
            var player = PlayerRegistry.Current;
            var effects = player != null ? player.GetComponent<StatusEffectController>() : null;
            int stage = effects != null ? effects.InfectionStage : 0;
            if (stage <= 0 || effects == null) return;
            var roster = SurvivorRoster.Instance;
            if (roster == null || roster.Leader == null) return;
            bool rose = roster.BringFever(stage);
            effects.DropInfection();
            if (rose) GameplayFeedback.Toast(HomeSick.Line(stage, null));
        }

        private void BringToCamp(bool clearInjury)
        {
            int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            SuccessionLedger.NextMorning(day, out int nextDay, out float nextHour);
            WorldClock.Instance?.Set(nextDay, nextHour);
            var player = PlayerRegistry.Current;
            if (player != null)
            {
                var health = player.GetComponent<Combat.HealthSystem>();
                health?.ResetHealth();
                if (clearInjury) player.GetComponent<StatusEffectController>()?.ClearInjury();
                var body = player.GetComponent<CharacterController>();
                if (body != null) body.enabled = false;
                player.transform.position = new Vector3(-12f, 0.1f, -12f);
                if (body != null) body.enabled = true;
            }
            EnterCamp();
        }

        public void BeginNewOutpost()
        {
            ObjectiveTracker.Instance?.ResetProgress();
            int next = SettingsService.Instance != null ? SettingsService.Instance.NextDifficulty : 2;
            WorldMapService.Instance?.ResetMap(next);
            int camp = System.Environment.TickCount;
            if (camp == 0) camp = DistrictGenerator.DefaultSeed + 3;
            if (WorldMapService.Instance != null)
            {
                WorldMapService.Instance.RerollSeed();
                camp = WorldMapService.Instance.WorldSeed ^ camp;
            }
            SurvivorRoster.Instance?.ResetRoster(camp);
            ColonyStorage.Instance?.ResetStores();
            GridBuilder.Instance?.ClearAll();
            TutorialDirector.Instance?.SetFinished(false);
            CodexDirector.Instance?.Restore("");
            PlayerRegistry.Current?.RestoreMods("");
            zombiesKilled = 0;
            lifetimeKills = 0;
            scrapLooted = 0;
            expeditionTimer = 0f;
            killTape = default;
            OnZombiesKilledChanged?.Invoke(zombiesKilled);
            OnScrapLootedChanged?.Invoke(scrapLooted);
            var player = PlayerRegistry.Current;
            if (player != null)
            {
                player.GetComponent<Combat.HealthSystem>()?.ResetHealth();
                player.GetComponent<StatusEffectController>()?.ClearInjury();
                var body = player.GetComponent<CharacterController>();
                if (body != null) body.enabled = false;
                player.transform.position = new Vector3(-12f, 0.1f, -12f);
                if (body != null) body.enabled = true;
            }
            SetState(GameState.CampManagement);
        }

        public void SetLifetimeKills(int kills)
        {
            lifetimeKills = kills < 0 ? 0 : kills;
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            ObjectiveTracker.Instance?.ResetProgress();
            WorldMapService.Instance?.ResetMap();
            SurvivorRoster.Instance?.ResetRoster();
            ColonyStorage.Instance?.ResetStores();
            GridBuilder.Instance?.ClearAll();
            TutorialDirector.Instance?.SetFinished(false);
            CodexDirector.Instance?.Restore("");
            zombiesKilled = 0;
            lifetimeKills = 0;
            scrapLooted = 0;
            expeditionTimer = 0f;
            killTape = default;
            currentState = GameState.ExpeditionActive;
            SceneManager.LoadScene(gameplayScene.buildIndex >= 0 ? gameplayScene.buildIndex : SceneManager.GetActiveScene().buildIndex);
        }
    }
}
