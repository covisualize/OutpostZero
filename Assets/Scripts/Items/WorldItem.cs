using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Items
{
    public class WorldItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string itemId = "scrap";
        [SerializeField] private int count = 1;
        private bool taken;

        public string Prompt => taken ? string.Empty : "Take " + (ItemCatalog.Find(itemId)?.DisplayName ?? itemId);

        public void Configure(string id, int amount)
        {
            itemId = id;
            count = Mathf.Max(1, amount);
        }

        public bool CanInteract(PlayerInventory inventory) => !taken && inventory != null && count > 0;

        public void Interact(PlayerInventory inventory)
        {
            if (!CanInteract(inventory)) return;
            var record = ItemCatalog.Find(itemId);
            if (record == null) return;
            if (!inventory.TryAddItem(record.Id, record.DisplayName, record.Category, count, record.Weight))
            {
                GameplayFeedback.Toast("Pack is too heavy");
                return;
            }
            taken = true;
            GameplayFeedback.Toast("Picked up " + record.DisplayName);
            Destroy(gameObject);
        }
    }
}
