using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;

namespace OutpostZero.Colony
{
    public class FactionTrade : MonoBehaviour
    {
        public static FactionTrade Instance { get; private set; }

        [SerializeField] private int standing = 10;
        [SerializeField] private string faction = "Ash Market";
        private bool open;

        public int Standing => standing;
        public string Faction => faction;
        public bool Open => open;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void Toggle() => open = !open;

        public int Price(string itemId)
        {
            int basePrice = itemId == "medkit" ? 14 : itemId == "ammo_rifle" ? 9 : 6;
            float discount = 1f - Mathf.Clamp(standing, 0, 60) / 200f;
            return Mathf.Max(1, Mathf.RoundToInt(basePrice * discount));
        }

        public bool Buy(string itemId)
        {
            if (ColonyStorage.Instance == null || !ColonyStorage.Instance.TrySpendScrap(Price(itemId)))
            {
                GameplayFeedback.Toast("The merchant shakes their head");
                return false;
            }
            var record = ItemCatalog.Find(itemId);
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (record == null || inventory == null)
            {
                ColonyStorage.Instance.AddScrap(Price(itemId));
                return false;
            }
            if (record.Use == ItemUse.Ammo) inventory.GrantAmmoPublic(record.AmmoType, record.AmmoAmount);
            else inventory.TryAddItem(record.Id, record.DisplayName, record.Category, 1, record.Weight);
            standing = Mathf.Min(100, standing + 2);
            GameplayFeedback.Toast(faction + " deal sealed");
            return true;
        }

        public void SetStanding(int value) => standing = Mathf.Clamp(value, 0, 100);
    }
}
