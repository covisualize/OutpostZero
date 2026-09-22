namespace OutpostZero.Player
{
    /// <summary>
    /// A camp wound opens the shot group. A clear leader keeps the sights they already had.
    /// </summary>
    public static class WoundSway
    {
        public const float Bite = 1.12f;
        public const float Fever = 1.35f;
        public const float Critical = 1.6f;

        public static float Angle(float spread, int injury)
        {
            if (spread < 0f) spread = 0f;
            if (injury <= 0) return spread;
            float scale = injury == 1 ? Bite : injury == 2 ? Fever : Critical;
            return spread * scale;
        }
    }
}
