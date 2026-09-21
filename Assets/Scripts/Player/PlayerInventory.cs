using System;
using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Player
{
    [System.Serializable]
    public class InventoryItem
    {
        public string ItemId;
        public string ItemName;
        public ItemCategory Category;
        public int Quantity;
        public float WeightPerUnit;
        public Sprite Icon;

        public float TotalWeight => Quantity * WeightPerUnit;
    }

    public class PlayerInventory : MonoBehaviour
    {
        [Header("Backpack Limits")]
        [SerializeField] private float maxWeightCapacity = 35f; // kg
        [SerializeField] private float currentWeight = 0f;

        [Header("Quick Ammo Stores")]
        [SerializeField] private int pistolAmmo = 45;
        [SerializeField] private int shotgunAmmo = 18;
        [SerializeField] private int rifleAmmo = 90;

        [Header("Scavenged Scrap & Supplies")]
        [SerializeField] private int scrapCount = 0;
        [SerializeField] private int medicalKits = 2;

        [Header("Item Bag")]
        [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

        public float CurrentWeight => currentWeight;
        public float MaxWeightCapacity => maxWeightCapacity;
        public int ScrapCount => scrapCount;
        public int MedicalKits => medicalKits;
        public IReadOnlyList<InventoryItem> Items => items;

        public event Action OnInventoryChanged;

        private void Start()
        {
            RecalculateWeight();
        }

        public bool TryAddItem(string id, string name, ItemCategory category, int count, float unitWeight)
        {
            float addedWeight = count * unitWeight;
            if (currentWeight + addedWeight > maxWeightCapacity)
            {
                Debug.LogWarning($"[PlayerInventory] Cannot add {name} - Exceeds weight capacity!");
                return false;
            }

            var existing = items.Find(i => i.ItemId == id);
            if (existing != null)
            {
                existing.Quantity += count;
            }
            else
            {
                items.Add(new InventoryItem
                {
                    ItemId = id,
                    ItemName = name,
                    Category = category,
                    Quantity = count,
                    WeightPerUnit = unitWeight
                });
            }

            RecalculateWeight();
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void AddScrap(int amount)
        {
            scrapCount += amount;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScrap(amount);
            }
            OnInventoryChanged?.Invoke();
        }

        public bool UseMedkit()
        {
            if (medicalKits <= 0) return false;

            var health = GetComponent<Combat.HealthSystem>();
            if (health != null && health.CurrentHealth < health.MaxHealth)
            {
                medicalKits--;
                health.Heal(50f);
                OnInventoryChanged?.Invoke();
                return true;
            }

            return false;
        }

        private void RecalculateWeight()
        {
            float w = 0f;
            foreach (var item in items)
            {
                w += item.TotalWeight;
            }
            w += (scrapCount * 0.1f);
            w += (medicalKits * 0.5f);
            currentWeight = w;
        }
    }
}
