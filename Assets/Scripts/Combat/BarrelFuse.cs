namespace OutpostZero.Combat
{
    /// <summary>
    /// A hit that does not finish a powder or toxic barrel starts a fuse.
    /// The barrel hisses until the fuse runs out, then it goes. Oil waits for the breaking hit.
    /// </summary>
    public static class BarrelFuse
    {
        public const float Cook = 1.4f;
        public const float Leak = 0.8f;
        public const float HissGap = 0.35f;

        public static bool Arms(HazardKind kind)
        {
            return kind == HazardKind.Explosive || kind == HazardKind.Toxic;
        }

        public static float Length(HazardKind kind)
        {
            if (kind == HazardKind.Explosive) return Cook;
            if (kind == HazardKind.Toxic) return Leak;
            return 0f;
        }

        public static bool Due(float started, float now, float length)
        {
            if (length <= 0f || started <= 0f) return false;
            return now - started >= length;
        }

        public static bool HissDue(float last, float now)
        {
            if (last <= 0f) return true;
            return now - last >= HissGap;
        }
    }
}
