#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Player;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/Status and Resources/StatusBook in step with the built-in condition rows.
    /// Missing kinds are created from the code rows; existing assets keep their tuned values.
    /// </summary>
    public static class StatusBookSync
    {
        public const string EffectsDir = "Assets/Data/Status";
        public const string BookPath = "Assets/Resources/StatusBook.asset";

        [MenuItem("Tools/Outpost Zero/Sync Status Book", false, 5)]
        public static void SyncFromMenu()
        {
            int count = Sync();
            Debug.Log("[StatusBookSync] The status book lists " + count + " conditions.");
        }

        public static int Sync()
        {
            Directory.CreateDirectory(EffectsDir);
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var listed = new List<StatusEffectDefinition>();
            foreach (var row in StatusTable.BuiltInRows())
            {
                string path = EffectsDir + "/" + row.Id + ".asset";
                var effect = AssetDatabase.LoadAssetAtPath<StatusEffectDefinition>(path);
                if (effect == null)
                {
                    effect = ScriptableObject.CreateInstance<StatusEffectDefinition>();
                    effect.CopyFrom(row);
                    AssetDatabase.CreateAsset(effect, path);
                }
                listed.Add(effect);
            }

            var book = AssetDatabase.LoadAssetAtPath<StatusBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<StatusBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            book.effects = listed.ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            StatusBook.Use(book);
            return listed.Count;
        }
    }
}
#endif
