using System;
using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;

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
        public string task = "Rest";
    }

    public class SurvivorRoster : MonoBehaviour
    {
        public static SurvivorRoster Instance { get; private set; }

        [SerializeField] private List<Survivor> survivors = new List<Survivor>();
        public IReadOnlyList<Survivor> Survivors => survivors;
        public event Action OnRosterChanged;

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
            survivors.Clear();
            survivors.Add(Make("mara", "Mara Quill", "Steady Hands", true));
            survivors.Add(Make("jonas", "Jonas Reed", "Light Sleeper", false));
            survivors.Add(Make("priya", "Priya Sen", "Field Medic", false));
            survivors.Add(Make("ellis", "Ellis Ward", "Scrounger", false));
            OnRosterChanged?.Invoke();
        }

        private static Survivor Make(string id, string name, string trait, bool leader)
        {
            return new Survivor { id = id, displayName = name, trait = trait, leader = leader, morale = 72f, task = "Rest" };
        }

        public bool MarkLeaderDead(Vector3 corpsePosition)
        {
            var leader = Leader;
            if (leader != null)
            {
                leader.alive = false;
                leader.leader = false;
                leader.task = "Fallen";
            }
            foreach (var survivor in survivors)
            {
                if (survivor.alive) survivor.morale = Mathf.Max(0f, survivor.morale - 14f);
            }
            SpawnCorpse(corpsePosition);
            OnRosterChanged?.Invoke();
            return NextLiving() != null;
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
            foreach (var survivor in survivors)
            {
                if (!survivor.alive) continue;
                switch (survivor.task)
                {
                    case "Scavenge":
                        if (storage != null) storage.AddScrap(4 + (survivor.trait == "Scrounger" ? 3 : 0));
                        survivor.morale = Mathf.Max(0f, survivor.morale - 4f);
                        break;
                    case "Cook":
                        if (storage != null) storage.AddFood(2);
                        survivor.morale = Mathf.Min(100f, survivor.morale + 3f);
                        break;
                    case "Guard":
                        survivor.morale = Mathf.Max(0f, survivor.morale - 2f);
                        if (storage != null) storage.AddSecurity(1);
                        break;
                    case "Rest":
                        survivor.morale = Mathf.Min(100f, survivor.morale + 8f);
                        break;
                    case "Medic":
                        survivor.morale = Mathf.Min(100f, survivor.morale + 2f);
                        break;
                }
            }
            OnRosterChanged?.Invoke();
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

        private static void SpawnCorpse(Vector3 position)
        {
            var corpse = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            corpse.name = "Corpse_Leader";
            corpse.transform.position = position + Vector3.up * 0.2f;
            corpse.transform.localScale = new Vector3(0.6f, 0.35f, 0.6f);
            var container = corpse.AddComponent<LootContainer>();
            container.Configure("crate");
        }
    }
}
