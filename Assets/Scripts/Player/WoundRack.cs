namespace OutpostZero.Player
{
    /// <summary>
    /// A camp wound slows a reload and the next machete swing.
    /// A gun still cycles at the rate on its card.
    /// </summary>
    public static class WoundRack
    {
        public const float Bite = 1.2f;
        public const float Fever = 1.45f;
        public const float Critical = 1.8f;

        public static float Scale(int injury)
        {
            if (injury <= 0) return 1f;
            if (injury == 1) return Bite;
            if (injury == 2) return Fever;
            return Critical;
        }

        public static float Reload(float seconds, int skill, int injury)
        {
            if (seconds < 0f) seconds = 0f;
            float practiced = seconds
                * OutpostZero.Colony.FieldHand.Reload(skill)
                * OutpostZero.Colony.HandDepth.Reload(skill);
            return practiced * Scale(injury);
        }

        public static float Swing(float gap, int injury, OutpostZero.Core.WeaponType type)
        {
            if (gap < 0f) gap = 0f;
            if (type != OutpostZero.Core.WeaponType.Melee) return gap;
            return gap * Scale(injury);
        }

        public static string Line(int injury, string language)
        {
            if (injury <= 0) return "";
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T("rack.wound");
            return OutpostZero.Shell.Loc.T("rack.wound", language);
        }
    }
}
