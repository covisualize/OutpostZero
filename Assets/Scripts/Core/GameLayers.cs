using UnityEngine;

namespace OutpostZero.Core
{
    /// <summary>
    /// Named physics layers (TagManager user layers 6–11) and the masks combat uses.
    /// A serialized LayerMask of 0 means Nothing, so weapons and vision fall back to these.
    /// </summary>
    public static class GameLayers
    {
        public const string PlayerName = "Player";
        public const string EnemyName = "Enemy";
        public const string EnvironmentName = "Environment";
        public const string LootName = "Loot";
        public const string ProjectileName = "Projectile";
        public const string InteractableName = "Interactable";

        public const int Player = 6;
        public const int Enemy = 7;
        public const int Environment = 8;
        public const int Loot = 9;
        public const int Projectile = 10;
        public const int Interactable = 11;

        public static int PlayerMask => 1 << Player;
        public static int EnemyMask => 1 << Enemy;
        public static int EnvironmentMask => 1 << Environment;
        public static int LootMask => 1 << Loot;
        public static int ProjectileMask => 1 << Projectile;
        public static int InteractableMask => 1 << Interactable;

        /// <summary>Bullets and melee stop on zombies and solid world geometry.</summary>
        public static LayerMask WeaponHitMask => EnemyMask | EnvironmentMask;

        /// <summary>Walls and props block zombie line of sight. Actors are not occluders.</summary>
        public static LayerMask VisionOcclusionMask => EnvironmentMask;

        public static LayerMask Resolve(LayerMask current, LayerMask fallback)
        {
            return current.value == 0 ? fallback : current;
        }

        public static void ApplyRecursively(GameObject root, int layer)
        {
            if (root == null) return;
            root.layer = layer;
            foreach (Transform child in root.transform)
            {
                ApplyRecursively(child.gameObject, layer);
            }
        }
    }
}
