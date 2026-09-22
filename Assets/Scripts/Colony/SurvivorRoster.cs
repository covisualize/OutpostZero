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
        public string aside = "";
        public string mark = "";
        public bool alive = true;
        public bool leader;
        public float morale = 70f;
        public float hunger = 78f;
        public float thirst = 78f;
        public int opinion = 18;
        public int injury;
        public int combat;
        public int medicine;
        public int engineering;
        public int cooking;
        public int scavenge;
        public int leadership;
        public string task = "Rest";
        public string bond = "";
        public string kin = "";
        public int age;
        public string past = "";
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

        public static int LeaderPractice(string task)
        {
            var leader = Instance != null ? Instance.Leader : null;
            if (leader == null) return 0;
            if (task == "Guard") return leader.combat;
            if (task == "Medic") return leader.medicine;
            if (task == "Scavenge") return leader.scavenge;
            return 0;
        }

        public static string LeaderTrait()
        {
            var leader = Instance != null ? Instance.Leader : null;
            if (leader == null || string.IsNullOrEmpty(leader.trait)) return "";
            return leader.trait;
        }

        public static string LeaderAside()
        {
            var leader = Instance != null ? Instance.Leader : null;
            if (leader == null || string.IsNullOrEmpty(leader.aside)) return "";
            return leader.aside;
        }

        public static string LeaderMark()
        {
            var leader = Instance != null ? Instance.Leader : null;
            if (leader == null || string.IsNullOrEmpty(leader.mark)) return "";
            return leader.mark;
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
            int seed = WorldMapService.Instance != null ? WorldMapService.Instance.WorldSeed : 0;
            Seed(seed);
        }

        public void Seed(int seed)
        {
            memorials.Clear();
            corpses.Clear();
            survivors.Clear();
            var drafts = SurvivorDraw.Open(seed);
            for (int i = 0; i < drafts.Length; i++) survivors.Add(Make(drafts[i]));
            OnRosterChanged?.Invoke();
        }

        private static Survivor Make(SurvivorDraw.Draft draft)
        {
            return new Survivor
            {
                id = draft.Id,
                displayName = draft.Name,
                trait = draft.Trait,
                aside = draft.Aside ?? "",
                mark = draft.Mark ?? "",
                leader = draft.Leader,
                morale = 72f,
                hunger = 78f,
                thirst = 78f,
                opinion = string.IsNullOrEmpty(draft.Bond) ? 0 : 18,
                combat = draft.Combat,
                medicine = draft.Medicine,
                engineering = draft.Engineering,
                cooking = draft.Cooking,
                scavenge = draft.Scavenge,
                task = draft.Leader ? "Lead" : "Rest",
                bond = draft.Bond,
                age = LifeLine.YearsOf(draft.Id),
                past = LifeLine.Past(draft.Id)
            };
        }

        private static Survivor Make(string id, string name, string trait, bool leader, string bond)
        {
            return Make(new SurvivorDraw.Draft
            {
                Id = id,
                Name = name,
                Trait = trait,
                Leader = leader,
                Bond = bond
            });
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
            Survivor next = string.IsNullOrEmpty(id) ? ChooseHeir() : Find(id);
            if (next == null || !next.alive) next = ChooseHeir();
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
                bool remains = MarkLeaderDead(new Vector3(-6f, 0f, -8f), "raid");
                if (!remains) EndCamp();
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
            if (CampEnd.Wiped(LivingCount())) EndCamp();
        }

        public int LivingCount()
        {
            int count = 0;
            for (int i = 0; i < survivors.Count; i++)
            {
                if (survivors[i].alive) count++;
            }
            return count;
        }

        private void EndCamp()
        {
            GameplayFeedback.Toast(Loc.T("camp.wiped"));
            GameManager.Instance?.SetState(GameState.GameOver);
            SaveSystem.Instance?.Save(false);
        }

        public string PostRaid(bool bodies, bool damaged)
        {
            int count = survivors.Count;
            var tasks = new string[count];
            var alive = new bool[count];
            var leader = new bool[count];
            for (int i = 0; i < count; i++)
            {
                tasks[i] = survivors[i].task;
                alive[i] = survivors[i].alive;
                leader[i] = survivors[i].leader;
            }
            int flags = MorningBoard.Apply(tasks, alive, leader, bodies, damaged);
            if (flags == 0) return "";
            for (int i = 0; i < count; i++) survivors[i].task = tasks[i];
            OnRosterChanged?.Invoke();
            return MorningBoard.Key(flags);
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
                        bool scrounge = TraitHook.Holds(survivor.trait, survivor.aside, survivor.mark, "Scrounger");
                        int scrap = Pay(4 + (scrounge ? 3 : 0), survivor.morale, survivor.trait, survivor.aside, survivor.mark);
                        if (scrap > 0)
                        {
                            survivor.scavenge = Practice.Gain(survivor.scavenge);
                            scrap += Practice.Bonus(survivor.scavenge);
                            scrap += ScrapDepth.Extra(survivor.scavenge);
                        }
                        if (scrap > 0 && storage != null) storage.AddScrap(scrap);
                        if (scrap > 0) survivor.morale = Mathf.Max(0f, survivor.morale - 4f);
                        if (storage != null)
                        {
                            CraftBill.Salvage(day * 17 + index, scrounge, out int cloth, out int chemicals, out int tape);
                            storage.AddCloth(cloth);
                            if (chemicals > 0) storage.AddChemicals(chemicals);
                            if (tape > 0) storage.AddTape(tape);
                            if (scrap > 0) storage.AddRaw(1);
                            if (scrap > 0) storage.AddRounds(GuardVolley.Brought(scrounge));
                            if (scrap > 0 && HaulCell.Due(day * 17 + index, survivor.scavenge)) storage.AddCells(1);
                        }
                        break;
                    case "Cook":
                        int hands = Pay(2, survivor.morale, survivor.trait, survivor.aside, survivor.mark);
                        bool fire = GridBuilder.Instance != null && GridBuilder.Instance.HasKind("Campfire");
                        int raw = storage != null ? storage.Raw : 0;
                        CookPot.Serve(fire, raw, hands, out int spent, out int served, out int lift);
                        if (hands > 0)
                        {
                            survivor.cooking = Practice.Gain(survivor.cooking);
                            served += Practice.Bonus(survivor.cooking);
                            if (spent > 0) served += PotDepth.Plate(survivor.cooking);
                        }
                        if (spent > 0 && storage != null) storage.TakeRaw(spent);
                        if (served > 0 && storage != null) storage.AddFood(served);
                        if (lift > 0) survivor.morale = Mathf.Min(100f, survivor.morale + lift + TraitHook.CookPlate(survivor.trait, survivor.aside, survivor.mark, spent > 0));
                        break;
                    case "Guard":
                        int watch = Pay(1, survivor.morale, survivor.trait, survivor.aside, survivor.mark);
                        if (watch > 0)
                        {
                            survivor.combat = Practice.Gain(survivor.combat);
                            watch += Practice.Bonus(survivor.combat);
                            watch += GuardDepth.Post(survivor.combat);
                            survivor.morale = Mathf.Max(0f, survivor.morale - TraitHook.WatchCost(survivor.trait, survivor.aside, survivor.mark));
                        }
                        watch = TraitHook.WatchPay(survivor.trait, survivor.aside, survivor.mark, watch);
                        if (watch > 0 && storage != null) storage.AddSecurity(watch);
                        break;
                    case "Rest":
                        survivor.morale = Mathf.Min(100f, survivor.morale + TraitHook.RestGain(survivor.trait, survivor.aside, survivor.mark, LifeLine.Rest(survivor.age)));
                        break;
                    case "Medic":
                        if (ColonyDay.OutputScale(survivor.morale, survivor.trait, survivor.aside, survivor.mark) <= 0f) break;
                        survivor.medicine = Practice.Gain(survivor.medicine);
                        survivor.morale = Mathf.Min(100f, survivor.morale + 2f);
                        var leader = PlayerRegistry.Current;
                        leader?.GetComponent<Combat.HealthSystem>()?.Heal(12f + Practice.Bonus(survivor.medicine) * 6f + MedDepth.Mend(survivor.medicine));
                        leader?.GetComponent<StatusEffectController>()?.ClearInjury();
                        for (int i = 0; i < survivors.Count; i++)
                        {
                            if (survivors[i].alive && survivors[i].injury > 0) survivors[i].injury--;
                        }
                        break;
                    case "Build":
                        int pace = BuildSite.Shift(survivor.trait, survivor.morale);
                        int asidePace = BuildSite.Shift(survivor.aside, survivor.morale);
                        int markPace = BuildSite.Shift(survivor.mark, survivor.morale);
                        if (asidePace > pace) pace = asidePace;
                        if (markPace > pace) pace = markPace;
                        if (pace > 0)
                        {
                            survivor.engineering = Practice.Gain(survivor.engineering);
                            pace += Practice.Bonus(survivor.engineering);
                            pace += BuildDepth.Raise(survivor.engineering);
                        }
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
            var held = Leader;
            if (held != null) held.leadership = Practice.Gain(held.leadership);
            FactionTrade.Instance?.OnMorning(WorldClock.Instance != null ? WorldClock.Instance.Day : 1);
            AudioManager.Instance?.Sting("dawn");
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
            return Pay(amount, morale, null);
        }

        private static int Pay(int amount, float morale, string trait)
        {
            return Pay(amount, morale, trait, null);
        }

        private static int Pay(int amount, float morale, string trait, string aside)
        {
            return Pay(amount, morale, trait, aside, null);
        }

        private static int Pay(int amount, float morale, string trait, string aside, string mark)
        {
            float scale = ColonyDay.OutputScale(morale, trait, aside, mark);
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
                    aside = survivor.aside,
                    mark = survivor.mark,
                    task = survivor.task,
                    bond = survivor.bond,
                    kin = survivor.kin ?? "",
                    name = survivor.displayName,
                    alive = survivor.alive,
                    leader = survivor.leader,
                    morale = survivor.morale,
                    hunger = survivor.hunger,
                    thirst = survivor.thirst,
                    opinion = survivor.opinion,
                    injury = survivor.injury,
                    leadership = survivor.leadership
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
                survivor.kin = day.kin ?? "";
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

        public void ResetRoster(int seed) => Seed(seed);

        private Survivor ChooseHeir()
        {
            int count = survivors.Count;
            var alive = new bool[count];
            var injury = new int[count];
            var leadership = new int[count];
            var morale = new float[count];
            for (int i = 0; i < count; i++)
            {
                alive[i] = survivors[i].alive;
                injury[i] = survivors[i].injury;
                leadership[i] = survivors[i].leadership;
                morale[i] = survivors[i].morale;
            }
            int index = Heir.Pick(alive, injury, leadership, morale);
            if (index < 0 || index >= count) return NextLiving();
            return survivors[index];
        }

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
