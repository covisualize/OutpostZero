#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// The components the pipeline owns on a generated prefab: its SurfaceTag and root layer.
    /// Everything else on the prefab is left to hand edits.
    /// </summary>
    public static class PrefabTags
    {
        public static SurfaceKind SurfaceFor(ModelSidecar sidecar)
        {
            return sidecar == null ? SurfaceKind.Default : SurfaceTag.Guess(sidecar.category, sidecar.materials);
        }

        public static int LayerFor(ModelSidecar sidecar)
        {
            return sidecar == null ? 0 : GameLayers.ForAsset(sidecar.category, sidecar.id);
        }

        public static bool Apply(GameObject root, ModelSidecar sidecar)
        {
            if (root == null || sidecar == null) return false;
            bool changed = false;
            var tag = root.GetComponent<SurfaceTag>();
            if (tag == null)
            {
                tag = root.AddComponent<SurfaceTag>();
                changed = true;
            }
            var kind = SurfaceFor(sidecar);
            if (tag.Kind != kind)
            {
                tag.Set(kind);
                changed = true;
            }
            int layer = LayerFor(sidecar);
            if (root.layer != layer)
            {
                root.layer = layer;
                changed = true;
            }
            return changed;
        }

        [MenuItem("Tools/Outpost Zero/Refresh Prefab Tags", false, 5)]
        public static void RefreshAll()
        {
            int touched = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { PrefabCatalog.ModelRoot }))
            {
                string model = AssetDatabase.GUIDToAssetPath(guid);
                if (!model.EndsWith(".fbx")) continue;
                string prefab = PrefabCatalog.PrefabRoot + "/" + Path.ChangeExtension(model.Substring(PrefabCatalog.ModelRoot.Length + 1), ".prefab");
                if (!File.Exists(prefab)) continue;
                var root = PrefabUtility.LoadPrefabContents(prefab);
                try
                {
                    if (Apply(root, ModelSidecar.Load(model)))
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, prefab);
                        touched++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            Debug.Log("[PrefabTags] Updated " + touched + " prefabs.");
        }
    }
}
#endif
