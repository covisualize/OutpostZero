namespace OutpostZero.Expedition
{
    /// <summary>
    /// A bite on the street slows the person following you.
    /// At three wounds they stop swinging and can barely keep up.
    /// </summary>
    public static class FollowLimp
    {
        public const float Bite = 0.85f;
        public const float Fever = 0.62f;
        public const float Critical = 0.4f;
        public const int Still = 3;

        public static float Pace(float speed, int injury)
        {
            if (speed < 0f) speed = 0f;
            if (injury <= 0) return speed;
            if (injury == 1) return speed * Bite;
            if (injury == 2) return speed * Fever;
            return speed * Critical;
        }

        public static bool Swings(int injury)
        {
            return injury < Still;
        }

        public static string Line(string name, string language)
        {
            return OutpostZero.Shell.StreetAsk.Drag(name, language);
        }
    }
}
