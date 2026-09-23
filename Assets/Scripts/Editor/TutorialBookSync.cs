#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Shell;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Keeps Assets/Data/Tutorial and Resources/TutorialBook in step with the built-in tutorial tracks. Missing steps
    /// are created from the code tracks; existing assets keep their edits. The book lists the built-in steps in
    /// order, then any step added by hand, on the track it names.
    /// </summary>
    public static class TutorialBookSync
    {
        public const string StepsDir = "Assets/Data/Tutorial";
        public const string BookPath = "Assets/Resources/TutorialBook.asset";

        [MenuItem("Tools/Outpost Zero/Sync Tutorial Book", false, 8)]
        public static void SyncFromMenu()
        {
            int count = Sync();
            Debug.Log("[TutorialBookSync] The tutorial book lists " + count + " steps.");
        }

        public static string FileName(string key)
        {
            return key.Replace(".", "_");
        }

        public static int Sync()
        {
            Directory.CreateDirectory(StepsDir);
            Directory.CreateDirectory(Path.GetDirectoryName(BookPath));
            var camp = new List<TutorialStepDefinition>();
            var street = new List<TutorialStepDefinition>();
            var seen = new HashSet<string>();
            Seed(TutorialTrack.CampTrack, TutorialTrack.CodeCamp, camp, seen);
            Seed(TutorialTrack.StreetTrack, TutorialTrack.CodeStreet, street, seen);
            foreach (string guid in AssetDatabase.FindAssets("t:TutorialStepDefinition", new[] { StepsDir }))
            {
                var extra = AssetDatabase.LoadAssetAtPath<TutorialStepDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (extra == null || string.IsNullOrEmpty(extra.key) || !seen.Add(extra.key)) continue;
                (extra.track == TutorialTrack.StreetTrack ? street : camp).Add(extra);
            }

            var book = AssetDatabase.LoadAssetAtPath<TutorialBook>(BookPath);
            if (book == null)
            {
                book = ScriptableObject.CreateInstance<TutorialBook>();
                AssetDatabase.CreateAsset(book, BookPath);
            }
            book.camp = camp.ToArray();
            book.street = street.ToArray();
            EditorUtility.SetDirty(book);
            AssetDatabase.SaveAssets();
            TutorialBook.Use(book);
            return camp.Count + street.Count;
        }

        private static void Seed(string track, TutorialStep[] steps, List<TutorialStepDefinition> listed, HashSet<string> seen)
        {
            foreach (var step in steps)
            {
                string path = StepsDir + "/" + FileName(step.Key) + ".asset";
                var asset = AssetDatabase.LoadAssetAtPath<TutorialStepDefinition>(path);
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<TutorialStepDefinition>();
                    asset.CopyFrom(track, step);
                    AssetDatabase.CreateAsset(asset, path);
                }
                listed.Add(asset);
                seen.Add(asset.key);
            }
        }
    }
}
#endif
