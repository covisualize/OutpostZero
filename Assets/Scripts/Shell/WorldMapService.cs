using System;
using System.Collections.Generic;
using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Colony;
using OutpostZero.Core;
using OutpostZero.Expedition;
using OutpostZero.Graphics;

namespace OutpostZero.Shell
{
    [Serializable]
    public class District
    {
        public string id;
        public string displayName;
        public string encounter;
        public bool cleared;
    }

    public class WorldMapService : MonoBehaviour
    {
        public static WorldMapService Instance { get; private set; }

        [SerializeField] private List<District> districts = new List<District>();
        [SerializeField] private int currentIndex;

        public IReadOnlyList<District> Districts => districts;
        public District Current => districts.Count == 0 ? null : districts[Mathf.Clamp(currentIndex, 0, districts.Count - 1)];
        public int CurrentIndex => districts.Count == 0 ? 0 : Mathf.Clamp(currentIndex, 0, districts.Count - 1);
        public int ClearedCount
        {
            get
            {
                int count = 0;
                foreach (var district in districts) if (district.cleared) count++;
                return count;
            }
        }
        private string parts = "";
        private int difficulty = 2;
        private bool broadcastWon;
        private bool endless;
        private int worldSeed = DistrictGenerator.DefaultSeed;
        private string street = "";

        public string Parts => parts;
        public int Difficulty => difficulty;
        public bool BroadcastWon => broadcastWon;
        public bool Endless => endless;
        public int WorldSeed => DistrictGenerator.Resolve(worldSeed);
        public string Street => street ?? "";
        public bool GeneratorBuilt => GridBuilder.Instance != null && GridBuilder.Instance.HasKind("Generator");
        public bool ReadyToBroadcast => CampaignBoard.Ready(parts, GeneratorBuilt, broadcastWon);
        public bool CampaignWon => CampaignBoard.Won(parts, GeneratorBuilt, broadcastWon);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            if (districts.Count == 0) Seed();
        }

        public void Seed()
        {
            var board = CampaignBoard.All();
            districts = new List<District>(board.Length);
            for (int i = 0; i < board.Length; i++)
            {
                districts.Add(new District { id = board[i].Id, displayName = board[i].Name, encounter = board[i].Encounter });
            }
            currentIndex = 0;
            parts = "";
            broadcastWon = false;
            endless = false;
            street = "";
        }

        public void NoteStreet(string mark)
        {
            string district = Current != null ? Current.id : "";
            street = StreetLedger.Note(street, district, mark);
        }

        public bool StreetTaken(string mark)
        {
            string district = Current != null ? Current.id : "";
            return StreetLedger.Has(street, district, mark);
        }

        public void NoteHold(string mark, string body)
        {
            string district = Current != null ? Current.id : "";
            street = StreetLedger.Hold(street, district, mark, body);
        }

        public string StreetLeft(string mark)
        {
            string district = Current != null ? Current.id : "";
            return StreetLedger.Read(street, district, mark);
        }

        public void RestoreStreet(string packed)
        {
            street = packed ?? "";
        }

        public bool TryBeginEndless()
        {
            if (!CampaignWon) return false;
            endless = true;
            return true;
        }

        public void RerollSeed()
        {
            worldSeed = DistrictGenerator.Resolve(worldSeed) + 17;
        }

        public void SetSeed(int seed)
        {
            worldSeed = DistrictGenerator.Resolve(seed);
        }

        public void SelectIndex(int index)
        {
            if (districts.Count == 0) Seed();
            index = Mathf.Clamp(index, 0, districts.Count - 1);
            if (districts[index].cleared && !endless) return;
            if (!CampaignBoard.Reachable(districts[index].id, ClearedIds())) return;
            currentIndex = index;
        }

        public bool Select(string id)
        {
            if (districts.Count == 0) Seed();
            for (int i = 0; i < districts.Count; i++)
            {
                if (districts[i].id != id) continue;
                if (districts[i].cleared && !endless) continue;
                if (!CampaignBoard.Reachable(id, ClearedIds()))
                {
                    GameplayFeedback.Toast(GateLine.Road(null));
                    return false;
                }
                currentIndex = i;
                return true;
            }
            return false;
        }

        public void SpendTravel()
        {
            float hours = CampaignBoard.TravelHours(Current != null ? Current.id : "ash_market");
            WorldClock.Instance?.Advance(hours);
            float burned = CampServices.Instance != null ? CampServices.Instance.BurnTrip(hours) : 0f;
            if (burned > 0.05f) GameplayFeedback.Toast(Loc.T("camp.trip") + " " + FuelTank.Label(burned));
        }

        public void ApplyOpening()
        {
            string districtId = Current != null ? Current.id : "ash_market";
            var rules = DistrictRules.For(districtId);
            DistrictRules.SetActiveTable(rules.LootTable);
            int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            var curve = DifficultyProfile.For(CampaignBoard.Tier(districtId), day, difficulty);
            int kills = rules.KillGoal + curve.ExtraKills;
            bool lessonsDone = TutorialDirector.Instance == null || TutorialDirector.Instance.Finished;
            bool tutorial = TutorialRun.Applies(lessonsDone, endless);
            if (tutorial) ObjectiveTracker.Instance?.SetGoals(TutorialRun.KillGoal, TutorialRun.Scrap(rules.ScrapGoal));
            else ObjectiveTracker.Instance?.SetGoals(kills, rules.ScrapGoal);
            float tension = rules.OpeningTension + curve.Tension;
            bool ambush = !tutorial && FactionTrade.Instance != null && FactionTrade.Instance.Ambush;
            if (ambush) tension += 12f;
            float interval = Mathf.Max(3f, rules.SpawnInterval * curve.Interval);
            if (endless)
            {
                tension += EndlessShift.Tension(day);
                interval = Mathf.Max(3f, interval * EndlessShift.IntervalScale(day));
            }
            string prefer = string.IsNullOrEmpty(rules.PreferredVariant) ? curve.Prefer : rules.PreferredVariant;
            HordeDirector.Instance?.ApplyOpening(tension, interval, prefer, difficulty, CampaignBoard.Tier(districtId));
            var player = PlayerRegistry.Current;
            if (tutorial && player != null) HordeDirector.Instance?.BeginTutorial(player.transform.position, player.transform.forward);
            if (ambush)
            {
                HordeDirector.Instance?.DropAmbush(true);
                GameplayFeedback.Toast(Loc.T("ambush.warn"));
            }
            WeatherController.Instance?.SetDistrict(districtId);
            WeatherController.Instance?.SetFor(CloudDeck.Lay(rules.Weather, day), 180f);
            DistrictDressing.Instance?.Build(districtId);
            SurvivorRoster.Instance?.RaiseCorpses(districtId);
        }

        public void ClearCurrent()
        {
            var district = Current;
            if (district == null || district.cleared) return;
            district.cleared = true;
            string before = parts;
            parts = CampaignBoard.AddPart(parts, CampaignBoard.PartFor(district.id));
            if (parts != before) GameplayFeedback.Toast(GateLine.Radio(null));
            string print = CraftGate.Sheet(district.id);
            if (!string.IsNullOrEmpty(print) && ColonyStorage.Instance != null)
            {
                string known = ColonyStorage.Instance.Prints;
                ColonyStorage.Instance.LearnPrint(print);
                if (ColonyStorage.Instance.Prints != known) GameplayFeedback.Toast(Loc.T("camp.print") + ": " + Loc.T("print." + print));
            }
            if (!CurrentOpen()) SelectFirstOpen();
            FactionTrade.Instance?.NoteDistrictCleared();
        }

        public void NoteBroadcast()
        {
            broadcastWon = true;
        }

        public void GrantSpare()
        {
            parts = CampaignBoard.AddPart(parts, "spare");
        }

        public void RestoreCampaign(string radio, int storedDifficulty, int broadcast, int seed = 0, int endlessFlag = 0)
        {
            parts = radio ?? "";
            difficulty = DifficultyProfile.Resolve(storedDifficulty);
            broadcastWon = broadcast != 0;
            worldSeed = DistrictGenerator.Resolve(seed);
            endless = endlessFlag != 0 && broadcastWon;
            if (!CurrentOpen()) SelectFirstOpen();
        }

        public void RestoreCleared(int count)
        {
            if (districts.Count == 0) Seed();
            for (int i = 0; i < districts.Count; i++) districts[i].cleared = i < count;
            currentIndex = Mathf.Clamp(count, 0, districts.Count - 1);
            if (!CurrentOpen()) SelectFirstOpen();
        }

        public void ResetMap()
        {
            ResetMap(difficulty);
        }

        public void ResetMap(int storedDifficulty)
        {
            Seed();
            difficulty = DifficultyProfile.Resolve(storedDifficulty);
            worldSeed = DistrictGenerator.DefaultSeed;
            DistrictRules.SetActiveTable("");
        }

        private bool CurrentOpen()
        {
            var district = Current;
            if (district == null || district.cleared) return false;
            return CampaignBoard.Reachable(district.id, ClearedIds());
        }

        private void SelectFirstOpen()
        {
            var cleared = ClearedIds();
            for (int i = 0; i < districts.Count; i++)
            {
                if (districts[i].cleared) continue;
                if (!CampaignBoard.Reachable(districts[i].id, cleared)) continue;
                currentIndex = i;
                return;
            }
        }

        private string[] ClearedIds()
        {
            int count = ClearedCount;
            var ids = new string[count];
            int write = 0;
            for (int i = 0; i < districts.Count; i++)
            {
                if (!districts[i].cleared) continue;
                ids[write] = districts[i].id;
                write++;
            }
            return ids;
        }
    }
}
