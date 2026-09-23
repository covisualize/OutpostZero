#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Colony;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/Factions and Resources/FactionBook in step with the built-in faction table. Missing factions
    /// are created from the code tables; existing assets keep their tuned values. A faction asset added by hand
    /// with a new id (lower-case letters, digits and underscores) is listed after the built-in four and joins
    /// the visit calendar as a new faction.
    /// </summary>
    public static class FactionBookSync
    {
        public const string FactionsDir = "Assets/Data/Factions";
        public const string BookPath = "Assets/Resources/FactionBook.asset";

        [MenuItem("Tools/Outpost Zero/Sync Faction Book", false, 6)]
        public static void SyncFromMenu()
        {
            int count = Sync();
            Debug.Log("[FactionBookSync] The faction book lists " + count + " factions.");
        }

        public static int Sync()
        {
            Directory.CreateDirectory(FactionsDir);
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var listed = new List<FactionDefinition>();
            var seen = new HashSet<string>();
            foreach (var row in FactionTable.BuiltInRows())
            {
                string path = FactionsDir + "/" + row.Id + ".asset";
                var faction = AssetDatabase.LoadAssetAtPath<FactionDefinition>(path);
                if (faction == null)
                {
                    faction = ScriptableObject.CreateInstance<FactionDefinition>();
                    faction.CopyFrom(row);
                    AssetDatabase.CreateAsset(faction, path);
                }
                listed.Add(faction);
                seen.Add(faction.id);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:FactionDefinition", new[] { FactionsDir }))
            {
                var extra = AssetDatabase.LoadAssetAtPath<FactionDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (extra == null || string.IsNullOrEmpty(extra.id) || !seen.Add(extra.id)) continue;
                listed.Add(extra);
            }

            var book = AssetDatabase.LoadAssetAtPath<FactionBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<FactionBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            book.factions = listed.ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            FactionBook.Use(book);
            return listed.Count;
        }
    }
}
#endif
