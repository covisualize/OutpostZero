using System;
using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;

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
        private readonly string[] belt = new[] { "", "", "", "" };

        public float CurrentWeight => currentWeight;
        public float MaxWeightCapacity => maxWeightCapacity;
        public float WeightRatio => currentWeight / Mathf.Max(0.01f, maxWeightCapacity);
        public int ScrapCount => scrapCount;
        public int MedicalKits => medicalKits;
        public IReadOnlyList<InventoryItem> Items => items;
        public string BeltLine => ItemBelt.Line(belt);

        public event Action OnInventoryChanged;

        public string BeltMark(string id) => ItemBelt.Mark(belt, id);

        public bool ToggleBelt(string id)
        {
            bool occupied = ItemBelt.Mark(belt, id) != "";
            int slot = ItemBelt.Toggle(belt, id);
            if (slot < 0)
            {
                if (slot == -2) GameplayFeedback.Toast("Belt is full");
                return false;
            }
            GameplayFeedback.Toast(occupied ? "Cleared belt" : "Belt " + (slot + 5));
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool UseBelt(int index)
        {
            string id = ItemBelt.IdAt(belt, index);
            if (string.IsNullOrEmpty(id)) return false;
            bool used = TryUse(id);
            if (used && id == "medkit") GameplayFeedback.Toast("Medkit used  +50 HP");
            if (!StillCarrying(id)) belt[index] = "";
            return used;
        }

        private bool StillCarrying(string id)
        {
            if (id == "medkit") return medicalKits > 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].ItemId == id && items[i].Quantity > 0) return true;
            }
            return false;
        }

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
            if (WeightRatio >= 0.8f) OutpostZero.Shell.CodexDirector.Hear("weight");
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
            if (record.Id == "antibiotics")
            {
                var fever = GetComponent<StatusEffectController>();
                if (fever == null || !Affliction.AntibioticsWork(fever.InfectionStage))
                {
                    GameplayFeedback.Toast("Antibiotics won't help");
                    return false;
                }
                if (!TryConsume(id)) return false;
                fever.CureInfection();
                GameplayFeedback.Toast("The fever breaks");
                return true;
            }
            if (record.Id == "painkillers")
            {
                if (!TryConsume(id)) return false;
                GetComponent<StatusEffectController>()?.ApplyPainkiller();
                GameplayFeedback.Toast("Painkillers");
                return true;
            }
            if (!TryConsume(id)) return false;
            if (record.Id == "bandage") GetComponent<StatusEffectController>()?.StopBleed();
            if (record.Heal > 0) GetComponent<Combat.HealthSystem>()?.Heal(record.Heal);
            var needs = GetComponent<SurvivalNeeds>();
            if (record.Hunger > 0f) needs?.Eat(record.Hunger);
            if (record.Thirst > 0f) needs?.Drink(record.Thirst);
            GameplayFeedback.Toast("Used " + record.DisplayName);
            return true;
        }

        public void DepositScrapToColony()
        {
            var storage = OutpostZero.Colony.ColonyStorage.Instance;
            if (storage == null) return;
            bool moved = false;
            if (scrapCount > 0)
            {
                storage.AddScrap(scrapCount);
                scrapCount = 0;
                moved = true;
            }
            if (DepositMaterial(storage, "cloth")) moved = true;
            if (DepositMaterial(storage, "chemicals")) moved = true;
            if (DepositMaterial(storage, "tape")) moved = true;
            if (!moved) return;
            RecalculateWeight();
            OnInventoryChanged?.Invoke();
        }

        private bool DepositMaterial(OutpostZero.Colony.ColonyStorage storage, string id)
        {
            var existing = items.Find(item => item.ItemId == id);
            if (existing == null || existing.Quantity <= 0) return false;
            int count = existing.Quantity;
            items.Remove(existing);
            if (id == "cloth") storage.AddCloth(count);
            else if (id == "chemicals") storage.AddChemicals(count);
            else storage.AddTape(count);
            return true;
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

        public string TakeGear()
        {
            var parts = new List<string>();
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null || items[i].Quantity <= 0 || string.IsNullOrEmpty(items[i].ItemId)) continue;
                parts.Add(items[i].ItemId + "*" + items[i].Quantity);
            }
            if (scrapCount > 0) parts.Add("scrap*" + scrapCount);
            if (medicalKits > 0) parts.Add("medkit*" + medicalKits);
            items.Clear();
            scrapCount = 0;
            medicalKits = 0;
            currentWeight = 0f;
            OnInventoryChanged?.Invoke();
            return string.Join("+", parts);
        }

        public void RestoreGear(string packed)
        {
            if (string.IsNullOrEmpty(packed)) return;
            string[] parts = packed.Split('+');
            for (int i = 0; i < parts.Length; i++)
            {
                int star = parts[i].IndexOf('*');
                if (star <= 0) continue;
                string id = parts[i].Substring(0, star);
                if (!int.TryParse(parts[i].Substring(star + 1), out int count) || count <= 0) continue;
                if (id == "scrap")
                {
                    scrapCount += count;
                    continue;
                }
                if (id == "medkit")
                {
                    medicalKits += count;
                    continue;
                }
                var record = OutpostZero.Items.ItemCatalog.Find(id);
                if (record == null) continue;
                TryAddItem(record.Id, record.DisplayName, record.Category, count, record.Weight);
            }
            RecalculateWeight();
            OnInventoryChanged?.Invoke();
        }

        public void AddScrap(int amount)
        {
            scrapCount += amount;
            if (scrapCount < 0) scrapCount = 0;
            if (amount > 0) OutpostZero.Shell.CodexDirector.Hear("loot");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScrap(amount);
            }
            RecalculateWeight();
            OnInventoryChanged?.Invoke();
        }

        public bool Drop(string id, int count)
        {
            if (count <= 0 || string.IsNullOrEmpty(id)) return false;
            var existing = items.Find(item => item.ItemId == id);
            if (existing == null) return false;
            int drop = count < existing.Quantity ? count : existing.Quantity;
            if (!TryConsume(id, drop)) return false;
            SpawnDrop(id, existing.ItemName, drop);
            return true;
        }

        public bool DropHalf(string id)
        {
            var existing = items.Find(item => item.ItemId == id);
            if (existing == null) return false;
            int half = PackOps.SplitOff(existing.Quantity);
            if (half <= 0) return false;
            return Drop(id, half);
        }

        private void SpawnDrop(string id, string name, int count)
        {
            var drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            drop.name = "Dropped_" + id;
            drop.transform.position = transform.position + transform.forward * 0.8f + Vector3.up * 0.25f;
            drop.transform.localScale = new Vector3(0.28f, 0.18f, 0.28f);
            drop.layer = GameLayers.Interactable;
            drop.AddComponent<WorldItem>().Configure(id, count);
            GameplayFeedback.Toast("Dropped " + (string.IsNullOrEmpty(name) ? id : name));
        }

        public bool TryConsume(string id, int count)
        {
            if (count <= 0 || string.IsNullOrEmpty(id)) return false;
            var existing = items.Find(item => item.ItemId == id);
            if (existing == null || existing.Quantity < count) return false;
            existing.Quantity -= count;
            if (existing.Quantity <= 0) items.Remove(existing);
            RecalculateWeight();
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool TrySpendMedical(int count)
        {
            if (count <= 0 || medicalKits < count) return false;
            medicalKits -= count;
            RecalculateWeight();
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool UseMedkit()
        {
            if (medicalKits <= 0) return false;

            var health = GetComponent<Combat.HealthSystem>();
            var effects = GetComponent<StatusEffectController>();
            bool wounded = effects != null && (effects.IsBleeding || effects.IsInfected);
            if (health != null && health.CurrentHealth >= health.MaxHealth && !wounded) return false;

            medicalKits--;
            health?.Heal(50f);
            effects?.StopBleed();
            effects?.CureInfection();
            RecalculateWeight();
            OnInventoryChanged?.Invoke();
            return true;
        }

        private void RecalculateWeight()
        {
            float w = 0f;
            foreach (var item in items)
            {
                w += item.TotalWeight;
            }
            w = PackOps.Weight(w, scrapCount, medicalKits);
            currentWeight = w;
        }
    }
}
