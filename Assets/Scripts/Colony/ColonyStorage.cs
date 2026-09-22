using System;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Colony
{
    public class ColonyStorage : MonoBehaviour
    {
        public static ColonyStorage Instance { get; private set; }

        [SerializeField] private int scrap;
        [SerializeField] private int food;
        [SerializeField] private int water;
        [SerializeField] private int security;
        [SerializeField] private int cloth;
        [SerializeField] private int chemicals;
        [SerializeField] private int tape;
        [SerializeField] private int raw;
        [SerializeField] private int bodies;

        public int Scrap => scrap;
        public int Food => food;
        public int Water => water;
        public int Security => security;
        public int Cloth => cloth;
        public int Chemicals => chemicals;
        public int Tape => tape;
        public int Raw => raw;
        public int Bodies => bodies;
        public int Used => CampRoom.Bulk(scrap, food, water, cloth, chemicals, tape, raw);
        public int Room => CampRoom.Room(GridBuilder.Instance != null ? GridBuilder.Instance.CountKind("Crate") : 0);
        public event Action OnStorageChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        public int AddScrap(int amount) => Admit(ref scrap, amount, CampRoom.Scrap);
        public int AddFood(int amount) => Admit(ref food, amount, CampRoom.Food);
        public int AddWater(int amount) => Admit(ref water, amount, CampRoom.Water);
        public void AddSecurity(int amount) => Shift(ref security, amount);
        public int AddCloth(int amount) => Admit(ref cloth, amount, CampRoom.Cloth);
        public int AddChemicals(int amount) => Admit(ref chemicals, amount, CampRoom.Chemicals);
        public int AddTape(int amount) => Admit(ref tape, amount, CampRoom.Tape);
        public int AddRaw(int amount) => Admit(ref raw, amount, CampRoom.Raw);

        public int TakeRaw(int amount)
        {
            if (amount <= 0 || raw <= 0) return 0;
            int taken = amount < raw ? amount : raw;
            raw -= taken;
            OnStorageChanged?.Invoke();
            return taken;
        }

        public void RestoreScrap(int amount) => Shift(ref scrap, amount);
        public void RestoreCloth(int amount) => Shift(ref cloth, amount);
        public void RestoreChemicals(int amount) => Shift(ref chemicals, amount);
        public void RestoreTape(int amount) => Shift(ref tape, amount);

        public bool TrySpendBill(int scrapDue, int clothNeed, int chemicalNeed, int tapeNeed)
        {
            if (scrapDue < 0 || clothNeed < 0 || chemicalNeed < 0 || tapeNeed < 0) return false;
            if (scrap < scrapDue || cloth < clothNeed || chemicals < chemicalNeed || tape < tapeNeed) return false;
            scrap -= scrapDue;
            cloth -= clothNeed;
            chemicals -= chemicalNeed;
            tape -= tapeNeed;
            OnStorageChanged?.Invoke();
            return true;
        }

        public void SetSupplies(int nextCloth, int nextChemicals, int nextTape)
        {
            cloth = Mathf.Max(0, nextCloth);
            chemicals = Mathf.Max(0, nextChemicals);
            tape = Mathf.Max(0, nextTape);
            OnStorageChanged?.Invoke();
        }

        public void SetRaw(int nextRaw)
        {
            raw = Mathf.Max(0, nextRaw);
            OnStorageChanged?.Invoke();
        }

        public void AddBodies(int amount)
        {
            if (amount <= 0) return;
            bodies += amount;
            OnStorageChanged?.Invoke();
        }

        public int TakeBodies(int amount)
        {
            if (amount <= 0 || bodies <= 0) return 0;
            int taken = amount < bodies ? amount : bodies;
            bodies -= taken;
            OnStorageChanged?.Invoke();
            return taken;
        }

        public void SetBodies(int nextBodies)
        {
            bodies = Mathf.Max(0, nextBodies);
            OnStorageChanged?.Invoke();
        }

        public bool TrySpendScrap(int amount)
        {
            if (scrap < amount) return false;
            scrap -= amount;
            OnStorageChanged?.Invoke();
            return true;
        }

        public void Set(int nextScrap, int nextFood, int nextWater)
        {
            scrap = Mathf.Max(0, nextScrap);
            food = Mathf.Max(0, nextFood);
            water = Mathf.Max(0, nextWater);
            OnStorageChanged?.Invoke();
        }

        public void ResetStores()
        {
            scrap = 0;
            food = 0;
            water = 0;
            security = 0;
            cloth = 0;
            chemicals = 0;
            tape = 0;
            raw = 0;
            bodies = 0;
            OnStorageChanged?.Invoke();
        }

        private int Admit(ref int field, int amount, int unit)
        {
            if (amount <= 0)
            {
                Shift(ref field, amount);
                return 0;
            }
            int take = CampRoom.Fit(Used, unit, amount, Room);
            if (take < amount) GameplayFeedback.Toast("Stores are full");
            if (take <= 0) return 0;
            field += take;
            OnStorageChanged?.Invoke();
            return take;
        }

        private void Shift(ref int field, int amount)
        {
            field = Mathf.Max(0, field + amount);
            OnStorageChanged?.Invoke();
        }
    }
}
