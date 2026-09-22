namespace OutpostZero.Graphics
{
    /// <summary>
    /// Upward faces take a dirt coat. Wet faces add a ripple on top of that.
    /// A wall stays the colour it already had.
    /// </summary>
    public static class StreetCoat
    {
        public const float DirtStart = 0.65f;
        public const float DirtSpan = 0.35f;
        public const float Dirt = 0.35f;
        public const float Ripple = 0.08f;

        public static float Cover(float normalY)
        {
            if (normalY <= DirtStart) return 0f;
            float t = (normalY - DirtStart) / DirtSpan;
            if (t > 1f) t = 1f;
            return t * Dirt;
        }

        public static float Shimmer(float wet, float wave)
        {
            if (wet < 0f) wet = 0f;
            if (wet > 1f) wet = 1f;
            if (wave < 0f) wave = 0f;
            if (wave > 1f) wave = 1f;
            return wet * wave * Ripple;
        }
    }
}
