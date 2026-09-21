using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Player;
using OutpostZero.Combat;
using OutpostZero.Sensory;

namespace OutpostZero.UI
{
    public class SurvivalHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController player;
        [SerializeField] private HealthSystem playerHealth;
        [SerializeField] private PlayerInventory inventory;

        [Header("Acoustic Monitoring")]
        private float currentNoiseLevel = 0f;
        private float noiseDecaySpeed = 3.5f;
        private string toastMessage;
        private float toastUntil;

        private void Start()
        {
            if (player == null)
            {
                player = PlayerRegistry.Current != null
                    ? PlayerRegistry.Current
                    : FindFirstObjectByType<PlayerController>();
            }
            if (player != null)
            {
                playerHealth = player.GetComponent<HealthSystem>();
                inventory = player.GetComponent<PlayerInventory>();
            }

            if (NoiseManager.Instance != null)
            {
                NoiseManager.Instance.OnNoiseEmitted += HandleNoiseEmitted;
            }

            GameplayFeedback.OnToast += ShowToast;
        }

        public void Bind(PlayerController boundPlayer)
        {
            player = boundPlayer;
            if (player != null)
            {
                playerHealth = player.GetComponent<HealthSystem>();
                inventory = player.GetComponent<PlayerInventory>();
            }
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
                currentNoiseLevel = Mathf.Max(0f, currentNoiseLevel - noiseDecaySpeed * Time.deltaTime);
            }
        }

        private void HandleNoiseEmitted(Vector3 origin, float radius, NoiseType type)
        {
            if (player != null && Vector3.Distance(origin, player.transform.position) < 1.0f)
            {
                currentNoiseLevel = Mathf.Clamp(radius / 30f, 0f, 1f);
            }
        }

        private void OnGUI()
        {
            // Immediate Mode GUI for zero-setup instant testing and playability
            if (!UitkHud.Live) DrawTopHUD();
            DrawBottomStatus();
            DrawNoiseIndicator();
            DrawToast();

            if (!ShellUi.OwnsMenus && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.SuccessionScreen)
            {
                DrawSuccessionScreen();
            }
            else if (!ShellUi.OwnsMenus && GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Paused)
            {
                DrawPauseMenu();
            }
        }

        private void DrawTopHUD()
        {
            GUILayout.BeginArea(new Rect(20, 20, 320, 110), GUI.skin.box);
            GUILayout.Label("<b>OUTPOST ZERO — EXPEDITION</b>", GetHeaderStyle());

            if (playerHealth != null)
            {
                float hpPercent = playerHealth.CurrentHealth / Mathf.Max(1f, playerHealth.MaxHealth);
                GUI.color = Color.Lerp(Color.red, Color.green, hpPercent);
                GUILayout.Label($"Health: {Mathf.CeilToInt(playerHealth.CurrentHealth)} / {playerHealth.MaxHealth}");
                GUI.color = Color.white;
            }

            if (player != null)
            {
                float stamPercent = player.CurrentStamina / Mathf.Max(1f, player.MaxStamina);
                GUI.color = Color.cyan;
                GUILayout.Label($"Stamina: {Mathf.CeilToInt(player.CurrentStamina)} / {player.MaxStamina}");
                GUI.color = Color.white;
            }

            if (inventory != null)
            {
                GUILayout.Label($"Scrap Looted: {inventory.ScrapCount}  |  Medkits: {inventory.MedicalKits} [Q]");
            }

            GUILayout.EndArea();
        }

        private void DrawBottomStatus()
        {
            float boxWidth = 280;
            float boxHeight = 100;
            Rect rect = new Rect(Screen.width - boxWidth - 20, Screen.height - boxHeight - 20, boxWidth, boxHeight);

            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("<b>EQUIPPED WEAPON [1, 2, 3]</b>", GetHeaderStyle());

            if (player != null && player.ActiveWeapon != null)
            {
                var weapon = player.ActiveWeapon;
                GUILayout.Label($"Weapon: <b>{weapon.WeaponName}</b>");

                if (weapon is FirearmWeapon firearm)
                {
                    string reloadStatus = firearm.IsReloading ? " <color=yellow>[RELOADING...]</color>" : "";
                    GUILayout.Label($"Ammo: <b>{firearm.CurrentAmmo} / {firearm.ReserveAmmo}</b>{reloadStatus}");
                    GUILayout.Label("Press [R] to reload magazine");
                }
                else
                {
                    GUILayout.Label("Melee Weapon (Silent, Conserves Ammo)");
                }
            }
            else
            {
                GUILayout.Label("No Weapon Equipped");
            }

            GUILayout.EndArea();
        }

        private void DrawNoiseIndicator()
        {
            float barWidth = 200;
            float barHeight = 18;
            float x = (Screen.width - barWidth) * 0.5f;
            float y = Screen.height - 45;

            GUI.Box(new Rect(x - 5, y - 22, barWidth + 10, barHeight + 26), "");
            GUI.Label(new Rect(x, y - 20, barWidth, 18), "<b>ACOUSTIC NOISE LEVEL</b>", GetCenteredStyle());

            // Background bar
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.DrawTexture(new Rect(x, y, barWidth, barHeight), Texture2D.whiteTexture);

            // Fill bar
            Color noiseColor = Color.Lerp(Color.green, Color.red, currentNoiseLevel);
            GUI.color = noiseColor;
            GUI.DrawTexture(new Rect(x, y, barWidth * currentNoiseLevel, barHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawToast()
        {
            if (string.IsNullOrEmpty(toastMessage) || Time.unscaledTime > toastUntil) return;

            float width = 420f;
            Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.22f, width, 36f);
            GUI.Box(rect, toastMessage);
        }

        private void DrawSuccessionScreen()
        {
            GUI.color = new Color(0.1f, 0f, 0f, 0.92f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float panelW = 450;
            float panelH = 260;
            Rect centerRect = new Rect((Screen.width - panelW) * 0.5f, (Screen.height - panelH) * 0.5f, panelW, panelH);

            GUILayout.BeginArea(centerRect, GUI.skin.box);
            GUILayout.Space(10);
            GUILayout.Label("<size=22><color=red><b>LEADER KILLED IN ACTION</b></color></size>", GetCenteredStyle());
            GUILayout.Space(10);
            GUILayout.Label("Your expedition leader has fallen to the infection.\nUnder <b>Community Succession Permadeath</b>, the mantle passes to the next survivor at Sanctuary.", GetCenteredStyle());
            GUILayout.Space(15);

            if (GUILayout.Button("Take Control of Next Community Survivor", GUILayout.Height(45)))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RestartCurrentScene();
                }
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Return to Sanctuary Hub", GUILayout.Height(35)))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RestartCurrentScene();
                }
            }

            GUILayout.EndArea();
        }

        private void DrawPauseMenu()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float panelW = 320;
            float panelH = 220;
            Rect centerRect = new Rect((Screen.width - panelW) * 0.5f, (Screen.height - panelH) * 0.5f, panelW, panelH);

            GUILayout.BeginArea(centerRect, GUI.skin.box);
            GUILayout.Space(10);
            GUILayout.Label("<size=20><b>PAUSED</b></size>", GetCenteredStyle());
            GUILayout.Space(15);

            if (GUILayout.Button("Resume Expedition", GUILayout.Height(38)))
            {
                GameManager.Instance.TogglePause();
            }

            GUILayout.Space(8);
            if (GUILayout.Button("Restart Expedition Run", GUILayout.Height(38)))
            {
                GameManager.Instance.RestartCurrentScene();
            }

            GUILayout.EndArea();
        }

        private GUIStyle GetHeaderStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            style.fontSize = 12;
            return style;
        }

        private GUIStyle GetCenteredStyle()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.richText = true;
            style.alignment = TextAnchor.MiddleCenter;
            return style;
        }
    }
}
