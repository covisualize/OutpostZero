using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.UI
{
    public static class ShellUi
    {
        public static bool OwnsMenus { get; set; }
    }

    /// <summary>
    /// Owns the pack toggle. The UI Toolkit document draws it.
    /// </summary>
    public class GameShellUI : MonoBehaviour
    {
        private bool inventoryOpen;
        private bool slowed;
        public bool InventoryOpen => inventoryOpen;

        private void Awake()
        {
            ShellUi.OwnsMenus = true;
        }

        public void CloseInventory() => inventoryOpen = false;

        private void Update()
        {
            if (PackView.ConsumeOpen()) inventoryOpen = true;
            if (ExpeditionInput.InventoryPressed)
            {
                inventoryOpen = !inventoryOpen;
                if (inventoryOpen) CodexDirector.Hear("pack");
            }
            Pace();
        }

        private void Pace()
        {
            PackView.Showing = inventoryOpen;
            var game = GameManager.Instance;
            bool street = game != null && (game.CurrentState == GameState.ExpeditionActive || game.CurrentState == GameState.RaidActive);
            if (inventoryOpen && street)
            {
                float pace = PackView.Pace(DifficultyProfile.Active);
                if (Time.timeScale > pace + 0.001f) Time.timeScale = pace;
                slowed = true;
            }
            else if (slowed)
            {
                slowed = false;
                if (street) Time.timeScale = 1f;
            }
        }

        private void OnDisable()
        {
            PackView.Showing = false;
        }
    }
}
