namespace OutpostZero.Combat
{
    /// <summary>
    /// An empty gun clicks. A reload speaks three times: the magazine leaves,
    /// it seats, and the action racks. A loaded gun and a gun already reloading stay quiet.
    /// </summary>
    public static class GunCue
    {
        public static string Click(int ammo, bool reloading)
        {
            if (reloading || ammo > 0) return "";
            return "dry";
        }

        public static string Stage(float fill, int heard)
        {
            int step = fill < 0.33f ? 1 : fill < 0.72f ? 2 : 3;
            if (step <= heard) return "";
            if (step == 1) return "mag_out";
            if (step == 2) return "mag_in";
            return "rack";
        }

        public static int Mark(string stage)
        {
            if (stage == "mag_out") return 1;
            if (stage == "mag_in") return 2;
            if (stage == "rack") return 3;
            return 0;
        }
    }
}
