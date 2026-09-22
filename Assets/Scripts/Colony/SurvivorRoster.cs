using System;
using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    [Serializable]
    public class Survivor
    {
        public string id;
        public string displayName;
        public string trait;
        public bool alive = true;
        public bool leader;
        public float morale = 70f;
        public float hunger = 78f;
        public float thirst = 78f;
        public int opinion = 18;
        public int injury;
        public string task = "Rest";
        public string bond = "";
    }

    public class SurvivorRoster : MonoBehaviour
    {
        public static SurvivorRoster Instance { get; private set; }

        [SerializeField] private List<Survivor> survivors = new List<Survivor>();
        public IReadOnlyList<Survivor> Survivors => survivors;
        public event Action OnRosterChanged;
        public string DayNotes { get; private set; } = "";
        private readonly List<SuccessionLedger.Memorial> memorials = new List<SuccessionLedger.Memorial>();
        private readonly List<SuccessionLedger.CorpseMark> corpses = new List<SuccessionLedger.CorpseMark>();
        public IReadOnlyList<SuccessionLedger.Memorial> Memorials => memorials;

        public Survivor Leader
        {
            get
            {
                for (int i = 0; i < survivors.Count; i++)
                {
                    if (survivors[i].alive && survivors[i].leader) return survivors[i];
                }
                return null;
            }
        }

        public void CopyLeaderNeeds(float hunger, float thirst)
        {
            var leader = Leader;
            if (leader == null) return;
            leader.hunger = Mathf.Clamp(hunger, 0f, 100f);
            leader.thirst = Mathf.Clamp(thirst, 0f, 100f);
            OnRosterChanged?.Invoke();
        }

        public bool ReadLeaderNeeds(out float hunger, out float thirst)
        {
            var leader = Leader;
            if (leader == null)
            {
                hunger = 0f;
                thirst = 0f;
                return false;
            }
            hunger = leader.hunger;
            thirst = leader.thirst;
            return true;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            if (survivors.Count == 0) Seed();
        }

        public void Seed()
        {
            memorials.Clear();
            corpses.Clear();
            survivors.Clear();
            survivors.Add(Make("mara", "Mara Quill", "Steady Hands", true, "Close to Jonas"));
            survivors.Add(Make("jonas", "Jonas Reed", "Light Sleeper", false, "Close to Mara"));
            survivors.Add(Make("priya", "Priya Sen", "Field Medic", false, "Trusts Ellis"));
            survivors.Add(Make("ellis", "Ellis Ward", "Scrounger", false, "Trusts Priya"));
            OnRosterChanged?.Invoke();
        }

        private static Survivor Make(string id, string name, string trait, bool leader, string bond)
        {
            return new Survivor
            {
                id = id,
                displayName = name,
                trait = trait,
                leader = leader,
                morale = 72f,
                hunger = 78f,
                thirst = 78f,
                opinion = string.IsNullOrEmpty(bond) ? 0 : 18,
                task = "Rest",
                bond = bond
            };
        }

        public bool MarkLeaderDead(Vector3 corpsePosition)
        {
            return MarkLeaderDead(corpsePosition, "killed");
        }

        public bool MarkLeaderDead(Vector3 corpsePosition, string cause)
        {
            var leader = Leader;
            string fallen = leader != null ? leader.displayName : "";
            if (leader != null)
            {
                leader.alive = false;
                leader.leader = false;
                leader.task = "Fallen";
            }
            var days = Snapshot();
            SuccessionLedger.Grieve(days, fallen);
            for (int i = 0; i < days.Count && i < survivors.Count; i++) survivors[i].morale = days[i].morale;
            string district = OutpostZero.Shell.WorldMapService.Instance != null && OutpostZero.Shell.WorldMapService.Instance.Current != null
                ? OutpostZero.Shell.WorldMapService.Instance.Current.id
                : "ash_market";
            int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            int kills = GameManager.Instance != null ? GameManager.Instance.ZombiesKilled : 0;
            var carried = PlayerRegistry.Current != null ? PlayerRegistry.Current.GetComponent<PlayerInventory>() : null;
            string gear = carried != null ? carried.TakeGear() : "";
            memorials.Add(new SuccessionLedger.Memorial
            {
                name = fallen,
                day = day,
                kills = kills,
                cause = string.IsNullOrEmpty(cause) ? "killed" : cause,
                district = district
            });
            corpses.Add(new SuccessionLedger.CorpseMark
            {
                district = district,
                x = corpsePosition.x,
                y = corpsePosition.y,
                z = corpsePosition.z,
                name = fallen,
                gear = gear,
                recovered = false
            });
            SpawnCorpse(corpsePosition, corpses.Count - 1, gear);
            OnRosterChanged?.Invoke();
            return NextLiving() != null;
        }

        public bool WoundLeader()
        {
            var leader = Leader;
            if (leader == null) return false;
            leader.injury = Math.Max(leader.injury, SuccessionLedger.MercyInjury);
            var days = Snapshot();
            SuccessionLedger.Grieve(days, "");
            for (int i = 0; i < days.Count && i < survivors.Count; i++) survivors[i].morale = days[i].morale;
            ColonyStorage.Instance?.TrySpendScrap(SuccessionLedger.MercyScrap);
            OnRosterChanged?.Invoke();
            return true;
        }

        public void Recover(int index)
        {
            if (index < 0 || index >= corpses.Count) return;
            corpses[index].recovered = true;
            OnRosterChanged?.Invoke();
        }

        public void RestoreStory(string memorialPacked, string corpsePacked)
        {
            memorials.Clear();
            memorials.AddRange(SuccessionLedger.UnpackMemorials(memorialPacked));
            corpses.Clear();
            corpses.AddRange(SuccessionLedger.UnpackCorpses(corpsePacked));
            OnRosterChanged?.Invoke();
        }

        public string PackMemorials() => SuccessionLedger.PackMemorials(memorials);

        public string PackCorpses() => SuccessionLedger.PackCorpses(corpses);

        public void RaiseCorpses(string districtId)
        {
            var standing = FindObjectsByType<FallenGear>(FindObjectsSortMode.None);
            for (int i = 0; i < standing.Length; i++)
            {
                if (standing[i] != null) Destroy(standing[i].gameObject);
            }
            for (int i = 0; i < corpses.Count; i++)
            {
                var mark = corpses[i];
                if (mark.recovered || mark.district != districtId) continue;
                SpawnCorpse(new Vector3(mark.x, mark.y, mark.z), i, mark.gear);
            }
        }

        public Survivor PromoteNext() => Promote(null);

        public Survivor Promote(string id)
        {
            Survivor next = string.IsNullOrEmpty(id) ? NextLiving() : Find(id);
            if (next == null || !next.alive) next = NextLiving();
            if (next == null) return null;
            for (int i = 0; i < survivors.Count; i++) survivors[i].leader = false;
            next.leader = true;
            next.task = "Lead";
            OnRosterChanged?.Invoke();
            return next;
        }

        public void WoundFromRaid(int salt)
        {
            int count = survivors.Count;
            if (count == 0) return;
            var alive = new bool[count];
            var guard = new bool[count];
            for (int i = 0; i < count; i++)
            {
                alive[i] = survivors[i].alive && survivors[i].task != "Fallen" && survivors[i].task != "Left";
                guard[i] = survivors[i].task == "Guard";
            }
            int index = RaidPlan.Pick(alive, guard, salt);
            if (index < 0) return;
            var person = survivors[index];
            person.injury = RaidBreach.Wound(person.injury, true, out bool fallen);
            if (!fallen)
            {
                GameplayFeedback.Toast(person.displayName + " " + Loc.T("camp.hit"));
                OnRosterChanged?.Invoke();
                return;
            }
            if (person.leader)
            {
                MarkLeaderDead(new Vector3(-6f, 0f, -8f), "raid");
                return;
            }
            person.alive = false;
            person.task = "Fallen";
            var days = Snapshot();
            SuccessionLedger.Grieve(days, person.displayName);
            for (int i = 0; i < days.Count && i < survivors.Count; i++) survivors[i].morale = days[i].morale;
            string district = WorldMapService.Instance != null && WorldMapService.Instance.Current != null
                ? WorldMapService.Instance.Current.id
                : "ash_market";
            int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            int kills = GameManager.Instance != null ? GameManager.Instance.ZombiesKilled : 0;
            memorials.Add(new SuccessionLedger.Memorial
            {
                name = person.displayName,
                day = day,
                kills = kills,
                cause = "raid",
                district = district
            });
            ColonyStorage.Instance?.AddBodies(1);
            GameplayFeedback.Toast(person.displayName + " " + Loc.T("camp.fell"));
            OnRosterChanged?.Invoke();
        }

        public void Assign(string id, string task)
        {
            var survivor = Find(id);
            if (survivor == null || !survivor.alive) return;
            survivor.task = task;
            OnRosterChanged?.Invoke();
        }

        public void TickTasks()
        {
            var storage = ColonyStorage.Instance;
            int day = WorldClock.Instance != null ? WorldClock.Instance.Day : 1;
            int index = 0;
            foreach (var survivor in survivors)
            {
                index++;
                if (!survivor.alive) continue;
                switch (survivor.task)
                {
                    case "Scavenge":
                        int scrap = Pay(4 + (survivor.trait == "Scrounger" ? 3 : 0), survivor.morale);
                        if (scrap > 0 && storage != null) storage.AddScrap(scrap);
                        if (scrap > 0) survivor.morale = Mathf.Max(0f, survivor.morale - 4f);
                        if (storage != null)
                        {
                            CraftBill.Salvage(day * 17 + index, survivor.trait == "Scrounger", out int cloth, out int chemicals, out int tape);
                            storage.AddCloth(cloth);
                            if (chemicals > 0) storage.AddChemicals(chemicals);
                            if (tape > 0) storage.AddTape(tape);
                            if (scrap > 0) storage.AddRaw(1);
                            if (scrap > 0) storage.AddRounds(GuardVolley.Brought(survivor.trait == "Scrounger"));
                        }
                        break;
                    case "Cook":
                        int hands = Pay(2, survivor.morale);
                        bool fire = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("Campfire");
                        int raw = storage != null ? storage.Raw : 0;
                        CookPot.Serve(fire, raw, hands, out int spent, out int served, out int lift);
                        if (spent > 0 && storage != null) storage.TakeRaw(spent);
                        if (served > 0 && storage != null) storage.AddFood(served);
                        if (lift > 0) survivor.morale = Mathf.Min(100f, survivor.morale + lift);
                        break;
                    case "Guard":
                        int watch = Pay(1, survivor.morale);
                        if (watch > 0) survivor.morale = Mathf.Max(0f, survivor.morale - 2f);
                        if (watch > 0 && storage != null) storage.AddSecurity(watch);
                        break;
                    case "Rest":
                        survivor.morale = Mathf.Min(100f, survivor.morale + 8f);
                        break;
                    case "Medic":
                        if (ColonyDay.OutputScale(survivor.morale) <= 0f) break;
                        survivor.morale = Mathf.Min(100f, survivor.morale + 2f);
                        var leader = PlayerRegistry.Current;
                        leader?.GetComponent<Combat.HealthSystem>()?.Heal(12f);
                        leader?.GetComponent<StatusEffectController>()?.ClearInjury();
                        for (int i = 0; i < survivors.Count; i++)
                        {
                            if (survivors[i].alive && survivors[i].injury > 0) survivors[i].injury--;
                        }
                        break;
                    case "Build":
                        int pace = BuildSite.Shift(survivor.trait, survivor.morale);
                        if (pace > 0 && GridBuilder.Instance != null && (GridBuilder.Instance.Raise(pace) || GridBuilder.Instance.Patch(pace) || GridBuilder.Instance.Lift(pace)))
                            survivor.morale = Mathf.Max(0f, survivor.morale - 2f);
                        break;
                    case "Clear":
                        int haul = YardDead.Hands(survivor.morale);
                        if (haul > 0 && storage != null && storage.TakeBodies(haul) > 0)
                            survivor.morale = Mathf.Max(0f, survivor.morale - 2f);
                        break;
                }
            }
            OnRosterChanged?.Invoke();
        }

        public void EndDay(bool expeditionWon)
        {
            EndDay(expeditionWon, "");
        }

        public void EndDay(bool expeditionWon, string fallenName)
        {
            GridBuilder.Instance?.Grow();
            var days = Snapshot();
            int food = ColonyStorage.Instance != null ? ColonyStorage.Instance.Food : 0;
            int water = ColonyStorage.Instance != null ? ColonyStorage.Instance.Water : 0;
            bool cot = CampServices.Instance != null && CampServices.Instance.CotOnline;
            int bodies = ColonyStorage.Instance != null ? ColonyStorage.Instance.Bodies : 0;
            int raw = ColonyStorage.Instance != null ? ColonyStorage.Instance.Raw : 0;
            var notes = ColonyDay.Simulate(days, ref food, ref water, cot, expeditionWon, fallenName, bodies, ref raw);
            ApplySnapshot(days);
            Spend(food, water);
            if (ColonyStorage.Instance != null) ColonyStorage.Instance.SetRaw(raw);
            Publish(notes);
            FactionTrade.Instance?.OnMorning(WorldClock.Instance != null ? WorldClock.Instance.Day : 1);
        }

        public void RewardReturn()
        {
            var days = Snapshot();
            var notes = ColonyDay.RewardReturn(days);
            ApplySnapshot(days);
            Publish(notes);
        }

        private static int Pay(int amount, float morale)
        {
            float scale = ColonyDay.OutputScale(morale);
            if (scale <= 0f) return 0;
            if (scale > 1f) return amount + 1;
            if (scale < 1f) return Math.Max(1, amount - 1);
            return amount;
        }

        private List<ColonistDay> Snapshot()
        {
            var days = new List<ColonistDay>(survivors.Count);
            for (int i = 0; i < survivors.Count; i++)
            {
                var survivor = survivors[i];
                days.Add(new ColonistDay
                {
                    id = survivor.id,
                    trait = survivor.trait,
                    task = survivor.task,
                    bond = survivor.bond,
                    alive = survivor.alive,
                    leader = survivor.leader,
                    morale = survivor.morale,
                    hunger = survivor.hunger,
                    thirst = survivor.thirst,
                    opinion = survivor.opinion,
                    injury = survivor.injury
                });
            }
            return days;
        }

        private void ApplySnapshot(List<ColonistDay> days)
        {
            int count = Math.Min(survivors.Count, days.Count);
            for (int i = 0; i < count; i++)
            {
                var survivor = survivors[i];
                var day = days[i];
                survivor.task = day.task;
                survivor.alive = day.alive;
                survivor.morale = day.morale;
                survivor.hunger = day.hunger;
                survivor.thirst = day.thirst;
                survivor.opinion = day.opinion;
                survivor.injury = day.injury;
            }
        }

        private void Spend(int foodLeft, int waterLeft)
        {
            var storage = ColonyStorage.Instance;
            if (storage == null) return;
            int foodDelta = storage.Food - foodLeft;
            int waterDelta = storage.Water - waterLeft;
            if (foodDelta > 0) storage.AddFood(-foodDelta);
            if (waterDelta > 0) storage.AddWater(-waterDelta);
        }

        private void Publish(string[] notes)
        {
            DayNotes = notes == null || notes.Length == 0 ? "" : string.Join(", ", notes);
            OnRosterChanged?.Invoke();
            if (DayNotes.Length > 0) GameplayFeedback.Toast(DayNotes);
        }

        public float AverageMorale()
        {
            float sum = 0f;
            int count = 0;
            foreach (var survivor in survivors)
            {
                if (!survivor.alive) continue;
                sum += survivor.morale;
                count++;
            }
            return count == 0 ? 0f : sum / count;
        }

        public Survivor Find(string id)
        {
            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i].id == id) return survivors[i];
            }
            return null;
        }

        public bool Has(string id) => Find(id) != null;

        public bool Adopt(string id, string name, string trait)
        {
            if (string.IsNullOrEmpty(id) || Has(id)) return false;
            if (survivors.Count >= RescueBook.RosterCap) return false;
            survivors.Add(Make(id, name, trait, false, "Found on the street"));
            OnRosterChanged?.Invoke();
            return true;
        }

        public void Replace(List<Survivor> loaded)
        {
            survivors = loaded ?? new List<Survivor>();
            if (survivors.Count == 0) Seed();
            OnRosterChanged?.Invoke();
        }

        public void ResetRoster() => Seed();

        private Survivor NextLiving()
        {
            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i].alive) return survivors[i];
            }
            return null;
        }

        private static void SpawnCorpse(Vector3 position, int index, string gear)
        {
            var corpse = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            corpse.name = "Corpse_Leader";
            corpse.transform.position = position + Vector3.up * 0.2f;
            corpse.transform.localScale = new Vector3(0.6f, 0.35f, 0.6f);
            corpse.layer = GameLayers.Interactable;
            corpse.AddComponent<FallenGear>().Configure(index, gear);
        }
    }
}
