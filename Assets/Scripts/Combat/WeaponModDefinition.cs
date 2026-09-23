using UnityEngine;

namespace OutpostZero.Combat
{
    /// <summary>One weapon mod's numbers: damage, noise and spread multipliers, magazine bonus, and whether it quiets the report.</summary>
    [CreateAssetMenu(menuName = "Outpost Zero/Weapon Mod", fileName = "WeaponMod")]
    public class WeaponModDefinition : ScriptableObject
    {
        [Tooltip("Mod id (suppressor, optic, extended_mag, rail), also the crafted item id and the asset name.")]
        public string id = "";
        [Tooltip("Damage multiplier.")]
        [Min(0f)] public float damage = 1f;
        [Tooltip("Multiplier on the gun's noise radius in metres.")]
        [Min(0f)] public float noise = 1f;
        [Tooltip("Multiplier on the gun's spread in degrees.")]
        [Min(0f)] public float spread = 1f;
        [Tooltip("Rounds added to the magazine.")]
        [Min(0)] public int magazineBonus;
        [Tooltip("Turns a loud report into a quiet one, so the spawner's loud-shot call never hears it.")]
        public bool quiet;

        public WeaponModTable.Row ToRow()
        {
            return new WeaponModTable.Row
            {
                Id = id,
                Damage = damage,
                Noise = noise,
                Spread = spread,
                MagazineBonus = magazineBonus,
                Quiet = quiet
            };
        }

        public void CopyFrom(WeaponModTable.Row row)
        {
            id = row.Id;
            damage = row.Damage;
            noise = row.Noise;
            spread = row.Spread;
            magazineBonus = row.MagazineBonus;
            quiet = row.Quiet;
        }
    }
}
