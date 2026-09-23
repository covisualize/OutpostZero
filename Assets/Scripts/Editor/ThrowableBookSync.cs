#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Player;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/Throwables and Resources/ThrowableBook in step with the built-in throwable rows.
    /// Missing ids are created from the code rows; existing assets keep their tuned values, and assets
    /// already listed past the built-in ones stay listed.
    /// </summary>
    public static class ThrowableBookSync
    {
        public const string ThrowablesDir = "Assets/Data/Throwables";
        public const string BookPath = "Assets/Resources/ThrowableBook.asset";

        [MenuItem("Tools/Outpost Zero/Sync Throwable Book", false, 5)]
        public static void SyncFromMenu()
        {
            int count = Sync();
            Debug.Log("[ThrowableBookSync] The throwable book lists " + count + " throwables.");
        }

        public static int Sync()
        {
            Directory.CreateDirectory(ThrowablesDir);
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var listed = new List<ThrowableDefinition>();
            foreach (var row in ThrowableTable.BuiltInRows())
            {
                string path = ThrowablesDir + "/" + row.Id + ".asset";
                var throwable = AssetDatabase.LoadAssetAtPath<ThrowableDefinition>(path);
                if (throwable == null)
                {
                    throwable = ScriptableObject.CreateInstance<ThrowableDefinition>();
                    throwable.CopyFrom(row);
                    AssetDatabase.CreateAsset(throwable, path);
                }
                listed.Add(throwable);
            }

            var book = AssetDatabase.LoadAssetAtPath<ThrowableBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<ThrowableBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            else if (book.throwables != null)
            {
                foreach (var extra in book.throwables)
                    if (extra != null && !listed.Contains(extra)) listed.Add(extra);
            }
            book.throwables = listed.ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            ThrowableBook.Use(book);
            return listed.Count;
        }
    }
}
#endif
