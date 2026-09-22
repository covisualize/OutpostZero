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

        public int Scrap => scrap;
        public int Food => food;
        public int Water => water;
        public int Security => security;
        public int Cloth => cloth;
        public int Chemicals => chemicals;
        public int Tape => tape;
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

        public void AddScrap(int amount) => Change(ref scrap, amount);
        public void AddFood(int amount) => Change(ref food, amount);
        public void AddWater(int amount) => Change(ref water, amount);
        public void AddSecurity(int amount) => Change(ref security, amount);
        public void AddCloth(int amount) => Change(ref cloth, amount);
        public void AddChemicals(int amount) => Change(ref chemicals, amount);
        public void AddTape(int amount) => Change(ref tape, amount);

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
            OnStorageChanged?.Invoke();
        }

        private void Change(ref int field, int amount)
        {
            field = Mathf.Max(0, field + amount);
            OnStorageChanged?.Invoke();
        }
    }
}
