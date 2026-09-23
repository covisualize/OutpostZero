#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using OutpostZero.Graphics;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Adds the URP decal feature to both project renderers, and SSAO to the full renderer only,
    /// so the Low tier (which uses the lite renderer) never pays for ambient occlusion.
    /// </summary>
    public static class RendererFeatureSetup
    {
        public static void EnsureFeatures()
        {
            Ensure<DecalRendererFeature>(QualityProfile.SettingsFolder + "/" + QualityProfile.RendererFull + ".asset", "DecalRendererFeature");
            Ensure<DecalRendererFeature>(QualityProfile.SettingsFolder + "/" + QualityProfile.RendererLite + ".asset", "DecalRendererFeature");
            Ensure<ScreenSpaceAmbientOcclusion>(QualityProfile.SettingsFolder + "/" + QualityProfile.RendererFull + ".asset", "ScreenSpaceAmbientOcclusion");
        }

        private static void Ensure<T>(string rendererPath, string featureName) where T : ScriptableRendererFeature
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (data == null) return;

            var serialized = new SerializedObject(data);
            var features = serialized.FindProperty("m_RendererFeatures");
            var map = serialized.FindProperty("m_RendererFeatureMap");
            if (features == null || map == null) return;

            for (int i = 0; i < features.arraySize; i++)
            {
                if (features.GetArrayElementAtIndex(i).objectReferenceValue is T) return;
            }

            var feature = ScriptableObject.CreateInstance<T>();
            feature.name = featureName;
            AssetDatabase.AddObjectToAsset(feature, data);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);

            features.arraySize++;
            features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;
            map.arraySize++;
            map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
