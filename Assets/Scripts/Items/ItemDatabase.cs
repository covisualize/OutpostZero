using System.Collections.Generic;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Items
{
    /// <summary>
    /// Runtime lookup of item definitions. Loads Resources/ItemDatabase once, applies its stats and loot tables,
    /// and fills any id the asset lacks with a definition built from the code fallback so Get never misses.
    /// </summary>
    public static class ItemDatabase
    {
        public const string ResourcePath = "ItemDatabase";

        private static readonly Dictionary<string, ItemDefinition> byId = new Dictionary<string, ItemDefinition>();
        private static bool loaded;

        public static bool FromAsset { get; private set; }

        public static ItemDefinition Get(string id)
        {
            Ensure();
            if (string.IsNullOrEmpty(id)) return null;
            return byId.TryGetValue(id, out var definition) ? definition : null;
        }

        public static IEnumerable<ItemDefinition> All
        {
            get
            {
                Ensure();
                return byId.Values;
            }
        }

        public static Texture2D Icon(string id)
        {
            var definition = Get(id);
            return definition != null ? definition.icon : null;
        }

        public static void Ensure()
        {
            if (loaded) return;
            Use(Resources.Load<ItemDatabaseAsset>(ResourcePath));
        }

        public static void Use(ItemDatabaseAsset asset)
        {
            loaded = true;
            byId.Clear();
            FromAsset = asset != null;
            if (asset != null)
            {
                var records = new List<ItemRecord>();
                foreach (var definition in asset.items)
                {
                    if (definition == null || string.IsNullOrEmpty(definition.id)) continue;
                    byId[definition.id] = definition;
                    records.Add(definition.ToRecord());
                }
                ItemCatalog.Apply(records);
                foreach (var table in asset.tables)
                {
                    if (table != null) LootTables.Use(table.id, table.entries);
                }
            }
            foreach (var record in ItemCatalog.All)
            {
                if (byId.ContainsKey(record.Id)) continue;
                ItemDefinition made = record.Use == ItemUse.Ammo
                    ? ScriptableObject.CreateInstance<AmmoDefinition>()
                    : ScriptableObject.CreateInstance<ItemDefinition>();
                made.name = record.Id;
                made.CopyFrom(record);
                byId[record.Id] = made;
            }
        }

        /// <summary>Puts one pickup in the world: the item's prefab when it has one, a small crate otherwise.</summary>
        public static GameObject SpawnWorld(string id, int count, Vector3 position, Quaternion rotation)
        {
            var definition = Get(id);
            GameObject drop;
            if (definition != null && definition.worldPrefab != null)
            {
                drop = Object.Instantiate(definition.worldPrefab, position, rotation);
                if (drop.GetComponentInChildren<Collider>() == null) drop.AddComponent<BoxCollider>();
            }
            else
            {
                drop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                drop.transform.SetPositionAndRotation(position, rotation);
                drop.transform.localScale = new Vector3(0.28f, 0.18f, 0.28f);
            }
            drop.name = "Dropped_" + id;
            drop.layer = GameLayers.Interactable;
            foreach (Transform child in drop.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = GameLayers.Interactable;
            drop.AddComponent<WorldItem>().Configure(id, count);
            return drop;
        }
    }
}
