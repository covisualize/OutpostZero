using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Items
{
    public class LootContainer : MonoBehaviour, IInteractable
    {
        [SerializeField] private string tableId = "crate";
        [SerializeField] private bool looted;

        public string Prompt => looted ? string.Empty : "Search container";

        public void Configure(string table)
        {
            tableId = string.IsNullOrEmpty(table) ? "crate" : table;
        }

        public bool CanInteract(PlayerInventory inventory) => !looted && inventory != null;

        public void Interact(PlayerInventory inventory)
        {
            if (!CanInteract(inventory)) return;
            looted = true;
            var grants = LootTables.Roll(tableId, GetInstanceID());
            int given = 0;
            foreach (var grant in grants)
            {
                if (grant.Count <= 0) continue;
                var record = ItemCatalog.Find(grant.ItemId);
                if (record == null) continue;
                if (record.Use == ItemUse.Ammo)
                {
                    inventory.GrantAmmoPublic(record.AmmoType, record.AmmoAmount * grant.Count);
                }
                else if (record.Id == "scrap")
                {
                    inventory.AddScrap(grant.Count);
                }
                else if (!inventory.TryAddItem(record.Id, record.DisplayName, record.Category, grant.Count, record.Weight))
                {
                    GameplayFeedback.Toast("Left some loot behind");
                    continue;
                }
                given++;
            }
            GameplayFeedback.Toast(given > 0 ? "Container searched" : "Empty");
        }
    }
}
