namespace OutpostZero.Combat
{
    /// <summary>
    /// A machete swing spends stamina. A gun does not. A short breath cannot start the swing.
    /// </summary>
    public static class SwingCost
    {
        public const float Melee = 8f;

        public static float Of(WeaponType type)
        {
            return type == WeaponType.Melee ? Melee : 0f;
        }

        public static bool Pays(float stamina, WeaponType type)
        {
            float cost = Of(type);
            if (cost <= 0f) return true;
            if (stamina < 0f) stamina = 0f;
            return stamina >= cost;
        }

        public static float After(float stamina, WeaponType type)
        {
            if (stamina < 0f) stamina = 0f;
            float cost = Of(type);
            if (cost <= 0f) return stamina;
            float next = stamina - cost;
            return next < 0f ? 0f : next;
        }

        public static string Line(string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T("swing.tired");
            return OutpostZero.Shell.Loc.T("swing.tired", language);
        }
    }
}
