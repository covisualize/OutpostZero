namespace OutpostZero.Shell
{
    /// <summary>
    /// The noise meter keeps its color. Colorblind modes add a shape that changes with loudness,
    /// so the reading does not depend on hue. Monochrome also grows the bar.
    /// </summary>
    public static class NoiseCue
    {
        public static float Height(int mode, float noise)
        {
            float level = noise < 0f ? 0f : (noise > 1f ? 1f : noise);
            if (mode == 2) return 8f + level * 10f;
            return 6f;
        }

        public static int Band(float noise)
        {
            float level = noise < 0f ? 0f : (noise > 1f ? 1f : noise);
            if (level < 0.25f) return 0;
            if (level < 0.55f) return 1;
            if (level < 0.8f) return 2;
            return 3;
        }

        public static string Mark(int mode, float noise)
        {
            if (mode != 1 && mode != 2) return "";
            int band = Band(noise);
            if (mode == 1)
            {
                if (band == 0) return ".";
                if (band == 1) return "-";
                if (band == 2) return "=";
                return "#";
            }
            if (band == 0) return "o";
            if (band == 1) return "^";
            if (band == 2) return "[]";
            return "*";
        }
    }
}
