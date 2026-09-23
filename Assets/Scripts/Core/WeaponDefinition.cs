using UnityEngine;

namespace OutpostZero.Core
{
    public enum ZombieSpecialAbility
    {
        None,
        Lunge,
        Charge
    }

    /// <summary>
    /// Tuning for one weapon. WeaponBase and FirearmWeapon copy these values when the definition is applied.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Outpost Zero/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        /// <summary>Stable id used by saves, loot tables and mods.</summary>
        [Tooltip("Stable id used by saves, loot tables and mods.")]
        public string id = "weapon";
        /// <summary>Name shown on the HUD and in the inventory.</summary>
        [Tooltip("Name shown on the HUD and in the inventory.")]
        public string displayName = "Weapon";
        /// <summary>Slot and ammo family this weapon uses.</summary>
        [Tooltip("Slot and ammo family this weapon uses.")]
        public WeaponType weaponType = WeaponType.Pistol;
        /// <summary>Damage per hit, or per pellet for shotguns, before armor.</summary>
        [Tooltip("Damage per hit, or per pellet for shotguns, before armor.")]
        public float baseDamage = 25f;
        /// <summary>Attacks per second. Values below 0.1 are raised to 0.1.</summary>
        [Tooltip("Attacks per second. Values below 0.1 are raised to 0.1.")]
        public float attackRate = 2f;
        /// <summary>Maximum hit distance in metres.</summary>
        [Tooltip("Maximum hit distance in metres.")]
        public float range = 25f;
        /// <summary>Cone half-angle in degrees for firearm spread.</summary>
        [Tooltip("Cone half-angle in degrees for firearm spread.")]
        public float spreadAngle = 2.5f;
        /// <summary>Pellets fired per trigger pull.</summary>
        [Tooltip("Pellets fired per trigger pull.")]
        public int projectilesPerShot = 1;
        /// <summary>Rounds in a full magazine.</summary>
        [Tooltip("Rounds in a full magazine.")]
        public int maxMagazine = 12;
        /// <summary>Rounds carried in reserve when the weapon is issued.</summary>
        [Tooltip("Rounds carried in reserve when the weapon is issued.")]
        public int reserveAmmo = 60;
        /// <summary>Seconds to reload a full magazine.</summary>
        [Tooltip("Seconds to reload a full magazine.")]
        public float reloadDuration = 1.8f;
        /// <summary>Metres an attack is heard before walls and suppressors.</summary>
        [Tooltip("Metres an attack is heard before walls and suppressors.")]
        public float noiseRadius = 15f;
        /// <summary>Loudness multiplier applied to the noise radius.</summary>
        [Tooltip("Loudness multiplier applied to the noise radius.")]
        public float noiseIntensity = 1f;
        /// <summary>Noise category zombies and the caption system react to.</summary>
        [Tooltip("Noise category zombies and the caption system react to.")]
        public NoiseType noiseType = NoiseType.GunshotQuiet;
        /// <summary>Stamina spent per melee swing.</summary>
        [Tooltip("Stamina spent per melee swing.")]
        public float staminaCost = 0f;
        /// <summary>True for blades and blunt weapons; they swing instead of firing.</summary>
        [Tooltip("True for blades and blunt weapons; they swing instead of firing.")]
        public bool isMelee;
        /// <summary>Holding fire keeps shooting.</summary>
        [Tooltip("Holding fire keeps shooting.")]
        public bool automatic;
        /// <summary>Spawn a travelling bullet instead of an instant raycast.</summary>
        [Tooltip("Spawn a travelling bullet instead of an instant raycast.")]
        public bool useProjectile;
        /// <summary>FBX under Assets/Models used for the held model.</summary>
        [Tooltip("FBX under Assets/Models used for the held model.")]
        public string modelPath;
        /// <summary>Generated prefab shown in the hand; mounted at runtime for weapons picked up or bought.</summary>
        [Tooltip("Generated prefab shown in the hand; mounted at runtime for weapons picked up or bought.")]
        public GameObject heldPrefab;
        /// <summary>Metres from the hand socket to the model's grip, in the socket's space.</summary>
        [Tooltip("Metres from the hand socket to the model's grip, in the socket's space.")]
        public Vector3 holdOffset = Vector3.zero;
        /// <summary>Degrees that turn the model so its barrel or blade points along the aim.</summary>
        [Tooltip("Degrees that turn the model so its barrel or blade points along the aim.")]
        public Vector3 holdEuler = new Vector3(0f, 90f, 0f);
    }
}
