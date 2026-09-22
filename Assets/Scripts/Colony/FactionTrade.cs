using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;

namespace OutpostZero.Colony
{
    public class FactionTrade : MonoBehaviour
    {
        public static FactionTrade Instance { get; private set; }

        private readonly int[] standing = { 10, 0, 0, 0 };
        private string quests = "";
        private bool open;

        public int Standing => standing[0];
        public string Faction => CaravanBook.Display(ActiveId);
        public bool Open => open;
        public string Quests => quests;
        public string ActiveId => CaravanBook.Counterparty(Day, PostBuilt);
        public string Signature => open + "|" + CaravanBook.Pack(standing) + "|" + quests + "|" + Day + "|" + PostBuilt;
        public bool Ambush => CaravanBook.Ambush(StandingOf("militia"));

        private int Day => WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
        private bool PostBuilt => GridBuilder.Instance != null && GridBuilder.Instance.HasKind("TradingPost");
        private bool LeaderPresent => SurvivorRoster.Instance != null && SurvivorRoster.Instance.Leader != null;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public void Toggle()
        {
            if (!open && string.IsNullOrEmpty(ActiveId))
            {
                GameplayFeedback.Toast("No caravan until the next visit");
                return;
            }
            open = !open;
        }

        public int StandingOf(string id)
        {
            int index = CaravanBook.IndexOf(id);
            if (index < 0) return 0;
            return standing[index];
        }

        public int Price(string itemId) => CaravanBook.Price(itemId, StandingOf(ActiveId), LeaderPresent);

        public bool Buy(string itemId)
        {
            string faction = ActiveId;
            if (string.IsNullOrEmpty(faction))
            {
                GameplayFeedback.Toast("No caravan until the next visit");
                return false;
            }
            if (CaravanBook.Refuses(faction, StandingOf(faction)))
            {
                GameplayFeedback.Toast(CaravanBook.Display(faction) + " will not trade");
                return false;
            }
            int price = Price(itemId);
            if (ColonyStorage.Instance == null || !ColonyStorage.Instance.TrySpendScrap(price))
            {
                GameplayFeedback.Toast("The merchant shakes their head");
                return false;
            }
            var record = ItemCatalog.Find(itemId);
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (record == null || inventory == null)
            {
                ColonyStorage.Instance.RestoreScrap(price);
                return false;
            }
            if (record.Use == ItemUse.Ammo) inventory.GrantAmmoPublic(record.AmmoType, record.AmmoAmount);
            else if (!inventory.TryAddItem(record.Id, record.DisplayName, record.Category, 1, record.Weight))
            {
                ColonyStorage.Instance.RestoreScrap(price);
                GameplayFeedback.Toast("The pack is full");
                return false;
            }
            CaravanBook.Shift(standing, faction, 2);
            GameplayFeedback.Toast(CaravanBook.Display(faction) + " deal sealed");
            return true;
        }

        public bool SellBandage()
        {
            string faction = ActiveId;
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (string.IsNullOrEmpty(faction) || inventory == null || !inventory.TryConsume("bandage", 1))
            {
                GameplayFeedback.Toast("No bandage to barter");
                return false;
            }
            int payout = Mathf.Max(1, Price("bandage") / 2);
            int stored = ColonyStorage.Instance != null ? ColonyStorage.Instance.AddScrap(payout) : 0;
            if (stored <= 0)
            {
                var record = ItemCatalog.Find("bandage");
                if (record != null) inventory.TryAddItem(record.Id, record.DisplayName, record.Category, 1, record.Weight);
                GameplayFeedback.Toast("Stores are full");
                return false;
            }
            CaravanBook.Shift(standing, faction, 1);
            GameplayFeedback.Toast("Bartered a bandage for " + stored + " scrap");
            return true;
        }

        public bool DeliverMedkits()
        {
            if (CaravanBook.QuestDone(quests, "clinic")) return false;
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (inventory == null || !inventory.TrySpendMedical(4))
            {
                GameplayFeedback.Toast("The Clinic wants 4 medkits");
                return false;
            }
            quests = CaravanBook.MarkQuest(quests, "clinic");
            CaravanBook.Shift(standing, "clinic", 15);
            ColonyStorage.Instance?.LearnPrint("dressing");
            GameplayFeedback.Toast("Clinic blueprint: field dressings");
            return true;
        }

        public void NoteDistrictCleared()
        {
            if (CaravanBook.QuestDone(quests, "farmers")) return;
            quests = CaravanBook.MarkQuest(quests, "farmers");
            CaravanBook.Shift(standing, "farmers", 10);
            GameplayFeedback.Toast("Free Farmers remember the cleared nest");
        }

        public void NoteExtracted()
        {
            if (!CaravanBook.Visits(Day) || CaravanBook.QuestDone(quests, "caravan")) return;
            quests = CaravanBook.MarkQuest(quests, "caravan");
            CaravanBook.Shift(standing, "caravan", 10);
            GameplayFeedback.Toast("The Caravan made it through");
        }

        public void OnMorning(int day)
        {
            CaravanBook.Decay(standing);
            string visitor = CaravanBook.Visitor(day);
            if (!string.IsNullOrEmpty(visitor))
            {
                CaravanBook.Gift(standing, visitor);
                GameplayFeedback.Toast(CaravanBook.Display(visitor) + " is at the gate");
            }
        }

        public void SetStanding(int value) => standing[0] = Mathf.Clamp(value, -100, 100);

        public void Restore(int legacy, string packed, string questPacked)
        {
            CaravanBook.Unpack(packed, legacy, standing);
            quests = questPacked ?? "";
        }

        public string Pack() => CaravanBook.Pack(standing);
    }
}
