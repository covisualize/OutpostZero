#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Sensory;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/Noise and Resources/NoiseBook in step with the built-in noise rows.
    /// Missing ids are created from the code rows; existing assets keep their tuned values.
    /// </summary>
    public static class NoiseBookSync
    {
        public const string NoisesDir = "Assets/Data/Noise";
        public const string BookPath = "Assets/Resources/NoiseBook.asset";

        [MenuItem("Tools/Outpost Zero/Sync Noise Book", false, 5)]
        public static void SyncFromMenu()
        {
            int count = Sync();
            Debug.Log("[NoiseBookSync] The noise book lists " + count + " noises.");
        }

        public static int Sync()
        {
            Directory.CreateDirectory(NoisesDir);
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var listed = new List<NoiseDefinition>();
            foreach (var row in NoiseTable.BuiltInRows())
            {
                string path = NoisesDir + "/" + row.Id + ".asset";
                var noise = AssetDatabase.LoadAssetAtPath<NoiseDefinition>(path);
                if (noise == null)
                {
                    noise = ScriptableObject.CreateInstance<NoiseDefinition>();
                    noise.CopyFrom(row);
                    AssetDatabase.CreateAsset(noise, path);
                }
                listed.Add(noise);
            }

            var book = AssetDatabase.LoadAssetAtPath<NoiseBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<NoiseBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            book.noises = listed.ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            NoiseBook.Use(book);
            return listed.Count;
        }
    }
}
#endif
