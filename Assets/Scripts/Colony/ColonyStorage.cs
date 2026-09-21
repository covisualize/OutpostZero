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

        public int Scrap => scrap;
        public int Food => food;
        public int Water => water;
        public int Security => security;
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
            OnStorageChanged?.Invoke();
        }

        private void Change(ref int field, int amount)
        {
            field = Mathf.Max(0, field + amount);
            OnStorageChanged?.Invoke();
        }
    }
}
