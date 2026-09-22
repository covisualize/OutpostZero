using UnityEngine;

namespace OutpostZero.Shell
{
    /// <summary>
    /// The HUD keeps its red and green until a colorblind mode is on.
    /// Blue-yellow splits danger from safety. Mono drops the hue.
    /// </summary>
    public static class HudPalette
    {
        public static string Name(int mode)
        {
            if (mode == 1) return "set.vision1";
            if (mode == 2) return "set.vision2";
            return "set.vision0";
        }

        public static Color Health(int mode)
        {
            if (mode == 1) return new Color(0.25f, 0.55f, 0.95f);
            if (mode == 2) return new Color(0.92f, 0.92f, 0.92f);
            return new Color(0.75f, 0.2f, 0.16f);
        }

        public static Color Ghost(int mode)
        {
            if (mode == 1) return new Color(0.12f, 0.28f, 0.55f);
            if (mode == 2) return new Color(0.45f, 0.45f, 0.45f);
            return new Color(0.45f, 0.18f, 0.14f);
        }

        public static Color Safe(int mode)
        {
            if (mode == 1) return new Color(0.95f, 0.85f, 0.2f);
            if (mode == 2) return new Color(0.55f, 0.55f, 0.55f);
            return new Color(0.35f, 0.62f, 0.38f);
        }

        public static Color Warn(int mode)
        {
            if (mode == 1) return new Color(0.98f, 0.92f, 0.55f);
            if (mode == 2) return Color.white;
            return new Color(0.95f, 0.55f, 0.25f);
        }

        public static Color Alarm(int mode)
        {
            if (mode == 1) return new Color(0.3f, 0.55f, 1f);
            if (mode == 2) return Color.white;
            return new Color(0.95f, 0.35f, 0.28f);
        }

        public static Color Ask(int mode)
        {
            if (mode == 1) return new Color(0.95f, 0.85f, 0.2f);
            if (mode == 2) return new Color(0.7f, 0.7f, 0.7f);
            return new Color(0.95f, 0.8f, 0.35f);
        }

        public static Color Hurt(int mode)
        {
            if (mode == 1) return new Color(0.08f, 0.16f, 0.4f, 0.82f);
            if (mode == 2) return new Color(0.2f, 0.2f, 0.2f, 0.82f);
            return new Color(0.45f, 0.08f, 0.06f, 0.82f);
        }

        public static Color Crit(int mode)
        {
            if (mode == 1) return new Color(0.3f, 0.7f, 1f);
            if (mode == 2) return Color.white;
            return new Color(1f, 0.85f, 0.2f);
        }
    }
}
