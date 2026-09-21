#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Turns each imported model into a prefab with a collider the first time it arrives.
    /// </summary>
    public class FbxPrefabPostprocessor : AssetPostprocessor
    {
        private static bool busy;

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith("Assets/Models/") || !assetPath.EndsWith(".fbx")) return;
            var importer = (ModelImporter)assetImporter;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
            importer.importCameras = false;
            importer.importLights = false;
            bool character = assetPath.Contains("/Characters/");
            importer.importAnimation = character;
            importer.animationType = character
                ? ModelImporterAnimationType.Generic
                : ModelImporterAnimationType.None;
            if (character)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.animationCompression = ModelImporterAnimationCompression.Off;
            }
        }

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (busy) return;
            busy = true;
            try
            {
                foreach (string path in imported)
                {
                    if (!path.StartsWith("Assets/Models/") || !path.EndsWith(".fbx")) continue;
                    if (path.Contains("/Characters/")) AssignCharacterClips(path);
                    string relative = path.Substring("Assets/Models/".Length);
                    string prefabPath = "Assets/Prefabs/" + Path.ChangeExtension(relative, ".prefab");
                    string directory = Path.GetDirectoryName(prefabPath);
                    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                    if (File.Exists(prefabPath)) continue;

                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (source == null) continue;
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    if (instance.GetComponentInChildren<Collider>() == null)
                    {
                        var box = instance.AddComponent<BoxCollider>();
                        var renderers = instance.GetComponentsInChildren<Renderer>();
                        if (renderers.Length > 0)
                        {
                            var bounds = renderers[0].bounds;
                            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
                            box.center = instance.transform.InverseTransformPoint(bounds.center);
                            box.size = bounds.size;
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    Object.DestroyImmediate(instance);
                }
            }
            finally
            {
                busy = false;
            }
        }

        private static void AssignCharacterClips(string path)
        {
            AnimationClip idle = null;
            AnimationClip walk = null;
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var clip = asset as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview")) continue;
                if (clip.name.Contains("Idle")) idle = clip;
                else if (clip.name.Contains("Walk")) walk = clip;
            }
            if (idle != null || walk != null) SurvivorAnimatorBuilder.AssignMotions(idle, walk);
        }
    }
}
#endif
