#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Expedition;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/Expeditions and Resources/ExpeditionBook in step with the built-in objective plan.
    /// Missing objectives and expeditions are created from the code table; existing assets keep their edits,
    /// and an expedition added by hand stays listed.
    /// </summary>
    public static class ExpeditionBookSync
    {
        public const string ExpeditionsDir = "Assets/Data/Expeditions";
        public const string ObjectivesDir = "Assets/Data/Expeditions/Objectives";
        public const string BookPath = "Assets/Resources/ExpeditionBook.asset";

        [MenuItem("Tools/Outpost Zero/Sync Expedition Book", false, 9)]
        public static void SyncFromMenu()
        {
            int count = Sync();
            Debug.Log("[ExpeditionBookSync] The expedition book lists " + count + " expeditions.");
        }

        public static string FileName(string id)
        {
            return id.Replace(".", "_");
        }

        public static int Sync()
        {
            Directory.CreateDirectory(ObjectivesDir);
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var listed = new List<ExpeditionDefinition>();
            var seen = new HashSet<string>();
            foreach (var pair in ObjectivePlan.Code)
            {
                string path = ExpeditionsDir + "/" + pair.Key + ".asset";
                var expedition = AssetDatabase.LoadAssetAtPath<ExpeditionDefinition>(path);
                if (expedition == null)
                {
                    expedition = ScriptableObject.CreateInstance<ExpeditionDefinition>();
                    expedition.district = pair.Key;
                    var objectives = new List<ObjectiveDefinition>();
                    foreach (var spec in pair.Value) objectives.Add(Objective(spec));
                    expedition.objectives = objectives.ToArray();
                    AssetDatabase.CreateAsset(expedition, path);
                }
                listed.Add(expedition);
                seen.Add(expedition.district);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:ExpeditionDefinition", new[] { ExpeditionsDir }))
            {
                var extra = AssetDatabase.LoadAssetAtPath<ExpeditionDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (extra == null || string.IsNullOrEmpty(extra.district) || !seen.Add(extra.district)) continue;
                listed.Add(extra);
            }

            var book = AssetDatabase.LoadAssetAtPath<ExpeditionBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<ExpeditionBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            book.expeditions = listed.ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            ExpeditionBook.Use(book);
            return listed.Count;
        }

        private static ObjectiveDefinition Objective(ObjectiveSpec spec)
        {
            string path = ObjectivesDir + "/" + FileName(spec.Id) + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<ObjectiveDefinition>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<ObjectiveDefinition>();
            asset.CopyFrom(spec);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
