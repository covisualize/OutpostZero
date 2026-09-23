using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A shotgun pellet is full inside four meters and fades to a third of that at the end of the gun's range.
    /// A rifle, an SMG, and a pistol keep the damage they left the barrel with.
    /// </summary>
    public static class PelletDrop
    {
        public const float Full = 4f;
        public const float Floor = 0.35f;

        public static float Scale(float distance, float range, WeaponType type)
        {
            if (type != WeaponType.Shotgun) return 1f;
            if (distance < 0f) distance = 0f;
            if (distance <= Full) return 1f;
            if (range <= Full) return Floor;
            float t = (distance - Full) / (range - Full);
            if (t > 1f) t = 1f;
            return 1f - (1f - Floor) * t;
        }

        public static float Damage(float baseDamage, float distance, float range, WeaponType type)
        {
            if (baseDamage < 0f) baseDamage = 0f;
            return baseDamage * Scale(distance, range, type);
        }
    }
}
