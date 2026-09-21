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
        public bool CampaignWon => ClearedCount >= 3;

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
            districts = new List<District>
            {
                new District { id = "ash_market", displayName = "Ash Market", encounter = "Loot the stalls, watch the alleys" },
                new District { id = "rail_yard", displayName = "Rail Yard", encounter = "Barrels and a brute pack" },
                new District { id = "old_hospital", displayName = "Old Hospital", encounter = "Medical caches, infected wards" },
                new District { id = "north_gate", displayName = "North Gate", encounter = "Final push to clear the ring" }
            };
            currentIndex = 0;
        }

        public void SelectIndex(int index)
        {
            if (districts.Count == 0) Seed();
            index = Mathf.Clamp(index, 0, districts.Count - 1);
            if (districts[index].cleared) return;
            currentIndex = index;
        }

        public bool Select(string id)
        {
            if (districts.Count == 0) Seed();
            for (int i = 0; i < districts.Count; i++)
            {
                if (districts[i].id != id || districts[i].cleared) continue;
                currentIndex = i;
                return true;
            }
            return false;
        }

        public void ApplyOpening()
        {
            var rules = DistrictRules.For(Current != null ? Current.id : "ash_market");
            DistrictRules.SetActiveTable(rules.LootTable);
            ObjectiveTracker.Instance?.SetGoals(rules.KillGoal, rules.ScrapGoal);
            float tension = rules.OpeningTension;
            if (FactionTrade.Instance != null && FactionTrade.Instance.Ambush) tension += 12f;
            HordeDirector.Instance?.ApplyOpening(tension, rules.SpawnInterval, rules.PreferredVariant);
            WeatherController.Instance?.SetFor(rules.Weather, 180f);
            string districtId = Current != null ? Current.id : "ash_market";
            DistrictDressing.Instance?.Build(districtId);
            SurvivorRoster.Instance?.RaiseCorpses(districtId);
        }

        public void ClearCurrent()
        {
            var district = Current;
            if (district == null || district.cleared) return;
            district.cleared = true;
            if (currentIndex < districts.Count - 1) currentIndex++;
            if (CampaignWon) GameplayFeedback.Toast("The ring is quiet. Outpost Zero holds.");
            FactionTrade.Instance?.NoteDistrictCleared();
        }

        public void RestoreCleared(int count)
        {
            if (districts.Count == 0) Seed();
            for (int i = 0; i < districts.Count; i++) districts[i].cleared = i < count;
            currentIndex = Mathf.Clamp(count, 0, districts.Count - 1);
        }

        public void ResetMap()
        {
            Seed();
            DistrictRules.SetActiveTable("");
        }
    }
}
