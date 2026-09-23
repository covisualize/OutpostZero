using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Shell;

namespace OutpostZero.Player
{
    /// <summary>The leader's kit as a save part, so a camp save and Continue hand back the same pack and guns.</summary>
    [RequireComponent(typeof(PlayerInventory))]
    public class LeaderKitSave : MonoBehaviour, ISaveable
    {
        public string SaveId => LeaderKit.SaveId;

        private void OnEnable() => SaveRegistry.Register(this);

        private void OnDisable() => SaveRegistry.Unregister(this);

        public string CaptureState()
        {
            var inventory = GetComponent<PlayerInventory>();
            var controller = GetComponent<PlayerController>();
            var kit = new LeaderKit.Kit
            {
                Gear = inventory != null ? inventory.PackGear() : "",
                Belt = inventory != null ? inventory.BeltSlots() : Items.ItemBelt.Fresh(),
                Arms = controller != null ? controller.PackArms() : new List<LeaderKit.Arm>(),
                Active = controller != null ? controller.ActiveSlot : 0
            };
            return LeaderKit.Pack(kit);
        }

        /// <summary>An older save has no kit part; the leader keeps what they hold.</summary>
        public void RestoreState(string state)
        {
            if (!LeaderKit.TryUnpack(state, out var kit)) return;
            var inventory = GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.ReplaceGear(kit.Gear);
                inventory.SetBelt(kit.Belt);
            }
            GetComponent<PlayerController>()?.RestoreArms(kit.Arms, kit.Active);
        }
    }
}
