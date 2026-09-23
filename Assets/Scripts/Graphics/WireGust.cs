namespace OutpostZero.Graphics
{
    /// <summary>
    /// A wire bows with the wind. A clear day keeps the small sway the line already had.
    /// </summary>
    public static class WireGust
    {
        public const float Base = 0.12f;
        public const float Clear = 0.08f;
        public const float Rate = 0.65f;

        public static float Side(float wind, float time)
        {
            if (wind < 0f) wind = 0f;
            float scale = wind / Clear;
            if (scale > 12f) scale = 12f;
            double wave = System.Math.Sin(time * Rate);
            return (float)(wave * Base * scale);
        }
    }
}
