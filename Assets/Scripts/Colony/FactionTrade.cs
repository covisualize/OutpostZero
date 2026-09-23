using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Expedition;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    public class FactionTrade : MonoBehaviour
    {
        public static FactionTrade Instance { get; private set; }

        private int[] standing = { 10, 0, 0, 0 };
        private string strays = "";
        private string quests = "";
        private string sold = "";
        private bool open;
        private int summonedDay = -1;

        public int Standing => standing[0];
        public string Faction => CaravanBook.Display(ActiveId);
        public bool Open => open;
        public string Quests => quests;
        public string Sold => sold;
        public string ActiveId => CaravanBook.Counterparty(Day, PostBuilt || summonedDay == Day);
        public bool Away => string.IsNullOrEmpty(ActiveId);
        public string Signature => open + "|" + Pack() + "|" + quests + "|" + sold + "|" + Day + "|" + PostBuilt;

        /// <summary>The same inputs as <see cref="Signature"/>, hashed without building a string.</summary>
        public int Key
        {
            get
            {
                var key = new UiKey();
                key.Add(open);
                Fit();
                for (int i = 0; i < standing.Length; i++) key.Add(standing[i]);
                key.Add(quests);
                key.Add(sold);
                key.Add(Day);
                key.Add(PostBuilt);
                key.Add(summonedDay);
                key.Add(ColonyStorage.Instance != null ? ColonyStorage.Instance.Meds : 0);
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
            Fit();
        }

        /// <summary>Grows the standing row when the faction book has added factions since it was made.</summary>
        private void Fit()
        {
            standing = CaravanBook.Fit(standing);
        }

        /// <summary>A caravan that turns up unannounced trades at the gate for the rest of that day.</summary>
        public void Summon(int day)
        {
            summonedDay = day;
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
            Fit();
            int index = CaravanBook.IndexOf(id);
            if (index < 0) return 0;
            return standing[index];
        }

        public int Price(string itemId) => CaravanBook.Price(ActiveId, itemId, StandingOf(ActiveId), Leadership);
        public int Offer(string itemId) => CaravanBook.Offer(itemId, StandingOf(ActiveId));
        public string[] Stock => CaravanBook.Stock(ActiveId, StandingOf(ActiveId));

        /// <summary>Today's shelf at the stall, before what the camp has already bought today.</summary>
        public List<KeyValuePair<string, int>> Shelf => StallShelf.Roll(ActiveId, Day, StandingOf(ActiveId));

        public int Left(string itemId) => StallShelf.Left(Shelf, sold, Day, ActiveId, itemId);

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
            if (Left(itemId) <= 0)
            {
                GameplayFeedback.Toast(StallVoice.SoldOut(null));
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
            sold = StallShelf.MarkSold(sold, Day, faction, itemId);
            Fit();
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
            Fit();
            CaravanBook.Shift(standing, faction, 1);
            GameplayFeedback.Toast(StallVoice.Bartered(stored, null));
            return true;
        }

        public int MedsWanted
        {
            get
            {
                var quest = FactionQuest.For("clinic");
                return FactionQuest.Has(quest) && quest.Kind == ObjectiveKind.Collect ? quest.Count : 0;
            }
        }

        public bool DeliverMeds()
        {
            int wanted = MedsWanted;
            if (wanted <= 0 || CaravanBook.QuestDone(quests, "clinic")) return false;
            var inventory = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            var storage = ColonyStorage.Instance;
            int shelf = FactionQuest.Shelf(wanted, storage != null ? storage.Meds : 0);
            int rest = wanted - shelf;
            if (rest > 0 && (inventory == null || !inventory.TrySpendMeds(rest)))
            {
                GameplayFeedback.Toast(StallVoice.Quest("clinic", false, null));
                return false;
            }
            if (shelf > 0) storage.TakeMeds(shelf);
            Complete("clinic");
            return true;
        }

        /// <summary>The caravan stands in the districts today, so its porter can be walked out.</summary>
        public bool CaravanOut => !string.IsNullOrEmpty(CaravanBook.Counterparty(Day, PostBuilt));

        /// <summary>The unfinished field quests the next run carries on its board.</summary>
        public List<ObjectiveSpec> FieldQuests() => FactionQuest.Open(quests, CaravanOut);

        /// <summary>Pays every faction quest the leader brought home done on the board.</summary>
        public void NoteExtracted(ObjectiveBoard board)
        {
            foreach (string faction in FactionQuest.Earned(board, quests)) Complete(faction);
        }

        private void Complete(string faction)
        {
            if (CaravanBook.QuestDone(quests, faction)) return;
            quests = CaravanBook.MarkQuest(quests, faction);
            Fit();
            CaravanBook.Shift(standing, faction, FactionQuest.CodeStanding(faction));
            string print = FactionQuest.CodePrint(faction);
            if (print.Length > 0)
            {
                ColonyStorage.Instance?.LearnPrint(print);
                GameplayFeedback.Toast(StallVoice.Blueprint(null));
            }
            if (faction == "farmers") GameplayFeedback.Toast(StallVoice.Nest(null));
            else if (faction == "caravan") GameplayFeedback.Toast(StallVoice.Through(null));
        }

        public void OnMorning(int day)
        {
            Fit();
            CaravanBook.Decay(standing);
            string visitor = CaravanBook.Visitor(day);
            if (!string.IsNullOrEmpty(visitor))
            {
                CaravanBook.Gift(standing, visitor);
                GameplayFeedback.Toast(StallVoice.Arrival(visitor, null));
            }
        }

        public void RestoreSold(string packed)
        {
            sold = packed ?? "";
        }

        public void SetStanding(int value)
        {
            Fit();
            standing[0] = Mathf.Clamp(value, -100, 100);
        }

        public void Restore(int legacy, string packed, string questPacked)
        {
            Fit();
            CaravanBook.Unpack(packed, legacy, standing);
            strays = CaravanBook.Strays(packed);
            quests = questPacked ?? "";
        }

        public string Pack()
        {
            Fit();
            return CaravanBook.Pack(standing, strays);
        }
    }
}
