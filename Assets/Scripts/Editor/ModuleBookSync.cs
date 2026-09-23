#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Colony;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/Modules and Resources/ModuleBook in step with the built-in module tables. Missing modules
    /// are created from the code tables; existing assets keep their tuned values. Modules added by hand in the
    /// folder join the book after the built-in kinds.
    /// </summary>
    public static class ModuleBookSync
    {
        public const string ModulesDir = "Assets/Data/Modules";
        public const string BookPath = "Assets/Resources/ModuleBook.asset";

        [MenuItem("Tools/Outpost Zero/Sync Module Book", false, 5)]
        public static void SyncFromMenu()
        {
            int count = Sync();
            Debug.Log("[ModuleBookSync] The module book lists " + count + " modules.");
        }

        public static int Sync()
        {
            Directory.CreateDirectory(ModulesDir);
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var listed = new List<BuildingModuleDefinition>();
            var seen = new HashSet<string>();
            foreach (var row in ModuleTable.BuiltInRows())
            {
                string path = ModulesDir + "/" + row.Id + ".asset";
                var module = AssetDatabase.LoadAssetAtPath<BuildingModuleDefinition>(path);
                if (module == null)
                {
                    module = ScriptableObject.CreateInstance<BuildingModuleDefinition>();
                    module.CopyFrom(row);
                    AssetDatabase.CreateAsset(module, path);
                }
                listed.Add(module);
                seen.Add(module.id);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:BuildingModuleDefinition", new[] { ModulesDir }))
            {
                var extra = AssetDatabase.LoadAssetAtPath<BuildingModuleDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (extra == null || string.IsNullOrEmpty(extra.id) || !seen.Add(extra.id)) continue;
                listed.Add(extra);
            }

            var book = AssetDatabase.LoadAssetAtPath<ModuleBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<ModuleBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            book.modules = listed.ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            ModuleBook.Use(book);
            return listed.Count;
        }
    }
}
#endif
