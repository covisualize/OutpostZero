using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using OutpostZero.Colony;
using OutpostZero.Expedition;
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
        [SerializeField] private int scrapLooted = 0;

        private GameState resumeState = GameState.ExpeditionActive;
        private Scene gameplayScene;

        public float ExpeditionTime => expeditionTimer;
        public int ZombiesKilled => zombiesKilled;
        public int ScrapLooted => scrapLooted;

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
            SetState(GameState.CampManagement);
            SaveSystem.Instance?.Save(false);
        }

        public void BeginExpedition()
        {
            zombiesKilled = 0;
            scrapLooted = 0;
            expeditionTimer = 0f;
            OnZombiesKilledChanged?.Invoke(zombiesKilled);
            OnScrapLootedChanged?.Invoke(scrapLooted);
            ObjectiveTracker.Instance?.ResetProgress();
            WorldMapService.Instance?.ApplyOpening();
            SetState(GameState.ExpeditionActive);
        }

        public void RecordZombieKill()
        {
            zombiesKilled++;
            OnZombiesKilledChanged?.Invoke(zombiesKilled);
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
            WorldMapService.Instance?.ClearCurrent();
            ObjectiveTracker.Instance?.MarkExtracted();
            bool won = WorldMapService.Instance != null && WorldMapService.Instance.CampaignWon;
            SetState(won ? GameState.Victory : GameState.ExpeditionResults);
            SaveSystem.Instance?.Save(false);
            GameplayFeedback.Toast(won ? "The ring is clear" : "Extracted");
        }

        public void TriggerPlayerDeath()
        {
            Vector3 corpse = PlayerRegistry.Current != null ? PlayerRegistry.Current.transform.position : Vector3.zero;
            bool successor = SurvivorRoster.Instance == null || SurvivorRoster.Instance.MarkLeaderDead(corpse);
            SetState(successor ? GameState.SuccessionScreen : GameState.GameOver);
        }

        public void AcceptSuccessor(string survivorId)
        {
            var next = SurvivorRoster.Instance != null ? SurvivorRoster.Instance.Promote(survivorId) : null;
            var player = PlayerRegistry.Current;
            if (player != null)
            {
                var health = player.GetComponent<Combat.HealthSystem>();
                health?.ResetHealth();
                player.GetComponent<StatusEffectController>()?.ClearInjury();
                var body = player.GetComponent<CharacterController>();
                if (body != null) body.enabled = false;
                player.transform.position = new Vector3(-12f, 0.1f, -12f);
                if (body != null) body.enabled = true;
            }
            EnterCamp();
            GameplayFeedback.Toast(next != null ? next.displayName + " takes the gate" : "Back inside the gate");
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            SurvivorRoster.Instance?.ResetRoster();
            ObjectiveTracker.Instance?.ResetProgress();
            WorldMapService.Instance?.ResetMap();
            ColonyStorage.Instance?.ResetStores();
            GridBuilder.Instance?.ClearAll();
            TutorialDirector.Instance?.SetFinished(false);
            zombiesKilled = 0;
            scrapLooted = 0;
            expeditionTimer = 0f;
            currentState = GameState.ExpeditionActive;
            SceneManager.LoadScene(gameplayScene.buildIndex >= 0 ? gameplayScene.buildIndex : SceneManager.GetActiveScene().buildIndex);
        }
    }
}
