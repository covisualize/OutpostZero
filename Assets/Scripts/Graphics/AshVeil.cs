namespace OutpostZero.Graphics
{
    /// <summary>
    /// Ash over the market shortens a look and greys the grade.
    /// A yard without ash keeps the weather sight it already had.
    /// </summary>
    public static class AshVeil
    {
        public const float Cut = 0.82f;
        public const float Mix = 0.28f;

        public static float Scale(float sight, bool ash)
        {
            if (sight < 0f) sight = 0f;
            if (sight > 1f) sight = 1f;
            if (!ash) return sight;
            return sight * Cut;
        }

        public static void Grit(bool ash, float red, float green, float blue, out float r, out float g, out float b)
        {
            if (!ash)
            {
                r = red;
                g = green;
                b = blue;
                return;
            }
            r = red + (0.55f - red) * Mix;
            g = green + (0.52f - green) * Mix;
            b = blue + (0.48f - blue) * Mix;
        }
    }
}
