#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Adds the URP decal feature to the project renderer the first time the arena is built.
    /// </summary>
    public static class RendererFeatureSetup
    {
        private const string RendererPath = "Assets/Settings/OutpostZero_URP_Renderer.asset";

        public static void EnsureDecals()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (data == null) return;

            var serialized = new SerializedObject(data);
            var features = serialized.FindProperty("m_RendererFeatures");
            var map = serialized.FindProperty("m_RendererFeatureMap");
            if (features == null || map == null) return;

            for (int i = 0; i < features.arraySize; i++)
            {
                if (features.GetArrayElementAtIndex(i).objectReferenceValue is DecalRendererFeature) return;
            }

            var feature = ScriptableObject.CreateInstance<DecalRendererFeature>();
            feature.name = "DecalRendererFeature";
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
