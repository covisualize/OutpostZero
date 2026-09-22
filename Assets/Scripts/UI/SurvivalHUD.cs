using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Player;
using OutpostZero.Combat;
using OutpostZero.Graphics;
using OutpostZero.Sensory;

namespace OutpostZero.UI
{
    /// <summary>
    /// Tracks the leader's noise and toast text. Drawing lives in <see cref="OutpostInterface"/>.
    /// </summary>
    public class SurvivalHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private HealthSystem playerHealth;
        [SerializeField] private PlayerInventory inventory;

        private float currentNoiseLevel;
        private float noiseDecaySpeed = 3.5f;
        private string toastMessage;
        private float toastUntil;

        public float NoiseLevel => currentNoiseLevel;
        public string Toast => Time.unscaledTime <= toastUntil ? toastMessage : null;
        public HealthSystem Health => playerHealth;
        public PlayerInventory Inventory => inventory;

        private void Start()
        {
            if (player == null)
            {
                player = PlayerRegistry.Current != null
                    ? PlayerRegistry.Current
                    : FindFirstObjectByType<PlayerController>();
            }
            Bind(player);
            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.OnNoiseEmitted += HandleNoiseEmitted;
            }
            GameplayFeedback.OnToast += ShowToast;
        }

        public void Bind(PlayerController boundPlayer)
        {
            player = boundPlayer;
            if (player == null) return;
            playerHealth = player.GetComponent<HealthSystem>();
            inventory = player.GetComponent<PlayerInventory>();
        }

        private void ShowToast(string message)
        {
            toastMessage = message;
            toastUntil = Time.unscaledTime + 2.4f;
        }

        private void OnDestroy()
        {
            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.OnNoiseEmitted -= HandleNoiseEmitted;
            }
            GameplayFeedback.OnToast -= ShowToast;
        }

        private void Update()
        {
            if (currentNoiseLevel > 0f)
            {
                currentNoiseLevel = Mathf.Max(0f, currentNoiseLevel - noiseDecaySpeed * Time.unscaledDeltaTime);
            }
        }

        private void HandleNoiseEmitted(Vector3 origin, float radius, NoiseType type)
        {
            if (player != null && Vector3.Distance(origin, player.transform.position) < 1.0f)
            {
                currentNoiseLevel = Mathf.Clamp(radius / 30f, 0f, 1f);
            }
            if (SettingsService.Instance != null && !SettingsService.Instance.Subtitles) return;
            Vector3 from = player != null ? origin - player.transform.position : origin;
            string line = Presentation.Caption(type, from.x, from.z, SettingsService.Instance != null ? SettingsService.Instance.Language : "en");
            if (!string.IsNullOrEmpty(line)) ShowToast(line);
        }
    }
}
