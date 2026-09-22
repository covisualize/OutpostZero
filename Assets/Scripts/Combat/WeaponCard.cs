using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// The guns a street can hand you. Numbers match the weapon definitions.
    /// </summary>
    public static class WeaponCard
    {
        public struct Spec
        {
            public string Id;
            public string Name;
            public WeaponType Type;
            public float Damage;
            public float Rate;
            public float Range;
            public float Spread;
            public int Pellets;
            public int Magazine;
            public int Reserve;
            public float Reload;
            public float Noise;
            public NoiseType NoiseKind;
            public bool Automatic;
            public bool Projectile;
            public bool Melee;
        }

        public static bool FiresAutomatic(WeaponType type, bool flagged)
        {
            if (flagged) return true;
            return type == WeaponType.Rifle || type == WeaponType.SMG;
        }

        public static bool FiresProjectile(WeaponType type, bool flagged)
        {
            if (flagged) return true;
            return type == WeaponType.Rifle || type == WeaponType.Shotgun || type == WeaponType.SMG;
        }

        public static string IdFor(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Pistol: return "pistol_9mm";
                case WeaponType.Shotgun: return "shotgun_pump";
                case WeaponType.Rifle: return "rifle_assault";
                case WeaponType.SMG: return "smg";
                case WeaponType.Melee: return "machete";
                default: return "";
            }
        }

        public static Spec Find(string id)
        {
            if (id == "pistol_9mm") return Gun("pistol_9mm", "Tactical 9mm Pistol", WeaponType.Pistol, 34f, 3.2f, 25f, 2.5f, 1, 12, 60, 1.8f, 20f, NoiseType.GunshotQuiet, false, false);
            if (id == "shotgun_pump") return Gun("shotgun_pump", "Remington 870 Shotgun", WeaponType.Shotgun, 19f, 1.1f, 16f, 8.5f, 7, 6, 24, 2.4f, 38f, NoiseType.GunshotLoud, false, true);
            if (id == "rifle_assault") return Gun("rifle_assault", "Assault Rifle", WeaponType.Rifle, 26f, 9f, 32f, 3f, 1, 30, 90, 2.1f, 34f, NoiseType.GunshotLoud, true, true);
            if (id == "smg") return Gun("smg", "Compact SMG", WeaponType.SMG, 16f, 14f, 22f, 5.5f, 1, 25, 75, 1.6f, 18f, NoiseType.GunshotLoud, true, true);
            if (id == "machete")
            {
                return new Spec
                {
                    Id = "machete",
                    Name = "Steel Machete",
                    Type = WeaponType.Melee,
                    Damage = 48f,
                    Rate = 1.8f,
                    Range = 1.9f,
                    Pellets = 1,
                    Noise = 2f,
                    NoiseKind = NoiseType.MeleeSwing,
                    Melee = true
                };
            }
            return new Spec();
        }

        private static Spec Gun(string id, string name, WeaponType type, float damage, float rate, float range, float spread, int pellets, int magazine, int reserve, float reload, float noise, NoiseType kind, bool automatic, bool projectile)
        {
            return new Spec
            {
                Id = id,
                Name = name,
                Type = type,
                Damage = damage,
                Rate = rate,
                Range = range,
                Spread = spread,
                Pellets = pellets,
                Magazine = magazine,
                Reserve = reserve,
                Reload = reload,
                Noise = noise,
                NoiseKind = kind,
                Automatic = automatic,
                Projectile = projectile
            };
        }
    }
}
