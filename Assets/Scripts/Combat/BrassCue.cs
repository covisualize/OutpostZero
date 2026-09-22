using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A fired casing waits, then clinks. A rifle tracer shows on every third round.
    /// A pistol and a shotgun still draw a tracer on each shot. A melee swing ejects nothing.
    /// </summary>
    public static class BrassCue
    {
        public const float Delay = 0.35f;
        public const float Clink = 0.22f;
        public const float Clack = 0.34f;
        public const int RifleEvery = 3;

        public static bool Ejects(WeaponType type)
        {
            return type == WeaponType.Pistol || type == WeaponType.Shotgun || type == WeaponType.Rifle || type == WeaponType.SMG;
        }

        public static string Sound(WeaponType type)
        {
            if (!Ejects(type)) return "";
            return type == WeaponType.Shotgun ? "clack" : "clink";
        }

        public static float Volume(WeaponType type)
        {
            if (!Ejects(type)) return 0f;
            return type == WeaponType.Shotgun ? Clack : Clink;
        }

        public static bool Due(float now, float ejectedAt)
        {
            if (ejectedAt <= 0f) return false;
            if (now < ejectedAt) return false;
            return now - ejectedAt >= Delay;
        }

        public static bool Tracer(WeaponType type, int round)
        {
            if (type != WeaponType.Rifle && type != WeaponType.SMG) return true;
            if (round <= 0) return false;
            return round % RifleEvery == 0;
        }
    }
}
