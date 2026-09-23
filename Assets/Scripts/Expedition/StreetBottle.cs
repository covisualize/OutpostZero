using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// A bottle on the street can be picked up and thrown. It breaks loud enough to pull a group.
    /// </summary>
    public class StreetBottle : MonoBehaviour, IInteractable
    {
        public const string Id = "street_bottle";
        public const float Weight = 0.25f;

        public string Prompt => Loc.T("toss.take_bottle");

        public bool CanInteract(PlayerInventory inventory)
        {
            return inventory != null;
        }

        public void Interact(PlayerInventory inventory)
        {
            if (inventory == null) return;
            var record = ItemCatalog.Find(Id);
            string name = record != null ? record.DisplayName : "Street Bottle";
            float weight = record != null ? record.Weight : Weight;
            var category = record != null ? record.Category : ItemCategory.KeyItem;
            if (!inventory.TryAddItem(Id, name, category, 1, weight))
            {
                GameplayFeedback.Toast(Loc.T("camp.heavy"));
                return;
            }
            GameplayFeedback.Toast(Loc.T("camp.picked") + " " + Loc.Item(Id));
            Destroy(gameObject);
        }
    }
}
