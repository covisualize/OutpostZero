using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A pistol flash is small. A shotgun flash is wide and smokes.
    /// A rifle or SMG strobes a tight bright flash. A blade has none.
    /// </summary>
    public static class MuzzleShape
    {
        public static bool Shows(WeaponType type)
        {
            return type != WeaponType.Melee;
        }

        public static bool Smoke(WeaponType type)
        {
            return type == WeaponType.Shotgun;
        }

        public static bool Strobe(WeaponType type)
        {
            return type == WeaponType.Rifle || type == WeaponType.SMG;
        }

        public static float Scale(WeaponType type)
        {
            if (type == WeaponType.Shotgun) return 0.28f;
            if (type == WeaponType.Rifle || type == WeaponType.SMG) return 0.08f;
            return 0.12f;
        }

        public static float Range(WeaponType type)
        {
            if (type == WeaponType.Shotgun) return 6.5f;
            if (type == WeaponType.Rifle || type == WeaponType.SMG) return 5.5f;
            return 3.2f;
        }

        public static float Intensity(WeaponType type)
        {
            if (type == WeaponType.Shotgun) return 4.2f;
            if (type == WeaponType.Rifle || type == WeaponType.SMG) return 5f;
            return 2.4f;
        }

        public static float Hold(WeaponType type)
        {
            if (type == WeaponType.Shotgun) return 0.08f;
            if (type == WeaponType.Rifle || type == WeaponType.SMG) return 0.03f;
            return 0.05f;
        }
    }
}
