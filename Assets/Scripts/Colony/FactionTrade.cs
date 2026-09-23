using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

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

        /// <summary>The same inputs as <see cref="Signature"/>, hashed without building a string.</summary>
        public int Key
        {
            get
            {
                var key = new UiKey();
                key.Add(open);
                for (int i = 0; i < standing.Length; i++) key.Add(standing[i]);
                key.Add(quests);
                key.Add(Day);
                key.Add(PostBuilt);
                return key.Value;
            }
        }
        public bool Ambush => CaravanBook.Ambush(StandingOf("militia"));

        private int Day => WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
        private bool PostBuilt => GridBuilder.Instance != null && GridBuilder.Instance.HasKind("TradingPost");
        private int Leadership => SurvivorRoster.Instance != null && SurvivorRoster.Instance.Leader != null ? SurvivorRoster.Instance.Leader.leadership : 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            FactionBook.Ensure();
        }

        public void Toggle()
        {
            if (!open && string.IsNullOrEmpty(ActiveId))
            {
                GameplayFeedback.Toast(StallVoice.Wait(null));
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

        public int Price(string itemId) => CaravanBook.Price(ActiveId, itemId, StandingOf(ActiveId), Leadership);
        public int Offer(string itemId) => CaravanBook.Offer(itemId, StandingOf(ActiveId));
        public string[] Stock => CaravanBook.Stock(ActiveId, StandingOf(ActiveId));

        public bool Buy(string itemId)
        {
            string faction = ActiveId;
            if (string.IsNullOrEmpty(faction))
            {
                GameplayFeedback.Toast(StallVoice.Wait(null));
                return false;
            }
            if (CaravanBook.Refuses(faction, StandingOf(faction)))
            {
                GameplayFeedback.Toast(StallVoice.Refuse(StallVoice.Name(faction, null), null));
                return false;
            }
            if (System.Array.IndexOf(Stock, itemId) < 0)
            {
                GameplayFeedback.Toast(StallVoice.Refuse(StallVoice.Name(faction, null), null));
                return false;
            }
            int price = Price(itemId);
            if (ColonyStorage.Instance == null || !ColonyStorage.Instance.TrySpendScrap(price))
            {
                GameplayFeedback.Toast(StallVoice.Shake(null));
                return false;
            }
            var record = ItemCatalog.Find(itemId);
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (record == null || inventory == null)
            {
                ColonyStorage.Instance.RestoreScrap(price);
                return false;
            }
            int stock = 0;
            if (record.Use == ItemUse.Ammo)
            {
                inventory.GrantAmmoPublic(record.AmmoType, record.AmmoAmount);
                stock = AmmoPress.Rounds(itemId, 1);
                if (stock > 0) ColonyStorage.Instance.AddRounds(stock);
            }
            else if (!inventory.TryAddItem(record.Id, record.DisplayName, record.Category, 1, record.Weight))
            {
                ColonyStorage.Instance.RestoreScrap(price);
                GameplayFeedback.Toast(StallVoice.Full(null));
                return false;
            }
            CaravanBook.Shift(standing, faction, 2);
            GameplayFeedback.Toast(StallVoice.Deal(faction, stock, null));
            return true;
        }

        public bool SellBandage() => Sell("bandage");

        public bool Sell(string itemId)
        {
            string faction = ActiveId;
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (string.IsNullOrEmpty(faction) || inventory == null || !CaravanBook.Sellable(itemId) || !inventory.TryConsume(itemId, 1))
            {
                GameplayFeedback.Toast(StallVoice.NoBandage(null));
                return false;
            }
            int payout = Offer(itemId);
            int stored = ColonyStorage.Instance != null ? ColonyStorage.Instance.AddScrap(payout) : 0;
            if (stored <= 0)
            {
                var record = ItemCatalog.Find(itemId);
                if (record != null) inventory.TryAddItem(record.Id, record.DisplayName, record.Category, 1, record.Weight);
                GameplayFeedback.Toast(YardSay.Stores(null));
                return false;
            }
            CaravanBook.Shift(standing, faction, 1);
            GameplayFeedback.Toast(StallVoice.Bartered(stored, null));
            return true;
        }

        public bool DeliverMedkits()
        {
            if (CaravanBook.QuestDone(quests, "clinic")) return false;
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            if (inventory == null || !inventory.TrySpendMedical(4))
            {
                GameplayFeedback.Toast(StallVoice.Quest("clinic", false, null));
                return false;
            }
            quests = CaravanBook.MarkQuest(quests, "clinic");
            CaravanBook.Shift(standing, "clinic", 15);
            ColonyStorage.Instance?.LearnPrint("dressing");
            GameplayFeedback.Toast(StallVoice.Blueprint(null));
            return true;
        }

        public void NoteDistrictCleared()
        {
            if (CaravanBook.QuestDone(quests, "farmers")) return;
            quests = CaravanBook.MarkQuest(quests, "farmers");
            CaravanBook.Shift(standing, "farmers", 10);
            GameplayFeedback.Toast(StallVoice.Nest(null));
        }

        public void NoteExtracted()
        {
            if (!CaravanBook.Visits(Day) || CaravanBook.QuestDone(quests, "caravan")) return;
            quests = CaravanBook.MarkQuest(quests, "caravan");
            CaravanBook.Shift(standing, "caravan", 10);
            GameplayFeedback.Toast(StallVoice.Through(null));
        }

        public void OnMorning(int day)
        {
            CaravanBook.Decay(standing);
            string visitor = CaravanBook.Visitor(day);
            if (!string.IsNullOrEmpty(visitor))
            {
                CaravanBook.Gift(standing, visitor);
                GameplayFeedback.Toast(StallVoice.Arrival(visitor, null));
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
