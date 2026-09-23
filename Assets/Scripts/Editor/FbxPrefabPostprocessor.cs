#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Turns each imported model into a prefab the first time it arrives. The pipeline's
    /// &lt;id&gt;.meta.json sidecar picks the collider and names the source materials that are
    /// remapped onto the baked surface material, so LOD copies share it too.
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
            RemapToBaked(importer, assetPath, ModelSidecar.Load(assetPath));
        }

        private static void RemapToBaked(ModelImporter importer, string fbxPath, ModelSidecar sidecar)
        {
            if (sidecar == null || sidecar.materials == null) return;
            var baked = AssetDatabase.LoadAssetAtPath<Material>(BakedPath(fbxPath));
            if (baked == null) return;
            foreach (string source in sidecar.materials)
            {
                if (string.IsNullOrEmpty(source)) continue;
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), source), baked);
            }
        }

        private static string BakedPath(string fbxPath)
        {
            return "Assets/Materials/Baked/" + Path.GetFileNameWithoutExtension(fbxPath) + ".mat";
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
                    var baked = EnsureSurfaceMaterial(path);
                    string relative = path.Substring("Assets/Models/".Length);
                    string prefabPath = "Assets/Prefabs/" + Path.ChangeExtension(relative, ".prefab");
                    string directory = Path.GetDirectoryName(prefabPath);
                    if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                    if (File.Exists(prefabPath))
                    {
                        var contents = PrefabUtility.LoadPrefabContents(prefabPath);
                        try
                        {
                            if (PrefabTags.Apply(contents, ModelSidecar.Load(path))) PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
                        }
                        finally
                        {
                            PrefabUtility.UnloadPrefabContents(contents);
                        }
                        continue;
                    }

                    var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (source == null) continue;
                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
                    if (baked != null)
                    {
                        foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                            renderer.sharedMaterial = baked;
                    }
                    var sidecar = ModelSidecar.Load(path);
                    if (instance.GetComponentInChildren<Collider>() == null)
                        ModelSidecar.AddCollider(instance, sidecar);
                    PrefabTags.Apply(instance, sidecar);
                    PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
                    Object.DestroyImmediate(instance);
                }
            }
            finally
            {
                busy = false;
            }
        }

        private static Material EnsureSurfaceMaterial(string fbxPath)
        {
            string directory = Path.GetDirectoryName(fbxPath)?.Replace('\\', '/');
            string stem = Path.GetFileNameWithoutExtension(fbxPath);
            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + "/" + stem + "_Albedo.png");
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + "/" + stem + "_Normal.png");
            var occlusion = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + "/" + stem + "_AO.png");
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(directory + "/" + stem + "_Mask.png");
            if (albedo == null) return null;
            var shader = Shader.Find("OutpostZero/TriplanarRim");
            if (shader == null) return null;
            const string folder = "Assets/Materials/Baked";
            Directory.CreateDirectory(folder);
            string materialPath = BakedPath(fbxPath);
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader) { name = stem };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetTexture("_BaseMap", albedo);
            material.SetTexture("_BumpMap", normal);
            material.SetTexture("_OcclusionMap", occlusion);
            material.SetTexture("_MaskMap", mask);
            material.SetFloat("_HasMaps", 1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void AssignCharacterClips(string path)
        {
            var clips = new System.Collections.Generic.List<AnimationClip>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                var clip = asset as AnimationClip;
                if (clip == null || clip.name.StartsWith("__preview")) continue;
                clips.Add(clip);
            }
            if (clips.Count > 0) SurvivorAnimatorBuilder.AssignMotions(OutpostZero.Core.CharacterRig.ControllerAsset(Path.GetFileNameWithoutExtension(path)), clips, AssetDatabase.LoadAssetAtPath<GameObject>(path));
        }
    }
}
#endif
