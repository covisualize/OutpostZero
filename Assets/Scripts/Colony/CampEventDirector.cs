using System;
using System.Globalization;
using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Graphics;
using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    /// <summary>The random-event state a save carries: what the last dawn brought and what is still waiting on the camp.</summary>
    public struct CampEventLog
    {
        public int RolledDay;
        public string LastId;
        public int LastDay;
        public string LastLine;
        public string StrangerId;
        public string StrangerName;
        public string StrangerTrait;
        public int StrangerDay;
        public int MerchantDay;
        public int SightedDay;
        public bool GeneratorBroken;

        public static CampEventLog Fresh => new CampEventLog
        {
            RolledDay = -1,
            LastId = "",
            LastDay = -1,
            LastLine = "",
            StrangerId = "",
            StrangerName = "",
            StrangerTrait = "",
            StrangerDay = -1,
            MerchantDay = -1,
            SightedDay = -1,
        };

        public string Pack()
        {
            return string.Join("|", new[]
            {
                "1", Num(RolledDay), Clean(LastId), Num(LastDay), Clean(LastLine), Clean(StrangerId), Clean(StrangerName),
                Clean(StrangerTrait), Num(StrangerDay), Num(MerchantDay), Num(SightedDay), GeneratorBroken ? "1" : "0",
            });
        }

        public static CampEventLog Unpack(string packed)
        {
            var log = Fresh;
            if (string.IsNullOrEmpty(packed)) return log;
            var parts = packed.Split('|');
            if (parts.Length < 12 || parts[0] != "1") return log;
            log.RolledDay = Read(parts[1]);
            log.LastId = parts[2];
            log.LastDay = Read(parts[3]);
            log.LastLine = parts[4];
            log.StrangerId = parts[5];
            log.StrangerName = parts[6];
            log.StrangerTrait = parts[7];
            log.StrangerDay = Read(parts[8]);
            log.MerchantDay = Read(parts[9]);
            log.SightedDay = Read(parts[10]);
            log.GeneratorBroken = parts[11] == "1";
            return log;
        }

        private static string Num(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static int Read(string text)
        {
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : -1;
        }

        private static string Clean(string text) => (text ?? "").Replace("|", "/");
    }

    /// <summary>
    /// Rolls one weighted random event at each dawn and applies it. The stranger waits for the board's answer
    /// until the next dawn; a broken generator stays down until an engineer repairs it.
    /// </summary>
    public class CampEventDirector : MonoBehaviour, ISaveable
    {
        public const string SaveKey = "camp_events";

        public static CampEventDirector Instance { get; private set; }

        private CampEventLog log = CampEventLog.Fresh;

        public string SaveId => SaveKey;
        public CampEventLog Log => log;
        public event Action OnChanged;

        private static int Day => WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
        private static int Seed => WorldMapService.Instance != null ? WorldMapService.Instance.WorldSeed : DistrictGenerator.DefaultSeed;

        public bool StrangerWaiting => log.StrangerDay == Day && !string.IsNullOrEmpty(log.StrangerId);
        public bool GeneratorBroken => log.GeneratorBroken;
        public string Latest => log.LastDay == Day ? log.LastLine : "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            CampEventBook.Ensure();
            SaveRegistry.Register(this);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            SaveRegistry.Unregister(this);
            Instance = null;
        }

        public string CaptureState() => log.Pack();

        public void RestoreState(string state)
        {
            log = CampEventLog.Unpack(state);
            Reapply();
        }

        public void ResetRun()
        {
            log = CampEventLog.Fresh;
            Reapply();
        }

        private void Reapply()
        {
            CampServices.Instance?.SetGeneratorBroken(log.GeneratorBroken);
            FactionTrade.Instance?.Summon(log.MerchantDay);
            NightRaidController.Instance?.Sight(log.SightedDay);
            OnChanged?.Invoke();
        }

        public CampEventContext Context(int day)
        {
            var roster = SurvivorRoster.Instance;
            var services = CampServices.Instance;
            var sky = WeatherController.Instance != null ? WeatherController.Instance.Kind : WeatherKind.Clear;
            return new CampEventContext
            {
                Day = day,
                Room = roster != null && roster.HasRoom,
                Rain = sky == WeatherKind.Rain || sky == WeatherKind.Storm,
                Feud = roster != null && roster.HasFeud(),
                Healthy = roster != null && roster.HasHealthy(),
                MerchantAway = FactionTrade.Instance != null && FactionTrade.Instance.Away,
                Generator = services != null && services.GeneratorOnline,
                Water = services != null && services.WaterOnline,
            };
        }

        public void Dawn(int day)
        {
            if (day <= log.RolledDay) return;
            log.RolledDay = day;
            string id = CampEventTable.Roll(Context(day), Seed);
            if (!string.IsNullOrEmpty(id)) Apply(id, day);
            OnChanged?.Invoke();
        }

        public void Apply(string id, int day)
        {
            var row = CampEventTable.Find(id);
            string title = Title(row);
            string detail = "";
            var roster = SurvivorRoster.Instance;
            var storage = ColonyStorage.Instance;
            int salt = CampEventTable.Mix(Seed, day * 31 + 5);
            switch (id)
            {
                case CampEventTable.Stranger:
                    var draft = SurvivorDraw.Stranger(Seed, day);
                    log.StrangerId = draft.Id;
                    log.StrangerName = draft.Name;
                    log.StrangerTrait = draft.Trait;
                    log.StrangerDay = day;
                    detail = draft.Name + ", " + Loc.Trait(draft.Trait);
                    break;
                case CampEventTable.Argument:
                    if (roster != null && roster.FlareFeud(salt, out string a, out string b)) detail = a + " / " + b;
                    break;
                case CampEventTable.Generator:
                    log.GeneratorBroken = true;
                    CampServices.Instance?.SetGeneratorBroken(true);
                    break;
                case CampEventTable.Sickness:
                    if (roster != null) detail = roster.Sicken(salt);
                    break;
                case CampEventTable.RainFill:
                    if (storage != null) detail = "+" + storage.AddWater(CampEventTable.RainWater) + " " + Loc.T("event.water");
                    break;
                case CampEventTable.Merchant:
                    log.MerchantDay = day;
                    FactionTrade.Instance?.Summon(day);
                    break;
                case CampEventTable.Sighting:
                    log.SightedDay = day;
                    NightRaidController.Instance?.Sight(day);
                    break;
                case CampEventTable.Cache:
                    if (storage != null)
                        detail = "+" + storage.AddScrap(CampEventTable.CacheScrap) + " " + Loc.T("result.scrap") + ", +" + storage.AddCloth(CampEventTable.CacheCloth) + " " + Loc.T("event.cloth");
                    break;
            }
            log.LastId = id;
            log.LastDay = day;
            log.LastLine = detail.Length > 0 ? title + " (" + detail + ")" : title;
            GameplayFeedback.Toast(log.LastLine);
        }

        public bool TakeStranger()
        {
            if (!StrangerWaiting) return false;
            var roster = SurvivorRoster.Instance;
            if (roster == null || !roster.Adopt(log.StrangerId, log.StrangerName, log.StrangerTrait))
            {
                GameplayFeedback.Toast(Loc.T("event.full"));
                return false;
            }
            GameplayFeedback.Toast(log.StrangerName + " " + Loc.T("event.joined"));
            ClearStranger();
            return true;
        }

        public void TurnAway()
        {
            if (!StrangerWaiting) return;
            GameplayFeedback.Toast(log.StrangerName + " " + Loc.T("event.left"));
            ClearStranger();
        }

        private void ClearStranger()
        {
            log.StrangerId = "";
            log.StrangerName = "";
            log.StrangerTrait = "";
            log.StrangerDay = -1;
            OnChanged?.Invoke();
        }

        public bool Repair()
        {
            if (!log.GeneratorBroken) return false;
            var pack = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<OutpostZero.Player.PlayerInventory>() : null;
            if (pack != null && pack.TryConsume(OutpostZero.Expedition.ObjectivePlan.GeneratorPart))
            {
                Mend("event.repaired_part");
                return true;
            }
            var roster = SurvivorRoster.Instance;
            var storage = ColonyStorage.Instance;
            int skill = roster != null ? roster.BestEngineering() : 0;
            if (storage == null || !CampEventTable.CanRepair(skill, storage.Scrap, storage.Tape)
                || !storage.TrySpendBill(CampEventTable.RepairScrap, 0, 0, CampEventTable.RepairTape))
            {
                GameplayFeedback.Toast(RepairNeed(null));
                return false;
            }
            Mend("event.repaired");
            return true;
        }

        private void Mend(string line)
        {
            log.GeneratorBroken = false;
            CampServices.Instance?.SetGeneratorBroken(false);
            GameplayFeedback.Toast(Loc.T(line));
            OnChanged?.Invoke();
        }

        public static string RepairNeed(string language)
        {
            string head = string.IsNullOrEmpty(language) ? Loc.T("event.repair_need") : Loc.T("event.repair_need", language);
            return head + " " + CampEventTable.RepairSkill + ", " + CampEventTable.RepairScrap + " " + Word("result.scrap", language)
                + ", " + CampEventTable.RepairTape + " " + Word("event.tape", language) + " " + Word("event.repair_part", language);
        }

        public static string Title(CampEventRow row)
        {
            return Title(row, null);
        }

        public static string Title(CampEventRow row, string language)
        {
            if (string.IsNullOrEmpty(row.Key)) return row.Fallback ?? "";
            string line = Word(row.Key, language);
            return line == row.Key && !string.IsNullOrEmpty(row.Fallback) ? row.Fallback : line;
        }

        private static string Word(string key, string language)
        {
            return string.IsNullOrEmpty(language) ? Loc.T(key) : Loc.T(key, language);
        }
    }
}
