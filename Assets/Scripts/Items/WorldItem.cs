using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Items
{
    public class WorldItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string itemId = "scrap";
        [SerializeField] private int count = 1;
        private bool taken;

        public string Prompt
        {
            get
            {
                if (taken) return string.Empty;
                var record = ItemCatalog.Find(itemId);
                float each = record == null || record.Use == ItemUse.Ammo ? 0f : record.Weight;
                return ItemBrief.Offer(Loc.T("camp.take"), Loc.Item(itemId), count, each);
            }
        }

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
            bool got = LootContainer.Give(inventory, record.Id, count)
                || (record.Use == ItemUse.Ammo && inventory.TryAddItem(record.Id, record.DisplayName, record.Category, count, record.Weight));
            if (!got)
            {
                GameplayFeedback.Toast(Loc.T("camp.heavy"));
                return;
            }
            taken = true;
            GameplayFeedback.Toast(Loc.T("camp.picked") + " " + Loc.Item(record.Id));
            Destroy(gameObject);
        }
    }
}
