using System.Collections.Generic;
using OutpostZero.Shell;

namespace OutpostZero.Items
{
    public enum ItemUse
    {
        None,
        Heal,
        Food,
        Water,
        Ammo,
        Lure,
        Molotov,
        Material,
        Cure,
        Relief
    }

    public sealed class ItemRecord
    {
        public string Id;
        public string DisplayName;
        public Core.ItemCategory Category;
        public float Weight;
        public int Heal;
        public float Hunger;
        public float Thirst;
        public Core.WeaponType AmmoType;
        public int AmmoAmount;
        public ItemUse Use;
    }

    /// <summary>
    /// Runtime item database. Definitions live in code so a scene can loot, craft, and save
    /// without an editor import pass. Editor assets can mirror these ids later.
    /// </summary>
    public static class ItemCatalog
    {
        private static readonly List<ItemRecord> records = new List<ItemRecord>
        {
            new ItemRecord { Id = "medkit", DisplayName = "Medkit", Category = Core.ItemCategory.Medical, Weight = 0.5f, Heal = 50, Use = ItemUse.Heal },
            new ItemRecord { Id = "bandage", DisplayName = "Bandage", Category = Core.ItemCategory.Medical, Weight = 0.1f, Heal = 10, Use = ItemUse.Heal },
            new ItemRecord { Id = "antibiotics", DisplayName = "Antibiotics", Category = Core.ItemCategory.Medical, Weight = 0.15f, Use = ItemUse.Cure },
            new ItemRecord { Id = "painkillers", DisplayName = "Painkillers", Category = Core.ItemCategory.Medical, Weight = 0.1f, Use = ItemUse.Relief },
            new ItemRecord { Id = "canned_food", DisplayName = "Canned Food", Category = Core.ItemCategory.FoodWater, Weight = 0.4f, Hunger = 35f, Use = ItemUse.Food },
            new ItemRecord { Id = "water", DisplayName = "Water Bottle", Category = Core.ItemCategory.FoodWater, Weight = 0.5f, Thirst = 40f, Use = ItemUse.Water },
            new ItemRecord { Id = "ammo_9mm", DisplayName = "9mm Rounds", Category = Core.ItemCategory.Ammunition, Weight = 0.02f, AmmoType = Core.WeaponType.Pistol, AmmoAmount = 12, Use = ItemUse.Ammo },
            new ItemRecord { Id = "ammo_shells", DisplayName = "Shotgun Shells", Category = Core.ItemCategory.Ammunition, Weight = 0.04f, AmmoType = Core.WeaponType.Shotgun, AmmoAmount = 6, Use = ItemUse.Ammo },
            new ItemRecord { Id = "ammo_rifle", DisplayName = "Rifle Magazine", Category = Core.ItemCategory.Ammunition, Weight = 0.08f, AmmoType = Core.WeaponType.Rifle, AmmoAmount = 30, Use = ItemUse.Ammo },
            new ItemRecord { Id = "scrap", DisplayName = "Scrap", Category = Core.ItemCategory.ScrapMaterial, Weight = 0.1f, Use = ItemUse.Material },
            new ItemRecord { Id = "cloth", DisplayName = "Cloth", Category = Core.ItemCategory.ScrapMaterial, Weight = 0.15f, Use = ItemUse.Material },
            new ItemRecord { Id = "chemicals", DisplayName = "Chemicals", Category = Core.ItemCategory.ScrapMaterial, Weight = 0.2f, Use = ItemUse.Material },
            new ItemRecord { Id = "tape", DisplayName = "Duct Tape", Category = Core.ItemCategory.ScrapMaterial, Weight = 0.1f, Use = ItemUse.Material },
            new ItemRecord { Id = "noise_lure", DisplayName = "Noise Lure", Category = Core.ItemCategory.KeyItem, Weight = 0.2f, Use = ItemUse.Lure },
            new ItemRecord { Id = "molotov", DisplayName = "Molotov", Category = Core.ItemCategory.Fuel, Weight = 0.6f, Use = ItemUse.Molotov }
        };

        public static IReadOnlyList<ItemRecord> All => records;

        public static ItemRecord Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < records.Count; i++)
            {
                if (records[i].Id == id) return records[i];
            }
            return null;
        }
    }

    public static class LootTables
    {
        public struct Grant
        {
            public string ItemId;
            public int Count;
        }

        public static Grant[] Roll(string tableId, int salt)
        {
            var rng = new System.Random(salt);
            if (tableId == "crate" && !string.IsNullOrEmpty(DistrictRules.ActiveTable))
            {
                tableId = DistrictRules.ActiveTable;
            }
            if (tableId == "medical")
            {
                return new[]
                {
                    new Grant { ItemId = rng.NextDouble() > 0.45 ? "medkit" : "bandage", Count = 1 },
                    new Grant { ItemId = "water", Count = 1 },
                    new Grant { ItemId = "antibiotics", Count = rng.NextDouble() > 0.55 ? 1 : 0 }
                };
            }
            if (tableId == "military")
            {
                return new[]
                {
                    new Grant { ItemId = rng.NextDouble() > 0.4 ? "ammo_rifle" : "ammo_9mm", Count = 1 },
                    new Grant { ItemId = "bandage", Count = rng.NextDouble() > 0.5 ? 1 : 0 }
                };
            }

            return new[]
            {
                new Grant { ItemId = "scrap", Count = 2 + rng.Next(0, 5) },
                new Grant { ItemId = rng.NextDouble() > 0.55 ? "canned_food" : "water", Count = 1 },
                new Grant { ItemId = "cloth", Count = 1 },
                new Grant { ItemId = rng.NextDouble() > 0.6 ? "chemicals" : "tape", Count = 1 }
            };
        }
    }
}
