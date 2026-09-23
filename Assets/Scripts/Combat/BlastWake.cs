namespace OutpostZero.Combat
{
    /// <summary>
    /// What a barrel leaves after the flash. Powder throws a ring, smoke, and ground fire.
    /// A toxic barrel leaves a cloud. Oil leaves a slick that burns.
    /// The blast damage stays on the barrel.
    /// </summary>
    public static class BlastWake
    {
        public const float Smoke = 2.4f;
        public const float Fire = 3.2f;
        public const float Cloud = 4.5f;
        public const float Slick = 6f;
        public const float RingTime = 0.28f;
        public const float Burn = 0.32f;
        public const float Haze = 0.26f;

        public static string Wake(HazardKind kind)
        {
            if (kind == HazardKind.Toxic) return "cloud";
            if (kind == HazardKind.Oil) return "slick";
            return "fire";
        }

        public static float Hold(HazardKind kind)
        {
            if (kind == HazardKind.Toxic) return Cloud;
            if (kind == HazardKind.Oil) return Slick;
            return Fire;
        }

        public static bool Ring(HazardKind kind)
        {
            return kind == HazardKind.Explosive;
        }

        public static bool Smokes(HazardKind kind)
        {
            return kind == HazardKind.Explosive;
        }

        public static string Sound(HazardKind kind)
        {
            return kind == HazardKind.Toxic ? "cloud" : "burn";
        }

        public static float Volume(HazardKind kind)
        {
            return kind == HazardKind.Toxic ? Haze : Burn;
        }
    }
}
