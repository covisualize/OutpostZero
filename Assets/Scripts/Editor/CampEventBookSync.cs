#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Colony;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/CampEvents and Resources/CampEventBook in step with the built-in event rows. Missing
    /// events are created from the code table; existing assets keep their edits, and one added by hand stays listed.
    /// </summary>
    public static class CampEventBookSync
    {
        public const string EventsDir = "Assets/Data/CampEvents";
        public const string BookPath = "Assets/Resources/CampEventBook.asset";

        [MenuItem("Tools/Outpost Zero/Sync Camp Event Book", false, 10)]
        public static void SyncFromMenu()
        {
            int count = Sync();
            Debug.Log("[CampEventBookSync] The camp event book lists " + count + " events.");
        }

        public static int Sync()
        {
            Directory.CreateDirectory(EventsDir);
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var listed = new List<CampEventDefinition>();
            var seen = new HashSet<string>();
            foreach (var row in CampEventTable.Code)
            {
                string path = EventsDir + "/" + row.Id + ".asset";
                var asset = AssetDatabase.LoadAssetAtPath<CampEventDefinition>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<CampEventDefinition>();
                    asset.CopyFrom(row);
                    AssetDatabase.CreateAsset(asset, path);
                }
                listed.Add(asset);
                seen.Add(asset.id);
            }
            foreach (string guid in AssetDatabase.FindAssets("t:CampEventDefinition", new[] { EventsDir }))
            {
                var extra = AssetDatabase.LoadAssetAtPath<CampEventDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (extra == null || string.IsNullOrEmpty(extra.id) || !seen.Add(extra.id)) continue;
                listed.Add(extra);
            }

            var book = AssetDatabase.LoadAssetAtPath<CampEventBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<CampEventBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            book.events = listed.ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            CampEventBook.Use(book);
            return listed.Count;
        }
    }
}
#endif
