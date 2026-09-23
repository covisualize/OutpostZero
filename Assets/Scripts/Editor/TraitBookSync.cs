#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Colony;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/Traits and Resources/TraitBook in step with the built-in trait table. Missing traits are
    /// created from the code table; existing assets keep their tuned values. Built-in traits stay first, in code
    /// order, so seeded camps keep their draw; traits added by hand follow.
    /// </summary>
    public static class TraitBookSync
    {
        public const string TraitsDir = "Assets/Data/Traits";
        public const string BookPath = "Assets/Resources/TraitBook.asset";

        [MenuItem("Tools/Outpost Zero/Sync Trait Book", false, 7)]
        public static void SyncFromMenu()
        {
            int count = Sync();
            Debug.Log("[TraitBookSync] The trait book lists " + count + " traits.");
        }

        public static string FileName(string id)
        {
            return id.Replace(" ", "");
        }

        public static int Sync()
        {
            Directory.CreateDirectory(TraitsDir);
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var listed = new List<TraitDefinition>();
            var seen = new HashSet<string>();
            foreach (var row in TraitTable.BuiltInRows())
            {
                string path = TraitsDir + "/" + FileName(row.Id) + ".asset";
                var trait = AssetDatabase.LoadAssetAtPath<TraitDefinition>(path);
                if (trait == null)
                {
                    trait = ScriptableObject.CreateInstance<TraitDefinition>();
                    trait.CopyFrom(row);
                    AssetDatabase.CreateAsset(trait, path);
                }
                listed.Add(trait);
                seen.Add(trait.id);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:TraitDefinition", new[] { TraitsDir }))
            {
                var extra = AssetDatabase.LoadAssetAtPath<TraitDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (extra == null || string.IsNullOrEmpty(extra.id) || !seen.Add(extra.id)) continue;
                listed.Add(extra);
            }

            var book = AssetDatabase.LoadAssetAtPath<TraitBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<TraitBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            book.traits = listed.ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            TraitBook.Use(book);
            return listed.Count;
        }
    }
}
#endif
