namespace OutpostZero.Expedition
{
    /// <summary>
    /// One bullet takes a single hit off a barred door.
    /// A shotgun blast takes two, once per pull, not once per pellet.
    /// </summary>
    public static class BarShot
    {
        public const int Bullet = 1;
        public const int Blast = 2;

        public static int Hits(OutpostZero.Core.WeaponType type)
        {
            if (type == OutpostZero.Core.WeaponType.Shotgun) return Blast;
            if (type == OutpostZero.Core.WeaponType.Pistol
                || type == OutpostZero.Core.WeaponType.Rifle
                || type == OutpostZero.Core.WeaponType.SMG) return Bullet;
            return 0;
        }

        public static int Volley(OutpostZero.Core.WeaponType type, int pellets)
        {
            if (pellets <= 0) return 0;
            return Hits(type);
        }

        public static int After(int hitsLeft, OutpostZero.Core.WeaponType type)
        {
            if (hitsLeft <= 0) return 0;
            int left = hitsLeft;
            int n = Hits(type);
            int i = 0;
            while (DoorBar.Holds(left) && i < n)
            {
                left = DoorBar.After(left);
                i++;
            }
            return left;
        }

        public static bool Opens(int hitsLeft, OutpostZero.Core.WeaponType type)
        {
            if (!DoorBar.Holds(hitsLeft)) return false;
            return !DoorBar.Holds(After(hitsLeft, type));
        }

        public static string Line(string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T("door.shot");
            return OutpostZero.Shell.Loc.T("door.shot", language);
        }
    }
}
