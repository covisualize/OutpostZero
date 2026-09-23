using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The baked base-kit prefab each finished module wears. A kind with no entry, or a site still going up,
    /// keeps the plain box.
    /// </summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Module Looks", fileName = "ModuleLooks")]
    public class ModuleLooks : ScriptableObject
    {
        public const string ResourcePath = "ModuleLooks";

        [System.Serializable]
        public struct Look
        {
            [Tooltip("ModuleKind name, e.g. Generator.")]
            public string kind;
            [Tooltip("Prefab under Assets/Prefabs placed under the module's collider root.")]
            public GameObject prefab;
        }

        public Look[] looks = new Look[0];

        private static ModuleLooks loaded;
        private static bool looked;

        public static GameObject For(string kind)
        {
            if (!looked)
            {
                looked = true;
                loaded = Resources.Load<ModuleLooks>(ResourcePath);
            }
            return loaded != null ? Find(loaded.looks, kind) : null;
        }

        public static GameObject Find(Look[] looks, string kind)
        {
            if (looks == null || string.IsNullOrEmpty(kind)) return null;
            for (int i = 0; i < looks.Length; i++)
                if (looks[i].kind == kind) return looks[i].prefab;
            return null;
        }
    }
}
