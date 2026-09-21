using UnityEngine;
using OutpostZero.Player;

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
        public bool InventoryOpen => inventoryOpen;

        private void Awake()
        {
            ShellUi.OwnsMenus = true;
        }

        public void CloseInventory() => inventoryOpen = false;

        private void Update()
        {
            if (ExpeditionInput.InventoryPressed)
            {
                inventoryOpen = !inventoryOpen;
            }
        }
    }
}
