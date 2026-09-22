using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace OutpostZero.EditorTools
{
    /// <summary>
    /// Resolves an asset id (the file stem shared by a model and its generated prefab) to the
    /// prefab under Assets/Prefabs, falling back to the source model while prefabs are regenerating.
    /// </summary>
    public static class PrefabCatalog
    {
        public const string PrefabRoot = "Assets/Prefabs";
        public const string ModelRoot = "Assets/Models";

        static Dictionary<string, string> prefabs;
        static Dictionary<string, string> models;

        public static string Id(string pathOrId)
        {
            if (string.IsNullOrEmpty(pathOrId)) return string.Empty;
            return Path.GetFileNameWithoutExtension(pathOrId.Replace('\\', '/'));
        }

        public static Dictionary<string, string> Index(IEnumerable<string> assetPaths)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            if (assetPaths == null) return map;
            foreach (var path in assetPaths)
            {
                string id = Id(path);
                if (id.Length > 0 && !map.ContainsKey(id)) map[id] = path.Replace('\\', '/');
            }
            return map;
        }

        public static string Lookup(string id, IReadOnlyDictionary<string, string> primary, IReadOnlyDictionary<string, string> fallback)
        {
            id = Id(id);
            if (primary != null && primary.TryGetValue(id, out var hit)) return hit;
            if (fallback != null && fallback.TryGetValue(id, out var model)) return model;
            return null;
        }

        public static void Refresh()
        {
            prefabs = Index(Paths("t:Prefab", PrefabRoot));
            models = Index(Paths("t:Model", ModelRoot));
        }

        public static string PathFor(string id)
        {
            if (prefabs == null || models == null) Refresh();
            return Lookup(id, prefabs, models);
        }

        public static GameObject Load(string id)
        {
            string path = PathFor(id);
            return path == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        static IEnumerable<string> Paths(string filter, string root)
        {
            if (!AssetDatabase.IsValidFolder(root)) yield break;
            foreach (var guid in AssetDatabase.FindAssets(filter, new[] { root }))
                yield return AssetDatabase.GUIDToAssetPath(guid);
        }
    }
}
