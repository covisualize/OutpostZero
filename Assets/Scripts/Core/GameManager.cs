using System;
using UnityEngine;

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

            var gameplayScene = gameObject.scene;
            Instance = this;
            DontDestroyOnLoad(gameObject);
            PlayabilityBootstrap.Apply(gameplayScene);
        }

        private void Update()
        {
            if (currentState == GameState.ExpeditionActive)
            {
                expeditionTimer += Time.deltaTime;
            }

            // Global pause toggle with Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        public void SetState(GameState newState)
        {
            if (currentState == newState) return;

            currentState = newState;
            Time.timeScale = (currentState == GameState.Paused) ? 0f : 1f;
            OnGameStateChanged?.Invoke(currentState);

            Debug.Log($"[GameManager] Game State changed to: {newState}");
        }

        public void TogglePause()
        {
            if (currentState == GameState.ExpeditionActive)
            {
                SetState(GameState.Paused);
            }
            else if (currentState == GameState.Paused)
            {
                SetState(GameState.ExpeditionActive);
            }
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

        public void TriggerPlayerDeath()
        {
            Debug.LogWarning("[GameManager] Active Leader Died!");
            SetState(GameState.SuccessionScreen);
        }

        public void RestartCurrentScene()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }
}
