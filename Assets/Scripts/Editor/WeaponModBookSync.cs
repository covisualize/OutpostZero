#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Combat;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/WeaponMods and Resources/WeaponModBook in step with the built-in mod rows.
    /// Missing ids are created from the code rows; existing assets keep their tuned values.
    /// </summary>
    public static class WeaponModBookSync
    {
        public const string ModsDir = "Assets/Data/WeaponMods";
        public const string BookPath = "Assets/Resources/WeaponModBook.asset";

        [MenuItem("Tools/Outpost Zero/Sync Weapon Mod Book", false, 5)]
        public static void SyncFromMenu()
        {
            int count = Sync();
            Debug.Log("[WeaponModBookSync] The weapon mod book lists " + count + " mods.");
        }

        public static int Sync()
        {
            Directory.CreateDirectory(ModsDir);
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var listed = new List<WeaponModDefinition>();
            foreach (var row in WeaponModTable.BuiltInRows())
            {
                string path = ModsDir + "/" + row.Id + ".asset";
                var mod = AssetDatabase.LoadAssetAtPath<WeaponModDefinition>(path);
                if (mod == null)
                {
                    mod = ScriptableObject.CreateInstance<WeaponModDefinition>();
                    mod.CopyFrom(row);
                    AssetDatabase.CreateAsset(mod, path);
                }
                listed.Add(mod);
            }

            var book = AssetDatabase.LoadAssetAtPath<WeaponModBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<WeaponModBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            book.mods = listed.ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            WeaponModBook.Use(book);
            return listed.Count;
        }
    }
}
#endif
