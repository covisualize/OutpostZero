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
        public float WeightRatio => currentWeight / Mathf.Max(0.01f, maxWeightCapacity);
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

        public void AddMedicalKits(int amount)
        {
            if (amount <= 0) return;
            medicalKits += amount;
            RecalculateWeight();
            OnInventoryChanged?.Invoke();
        }

        public bool TryCollect(LootKind kind, int amount)
        {
            switch (kind)
            {
                case LootKind.Medkit:
                    AddMedicalKits(amount);
                    return true;
                case LootKind.Ammo9mm:
                    return GrantAmmo(WeaponType.Pistol, amount);
                case LootKind.AmmoShotgun:
                    return GrantAmmo(WeaponType.Shotgun, amount);
                case LootKind.Scrap:
                    AddScrap(amount);
                    return true;
                default:
                    return false;
            }
        }

        public bool GrantAmmoPublic(WeaponType weaponType, int amount) => GrantAmmo(weaponType, amount);

        public bool TryConsume(string id)
        {
            var existing = items.Find(i => i.ItemId == id && i.Quantity > 0);
            if (existing == null) return false;
            existing.Quantity--;
            if (existing.Quantity <= 0) items.Remove(existing);
            RecalculateWeight();
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool TryUse(string id)
        {
            var record = OutpostZero.Items.ItemCatalog.Find(id);
            if (record == null) return false;
            if (record.Use == OutpostZero.Items.ItemUse.Ammo) return false;
            if (record.Id == "medkit") return UseMedkit();
            if (!TryConsume(id)) return false;
            if (record.Heal > 0) GetComponent<Combat.HealthSystem>()?.Heal(record.Heal);
            var needs = GetComponent<SurvivalNeeds>();
            if (record.Hunger > 0f) needs?.Eat(record.Hunger);
            if (record.Thirst > 0f) needs?.Drink(record.Thirst);
            if (record.Id == "bandage") GetComponent<StatusEffectController>()?.ClearInjury();
            GameplayFeedback.Toast("Used " + record.DisplayName);
            return true;
        }

        public void DepositScrapToColony()
        {
            if (scrapCount <= 0) return;
            OutpostZero.Colony.ColonyStorage.Instance?.AddScrap(scrapCount);
            scrapCount = 0;
            RecalculateWeight();
            OnInventoryChanged?.Invoke();
        }

        private bool GrantAmmo(WeaponType weaponType, int amount)
        {
            var guns = GetComponentsInChildren<Combat.FirearmWeapon>(true);
            bool granted = false;
            foreach (var gun in guns)
            {
                if (gun.Type != weaponType) continue;
                gun.AddReserveAmmo(amount);
                granted = true;
            }
            return granted;
        }

        public void AddScrap(int amount)
        {
            scrapCount += amount;
            if (amount > 0) OutpostZero.Shell.TutorialDirector.Instance?.Note("loot");
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
