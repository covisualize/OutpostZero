#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Items;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/Items, Assets/Data/Loot and Resources/ItemDatabase in step with the item ids the game knows.
    /// Missing definitions are created from the code fallback. Existing ones keep their designer values and only
    /// gain the world model, prefab and icon references the pipeline produced.
    /// </summary>
    public static class ItemDatabaseSync
    {
        public const string ItemsDir = "Assets/Data/Items";
        public const string LootDir = "Assets/Data/Loot";
        public const string DatabasePath = "Assets/Resources/ItemDatabase.asset";

        [MenuItem("Tools/Outpost Zero/Sync Item Database", false, 3)]
        public static void SyncFromMenu()
        {
            var problems = Sync();
            if (problems.Count == 0) Debug.Log("[ItemDatabaseSync] Every item has a definition, a world prefab and an icon.");
            foreach (var problem in problems) Debug.LogWarning("[ItemDatabaseSync] " + problem);
        }

        /// <summary>Returns the ids still missing a prefab or icon, so callers can fail loudly.</summary>
        public static List<string> Sync()
        {
            Directory.CreateDirectory(ItemsDir);
            Directory.CreateDirectory(LootDir);
            Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath));
            PrefabCatalog.Refresh();

            var problems = new List<string>();
            var items = new List<ItemDefinition>();
            foreach (var record in ItemCatalog.All)
            {
                var definition = LoadOrCreateItem(record);
                if (string.IsNullOrEmpty(definition.worldModel)) definition.worldModel = ItemVisuals.ModelFor(record.Id);
                string model = definition.worldModel;
                if (string.IsNullOrEmpty(model))
                {
                    problems.Add(record.Id + " has no world model in ItemVisuals");
                }
                else
                {
                    if (definition.icon == null) definition.icon = AssetDatabase.LoadAssetAtPath<Texture2D>(ItemVisuals.IconPath(model));
                    if (definition.worldPrefab == null) definition.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemVisuals.PrefabPath(model));
                    if (definition.icon == null) problems.Add(record.Id + " missing icon " + ItemVisuals.IconPath(model));
                    if (definition.worldPrefab == null) problems.Add(record.Id + " missing prefab " + ItemVisuals.PrefabPath(model));
                }
                EditorUtility.SetDirty(definition);
                items.Add(definition);
            }

            var tables = new List<LootTableDefinition>();
            foreach (var id in new List<string>(LootTables.Ids))
            {
                string path = LootDir + "/" + id + ".asset";
                var table = AssetDatabase.LoadAssetAtPath<LootTableDefinition>(path);
                if (table == null)
                {
                    table = ScriptableObject.CreateInstance<LootTableDefinition>();
                    table.id = id;
                    table.entries = (LootEntry[])LootTables.Entries(id).Clone();
                    AssetDatabase.CreateAsset(table, path);
                }
                tables.Add(table);
            }

            var database = AssetDatabase.LoadAssetAtPath<ItemDatabaseAsset>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<ItemDatabaseAsset>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }
            database.items = items.ToArray();
            database.tables = tables.ToArray();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            ItemDatabase.Use(database);
            return problems;
        }

        private static ItemDefinition LoadOrCreateItem(ItemRecord record)
        {
            string path = ItemVisuals.AssetPath(record.Id);
            var existing = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if (existing != null) return existing;
            ItemDefinition made = record.Use == ItemUse.Ammo
                ? ScriptableObject.CreateInstance<AmmoDefinition>()
                : ScriptableObject.CreateInstance<ItemDefinition>();
            made.CopyFrom(record);
            AssetDatabase.CreateAsset(made, path);
            return made;
        }
    }
}
#endif
