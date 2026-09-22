namespace OutpostZero.Graphics
{
    /// <summary>
    /// A raid pulls the yard off the daytime grade: colder, darker, a heavier vignette, and a red pulse at the gate.
    /// </summary>
    public static class RaidGrade
    {
        public static float Exposure(float stored, bool raid)
        {
            float level = Presentation.Exposure(stored);
            if (!raid) return level;
            float next = level - 0.35f;
            if (next < -0.45f) return -0.45f;
            return next;
        }

        public static void Filter(bool raid, out float red, out float green, out float blue)
        {
            if (!raid)
            {
                red = 1f;
                green = 0.96f;
                blue = 0.9f;
                return;
            }
            red = 0.72f;
            green = 0.58f;
            blue = 0.78f;
        }

        public static float Vignette(bool raid, int tier)
        {
            float day = tier <= 0 ? 0.16f : 0.28f;
            if (!raid) return day;
            float next = day + 0.18f;
            if (next > 0.62f) return 0.62f;
            return next;
        }

        public static float Pulse(float time)
        {
            float wave = (float)System.Math.Sin(time * 3.2);
            return 1.4f + wave * 0.7f;
        }
    }
}
