using System;
using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Colony;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Shell;

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
        [SerializeField] private int packTier = 1;
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
        public int PackTier => PackOps.Tier(packTier);
        public float MaxWeightCapacity => PackOps.Limit(packTier);
        public float WeightRatio => currentWeight / Mathf.Max(0.01f, MaxWeightCapacity);
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
                if (slot == -2) GameplayFeedback.Toast(PackSay.Full(null));
                return false;
            }
            GameplayFeedback.Toast(occupied ? PackSay.Clear(null) : PackSay.Slot(slot + 5, null));
            OnInventoryChanged?.Invoke();
            return true;
        }

        public bool UseBelt(int index)
        {
            string id = ItemBelt.IdAt(belt, index);
            if (string.IsNullOrEmpty(id)) return false;
            bool used = TryUse(id);
            if (used && id == "medkit") GameplayFeedback.Toast(FieldHand.Dose(LastDoseSkill, null));
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
            maxWeightCapacity = MaxWeightCapacity;
            RecalculateWeight();
        }

        public void SetPackTier(int tier)
        {
            packTier = PackOps.Tier(tier);
            maxWeightCapacity = MaxWeightCapacity;
            OnInventoryChanged?.Invoke();
        }

        public bool TryRaisePack(int benchTier, OutpostZero.Colony.ColonyStorage storage)
        {
            if (storage == null) return false;
            if (!PackOps.CanRaise(packTier, benchTier, storage.Scrap, storage.Cloth, storage.Tape)) return false;
            if (!storage.TrySpendBill(PackOps.RaiseScrap, PackOps.RaiseCloth, 0, PackOps.RaiseTape)) return false;
            SetPackTier(2);
            GameplayFeedback.Toast(OutpostZero.Shell.Loc.T("camp.pack_t2"));
            return true;
        }

        public bool TryAddItem(string id, string name, ItemCategory category, int count, float unitWeight)
        {
            float addedWeight = count * unitWeight;
            if (!PackOps.Fits(currentWeight, MaxWeightCapacity, addedWeight))
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
            if (TossKind.Of(id) != TossKind.None) OutpostZero.Shell.CodexDirector.Hear("throwable");
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
                bool street = fever != null && Affliction.AntibioticsWork(fever.InfectionStage);
                var leader = SurvivorRoster.Instance != null ? SurvivorRoster.Instance.Leader : null;
                bool camp = leader != null && WoundEase.Helps(leader.injury);
                if (!street && !camp)
                {
                    GameplayFeedback.Toast(FieldHand.Fail(null));
                    return false;
                }
                if (!TryConsume(id)) return false;
                if (street) fever.CureInfection();
                bool leaderEased = SurvivorRoster.Instance != null && SurvivorRoster.Instance.EaseLeader();
                if (leaderEased && !street) GameplayFeedback.Toast(WoundEase.Line(null));
                else GameplayFeedback.Toast(WoundEase.Note(FieldHand.Breaks(null), leaderEased, null));
                return true;
            }
            if (record.Id == "painkillers")
            {
                if (!TryConsume(id)) return false;
                GetComponent<StatusEffectController>()?.ApplyPainkiller();
                GameplayFeedback.Toast(FieldHand.Relief(record.Id, null));
                return true;
            }
            if (TossKind.Throws(record.Id))
            {
                var hands = GetComponent<PlayerInteractor>();
                if (hands == null || !hands.ThrowId(record.Id))
                {
                    GameplayFeedback.Toast(Loc.T("toss.none"));
                    return false;
                }
                return true;
            }
            if (record.Id == "cell")
            {
                var body = GetComponent<PlayerController>();
                if (body == null || LampCell.Tops(body.LampCellCharge))
                {
                    GameplayFeedback.Toast(Loc.T("lamp.full"));
                    return false;
                }
                if (!TryConsume(id)) return false;
                body.AddLamp(LampCell.Pack);
                GameplayFeedback.Toast(Loc.T("lamp.fit"));
                return true;
            }
            if (!TryConsume(id)) return false;
            bool eased = false;
            if (record.Id == "bandage")
            {
                GetComponent<StatusEffectController>()?.StopBleed();
                eased = SurvivorRoster.Instance != null && SurvivorRoster.Instance.EaseLeader();
            }
            if (record.Heal > 0) GetComponent<Combat.HealthSystem>()?.Heal(record.Heal);
            var needs = GetComponent<SurvivalNeeds>();
            if (record.Hunger > 0f) needs?.Eat(record.Hunger);
            if (record.Thirst > 0f) needs?.Drink(record.Thirst);
            if (RationNoise.Calls(record.Hunger, record.Thirst) && OutpostZero.Sensory.NoiseManager.Instance != null)
                OutpostZero.Sensory.NoiseManager.Instance.EmitNoise(transform.position, RationNoise.Radius, RationNoise.Loud, NoiseType.RationBite, gameObject);
            GameplayFeedback.Toast(WoundEase.Note(FieldHand.Spent(record.Id, null), eased, null));
            return true;
        }

        public void DepositScrapToColony()
        {
            var storage = OutpostZero.Colony.ColonyStorage.Instance;
            if (storage == null) return;
            bool moved = false;
            if (scrapCount > 0)
            {
                int movedScrap = storage.AddScrap(scrapCount);
                scrapCount -= movedScrap;
                if (movedScrap > 0) moved = true;
            }
            if (DepositMaterial(storage, "cloth")) moved = true;
            if (DepositMaterial(storage, "chemicals")) moved = true;
            if (DepositMaterial(storage, "tape")) moved = true;
            if (DepositRaw(storage)) moved = true;
            if (DepositPrints(storage)) moved = true;
            if (!moved) return;
            RecalculateWeight();
            OnInventoryChanged?.Invoke();
        }

        private bool DepositMaterial(OutpostZero.Colony.ColonyStorage storage, string id)
        {
            var existing = items.Find(item => item.ItemId == id);
            if (existing == null || existing.Quantity <= 0) return false;
            int count = existing.Quantity;
            int moved;
            if (id == "cloth") moved = storage.AddCloth(count);
            else if (id == "chemicals") moved = storage.AddChemicals(count);
            else moved = storage.AddTape(count);
            if (moved <= 0) return false;
            existing.Quantity -= moved;
            if (existing.Quantity <= 0) items.Remove(existing);
            return true;
        }

        private bool DepositPrints(OutpostZero.Colony.ColonyStorage storage)
        {
            bool moved = false;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                var existing = items[i];
                if (existing == null || existing.Quantity <= 0 || string.IsNullOrEmpty(existing.ItemId)) continue;
                if (!existing.ItemId.StartsWith("print_")) continue;
                storage.LearnPrint(existing.ItemId.Substring(6));
                items.RemoveAt(i);
                moved = true;
            }
            return moved;
        }

        private bool DepositRaw(OutpostZero.Colony.ColonyStorage storage)
        {
            var existing = items.Find(item => item.ItemId == "raw_food");
            if (existing == null || existing.Quantity <= 0) return false;
            int moved = storage.AddRaw(existing.Quantity);
            if (moved <= 0) return false;
            existing.Quantity -= moved;
            if (existing.Quantity <= 0) items.Remove(existing);
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
            ItemDatabase.SpawnWorld(id, count, transform.position + transform.forward * 0.8f + Vector3.up * 0.05f, Quaternion.Euler(0f, transform.eulerAngles.y, 0f));
            GameplayFeedback.Toast(PackSay.Dropped(id, name, null));
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

        public int LastDoseSkill { get; private set; }

        public bool LastEase { get; private set; }

        public bool UseMedkit()
        {
            LastEase = false;
            if (medicalKits <= 0) return false;

            var health = GetComponent<Combat.HealthSystem>();
            var effects = GetComponent<StatusEffectController>();
            bool wounded = effects != null && (effects.IsBleeding || effects.IsInfected);
            var leader = SurvivorRoster.Instance != null ? SurvivorRoster.Instance.Leader : null;
            bool camp = leader != null && WoundEase.Helps(leader.injury);
            if (health != null && health.CurrentHealth >= health.MaxHealth && !wounded && !camp) return false;

            medicalKits--;
            LastDoseSkill = SurvivorRoster.LeaderPractice("Medic");
            health?.Heal(FieldHand.Medkit(LastDoseSkill) + HandDepth.Heal(LastDoseSkill));
            effects?.StopBleed();
            effects?.CureInfection();
            LastEase = SurvivorRoster.Instance != null && SurvivorRoster.Instance.EaseLeader();
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
