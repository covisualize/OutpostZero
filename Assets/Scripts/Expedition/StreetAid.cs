namespace OutpostZero.Expedition
{
    /// <summary>
    /// A follower who is close enough swings. The cry still carries farther than the swing.
    /// </summary>
    public static class StreetAid
    {
        public const float Reach = 1.6f;
        public const float Gap = 1.35f;
        public const float Damage = 14f;
        public const float Noise = 6f;

        public static bool Due(float last, float now, float nearest)
        {
            if (nearest < 0f || nearest > Reach) return false;
            if (last <= 0f) return true;
            if (now < last) return false;
            return OutpostZero.Core.Tick.Past(now, last, Gap);
        }

        public static string Line(string name, string language)
        {
            return OutpostZero.Shell.StreetAsk.Swing(name, language);
        }
    }
}
