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
        Relief,
        Flare,
        Bomb,
        Cell
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
    /// Item stats the game reads. These built-in records are the fallback; at startup
    /// <see cref="ItemDatabase"/> overwrites them from the ItemDefinition assets designers tune.
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
            new ItemRecord { Id = "raw_food", DisplayName = "Raw Food", Category = Core.ItemCategory.FoodWater, Weight = 0.35f, Hunger = 12f, Use = ItemUse.Food },
            new ItemRecord { Id = "water", DisplayName = "Water Bottle", Category = Core.ItemCategory.FoodWater, Weight = 0.5f, Thirst = 40f, Use = ItemUse.Water },
            new ItemRecord { Id = "ammo_9mm", DisplayName = "9mm Rounds", Category = Core.ItemCategory.Ammunition, Weight = 0.02f, AmmoType = Core.WeaponType.Pistol, AmmoAmount = 12, Use = ItemUse.Ammo },
            new ItemRecord { Id = "ammo_shells", DisplayName = "Shotgun Shells", Category = Core.ItemCategory.Ammunition, Weight = 0.04f, AmmoType = Core.WeaponType.Shotgun, AmmoAmount = 6, Use = ItemUse.Ammo },
            new ItemRecord { Id = "ammo_rifle", DisplayName = "Rifle Magazine", Category = Core.ItemCategory.Ammunition, Weight = 0.08f, AmmoType = Core.WeaponType.Rifle, AmmoAmount = 30, Use = ItemUse.Ammo },
            new ItemRecord { Id = "ammo_smg", DisplayName = "SMG Magazine", Category = Core.ItemCategory.Ammunition, Weight = 0.05f, AmmoType = Core.WeaponType.SMG, AmmoAmount = 25, Use = ItemUse.Ammo },
            new ItemRecord { Id = "scrap", DisplayName = "Scrap", Category = Core.ItemCategory.ScrapMaterial, Weight = 0.1f, Use = ItemUse.Material },
            new ItemRecord { Id = "cloth", DisplayName = "Cloth", Category = Core.ItemCategory.ScrapMaterial, Weight = 0.15f, Use = ItemUse.Material },
            new ItemRecord { Id = "chemicals", DisplayName = "Chemicals", Category = Core.ItemCategory.ScrapMaterial, Weight = 0.2f, Use = ItemUse.Material },
            new ItemRecord { Id = "tape", DisplayName = "Duct Tape", Category = Core.ItemCategory.ScrapMaterial, Weight = 0.1f, Use = ItemUse.Material },
            new ItemRecord { Id = "noise_lure", DisplayName = "Noise Lure", Category = Core.ItemCategory.KeyItem, Weight = 0.2f, Use = ItemUse.Lure },
            new ItemRecord { Id = "street_bottle", DisplayName = "Street Bottle", Category = Core.ItemCategory.KeyItem, Weight = 0.25f, Use = ItemUse.Lure },
            new ItemRecord { Id = "molotov", DisplayName = "Molotov", Category = Core.ItemCategory.Fuel, Weight = 0.6f, Use = ItemUse.Molotov },
            new ItemRecord { Id = "flare", DisplayName = "Flare", Category = Core.ItemCategory.KeyItem, Weight = 0.3f, Use = ItemUse.Flare },
            new ItemRecord { Id = "pipe_bomb", DisplayName = "Pipe Bomb", Category = Core.ItemCategory.Fuel, Weight = 0.8f, Use = ItemUse.Bomb },
            new ItemRecord { Id = "print_flare", DisplayName = "Flare Blueprint", Category = Core.ItemCategory.KeyItem, Weight = 0.05f },
            new ItemRecord { Id = "print_repair", DisplayName = "Repair Blueprint", Category = Core.ItemCategory.KeyItem, Weight = 0.05f },
            new ItemRecord { Id = "print_wall", DisplayName = "Wall Blueprint", Category = Core.ItemCategory.KeyItem, Weight = 0.05f },
            new ItemRecord { Id = "print_radio", DisplayName = "Radio Blueprint", Category = Core.ItemCategory.KeyItem, Weight = 0.05f },
            new ItemRecord { Id = "cell", DisplayName = "Lamp Cell", Category = Core.ItemCategory.KeyItem, Weight = 0.15f, Use = ItemUse.Cell }
        };

        public static IReadOnlyList<ItemRecord> All => records;

        /// <summary>Takes designer values for known ids and adds new ones. Returns how many records changed.</summary>
        public static int Apply(IEnumerable<ItemRecord> incoming)
        {
            if (incoming == null) return 0;
            int changed = 0;
            foreach (var record in incoming)
            {
                if (record == null || string.IsNullOrEmpty(record.Id)) continue;
                int at = records.FindIndex(r => r.Id == record.Id);
                if (at >= 0) records[at] = record;
                else records.Add(record);
                changed++;
            }
            return changed;
        }

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

    public enum LootRoll
    {
        Fixed,
        Pick,
        Chance,
        Spread
    }

    /// <summary>
    /// One line of a loot table. Pick rolls once and takes itemId above the threshold, altItemId otherwise.
    /// Chance rolls once and grants count above the threshold, nothing otherwise. Spread adds 0..spread-1.
    /// Fixed never touches the dice, so entries keep the same random sequence in any order of edits after them.
    /// </summary>
    [System.Serializable]
    public struct LootEntry
    {
        public string itemId;
        public string altItemId;
        public LootRoll roll;
        public float threshold;
        public int count;
        public int spread;

        public static LootEntry Fixed(string id, int count) => new LootEntry { itemId = id, roll = LootRoll.Fixed, count = count };
        public static LootEntry Pick(string above, float threshold, string otherwise) => new LootEntry { itemId = above, altItemId = otherwise, roll = LootRoll.Pick, threshold = threshold, count = 1 };
        public static LootEntry Chance(string id, float threshold) => new LootEntry { itemId = id, roll = LootRoll.Chance, threshold = threshold, count = 1 };
        public static LootEntry Spread(string id, int count, int spread) => new LootEntry { itemId = id, roll = LootRoll.Spread, count = count, spread = spread };
    }

    public static class LootTables
    {
        public struct Grant
        {
            public string ItemId;
            public int Count;
        }

        public const string Crate = "crate";

        private static readonly Dictionary<string, LootEntry[]> builtin = new Dictionary<string, LootEntry[]>
        {
            ["medical"] = new[]
            {
                LootEntry.Pick("medkit", 0.45f, "bandage"),
                LootEntry.Fixed("water", 1),
                LootEntry.Chance("antibiotics", 0.55f)
            },
            ["military"] = new[]
            {
                LootEntry.Pick("ammo_rifle", 0.4f, "ammo_9mm"),
                LootEntry.Chance("bandage", 0.5f),
                LootEntry.Chance("print_flare", 0.62f),
                LootEntry.Chance("ammo_smg", 0.7f)
            },
            [Crate] = new[]
            {
                LootEntry.Spread("scrap", 2, 5),
                LootEntry.Pick("canned_food", 0.55f, "water"),
                LootEntry.Fixed("cloth", 1),
                LootEntry.Pick("chemicals", 0.6f, "tape"),
                LootEntry.Chance("flare", 0.72f),
                LootEntry.Chance("pipe_bomb", 0.88f),
                LootEntry.Chance("raw_food", 0.5f)
            }
        };

        private static readonly Dictionary<string, LootEntry[]> tables = new Dictionary<string, LootEntry[]>(builtin);

        public static IEnumerable<string> Ids => tables.Keys;

        public static LootEntry[] Entries(string tableId)
        {
            if (tableId != null && tables.TryGetValue(tableId, out var entries)) return entries;
            return tables[Crate];
        }

        /// <summary>Replaces a table with designer data. Unknown or empty tables keep what they had.</summary>
        public static void Use(string tableId, LootEntry[] entries)
        {
            if (string.IsNullOrEmpty(tableId) || entries == null || entries.Length == 0) return;
            tables[tableId] = entries;
        }

        public static void Reset()
        {
            tables.Clear();
            foreach (var pair in builtin) tables[pair.Key] = pair.Value;
        }

        public static Grant[] Roll(string tableId, int salt)
        {
            var rng = new System.Random(salt);
            if (tableId == Crate && !string.IsNullOrEmpty(DistrictRules.ActiveTable))
            {
                tableId = DistrictRules.ActiveTable;
            }
            var entries = Entries(tableId);
            var grants = new Grant[entries.Length];
            for (int i = 0; i < entries.Length; i++) grants[i] = Evaluate(entries[i], rng);
            return grants;
        }

        public static Grant Evaluate(LootEntry entry, System.Random rng)
        {
            switch (entry.roll)
            {
                case LootRoll.Pick:
                    return new Grant { ItemId = rng.NextDouble() > entry.threshold ? entry.itemId : entry.altItemId, Count = entry.count };
                case LootRoll.Chance:
                    return new Grant { ItemId = entry.itemId, Count = rng.NextDouble() > entry.threshold ? entry.count : 0 };
                case LootRoll.Spread:
                    return new Grant { ItemId = entry.itemId, Count = entry.count + (entry.spread > 0 ? rng.Next(0, entry.spread) : 0) };
                default:
                    return new Grant { ItemId = entry.itemId, Count = entry.count };
            }
        }
    }
}
