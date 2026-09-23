#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.AI;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/Difficulty and Resources/DifficultyBook in step with the built-in difficulty rows.
    /// Missing levels are created from the code rows; existing assets keep their tuned values.
    /// </summary>
    public static class DifficultyBookSync
    {
        public const string LevelsDir = "Assets/Data/Difficulty";
        public const string BookPath = "Assets/Resources/DifficultyBook.asset";

        [MenuItem("Tools/Outpost Zero/Sync Difficulty Book", false, 5)]
        public static void SyncFromMenu()
        {
            int count = Sync();
            Debug.Log("[DifficultyBookSync] The difficulty book lists " + count + " levels.");
        }

        public static int Sync()
        {
            Directory.CreateDirectory(LevelsDir);
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var listed = new List<DifficultyDefinition>();
            foreach (var row in DifficultyTable.BuiltInRows())
            {
                string path = LevelsDir + "/" + row.Name + ".asset";
                var level = AssetDatabase.LoadAssetAtPath<DifficultyDefinition>(path);
                if (level == null)
                {
                    level = ScriptableObject.CreateInstance<DifficultyDefinition>();
                    level.CopyFrom(row);
                    AssetDatabase.CreateAsset(level, path);
                }
                listed.Add(level);
            }

            var book = AssetDatabase.LoadAssetAtPath<DifficultyBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<DifficultyBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            book.levels = listed.ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            DifficultyBook.Use(book);
            return listed.Count;
        }
    }
}
#endif
