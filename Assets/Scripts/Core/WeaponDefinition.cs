using UnityEngine;

namespace OutpostZero.Core
{
    public enum ZombieSpecialAbility
    {
        None,
        Lunge,
        Charge
    }

    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Outpost Zero/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        public string id = "weapon";
        public string displayName = "Weapon";
        public WeaponType weaponType = WeaponType.Pistol;
        public float baseDamage = 25f;
        public float attackRate = 2f;
        public float range = 25f;
        public float spreadAngle = 2.5f;
        public int projectilesPerShot = 1;
        public int maxMagazine = 12;
        public int reserveAmmo = 60;
        public float reloadDuration = 1.8f;
        public float noiseRadius = 15f;
        public float noiseIntensity = 1f;
        public NoiseType noiseType = NoiseType.GunshotQuiet;
        public float staminaCost = 0f;
        public bool isMelee;
        public bool automatic;
        public bool useProjectile;
        public string modelPath;
    }
}
