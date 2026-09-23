using UnityEngine;

namespace OutpostZero.Core
{
    /// <summary>
    /// Every weapon definition, loaded from Resources so a weapon built at runtime (a loadout
    /// card, a purchase, the rifle top-up) can find its held model and grip.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponSet", menuName = "Outpost Zero/Weapon Set")]
    public class WeaponSet : ScriptableObject
    {
        public const string ResourcePath = "WeaponSet";

        /// <summary>The weapon definitions under Assets/Data/Weapons.</summary>
        [Tooltip("The weapon definitions under Assets/Data/Weapons.")]
        public WeaponDefinition[] weapons = new WeaponDefinition[0];

        private static WeaponSet loaded;

        public static WeaponDefinition Find(string id, WeaponType type)
        {
            if (loaded == null) loaded = Resources.Load<WeaponSet>(ResourcePath);
            return loaded != null ? FindIn(loaded.weapons, id, type) : null;
        }

        /// <summary>The definition with this id, else the first of this type; null when neither exists.</summary>
        public static WeaponDefinition FindIn(WeaponDefinition[] weapons, string id, WeaponType type)
        {
            if (weapons == null) return null;
            WeaponDefinition sameType = null;
            for (int i = 0; i < weapons.Length; i++)
            {
                var weapon = weapons[i];
                if (weapon == null) continue;
                if (!string.IsNullOrEmpty(id) && weapon.id == id) return weapon;
                if (sameType == null && weapon.weaponType == type) sameType = weapon;
            }
            return sameType;
        }
    }
}
