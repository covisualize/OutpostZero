using UnityEngine;
using OutpostZero.Core;

namespace OutpostZero.Items
{
    /// <summary>A box of rounds: which weapon it feeds and how many rounds one pickup adds to the reserve.</summary>
    [CreateAssetMenu(fileName = "AmmoDefinition", menuName = "Outpost Zero/Ammo Definition")]
    public class AmmoDefinition : ItemDefinition
    {
        [Tooltip("Weapon family whose reserve this ammo fills.")]
        public WeaponType weapon = WeaponType.Pistol;
        [Tooltip("Rounds added to that reserve per unit picked up.")]
        public int rounds = 12;

        public override ItemRecord ToRecord()
        {
            var record = base.ToRecord();
            record.AmmoType = weapon;
            record.AmmoAmount = rounds;
            return record;
        }

        public override void CopyFrom(ItemRecord record)
        {
            base.CopyFrom(record);
            if (record == null) return;
            weapon = record.AmmoType;
            rounds = record.AmmoAmount;
        }
    }
}
