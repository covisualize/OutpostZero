using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// Kit catalog id to generated prefab, so districts raised at runtime show the baked kit meshes.
    /// "Tools/Outpost Zero/Sync Kit Prefabs" rebuilds it from Assets/Prefabs/Kit.
    /// </summary>
    [CreateAssetMenu(fileName = "KitPrefabs", menuName = "Outpost Zero/Kit Prefab Set")]
    public class KitPrefabSet : ScriptableObject
    {
        public const string ResourcePath = "KitPrefabs";

        [Serializable]
        public class Entry
        {
            public string id;
            public GameObject prefab;
        }

        public List<Entry> entries = new List<Entry>();

        public GameObject Find(string id)
        {
            if (string.IsNullOrEmpty(id) || entries == null) return null;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i] != null && entries[i].id == id) return entries[i].prefab;
            }
            return null;
        }
    }
}
