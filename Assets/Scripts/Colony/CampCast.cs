using UnityEngine;

namespace OutpostZero.Colony
{
    /// <summary>
    /// The prefab each colonist wears in the yard. With no asset or no prefab the yard falls back to capsules.
    /// </summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Camp Cast", fileName = "CampCast")]
    public class CampCast : ScriptableObject
    {
        public const string ResourcePath = "CampCast";

        [Tooltip("Model prefab for colonists in the yard; Assets/Prefabs/Characters/Colonist_Survivor.prefab.")]
        public GameObject mate;

        private static CampCast loaded;
        private static bool looked;

        public static GameObject Mate
        {
            get
            {
                if (!looked)
                {
                    looked = true;
                    loaded = Resources.Load<CampCast>(ResourcePath);
                }
                return loaded != null ? loaded.mate : null;
            }
        }
    }
}
