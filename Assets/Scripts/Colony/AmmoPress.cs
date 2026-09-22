namespace OutpostZero.Colony
{
    /// <summary>
    /// A pressed batch fills the gun and also leaves rounds in the camp store.
    /// Rifle feeds the turret and the guards. A single press cannot stock more than 24.
    /// </summary>
    public static class AmmoPress
    {
        public const int Cap = 24;

        public static int Rounds(string id, int batches)
        {
            if (batches < 1 || string.IsNullOrEmpty(id)) return 0;
            int each = 0;
            if (id == "ammo_9mm") each = 4;
            else if (id == "ammo_shells") each = 6;
            else if (id == "ammo_rifle") each = 8;
            else if (id == "ammo_smg") each = 5;
            if (each == 0) return 0;
            int next = each * batches;
            return next > Cap ? Cap : next;
        }
    }
}
