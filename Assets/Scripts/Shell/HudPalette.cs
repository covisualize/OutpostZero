using UnityEngine;

namespace OutpostZero.Shell
{
    /// <summary>
    /// The HUD keeps its red and green until a colorblind mode is on.
    /// Blue-yellow (1) serves deuteranopia and protanopia, red-teal (3) serves tritanopia,
    /// and mono (2) drops the hue. Mode numbers stay stable so old settings files keep their choice.
    /// </summary>
    public static class HudPalette
    {
        public const int Count = 4;
        public const int Tritan = 3;
        public static readonly int[] Order = { 0, 1, Tritan, 2 };

        public static int Next(int mode)
        {
            for (int i = 0; i < Order.Length; i++)
                if (Order[i] == mode) return Order[(i + 1) % Order.Length];
            return 0;
        }

        public static int Clamp(int mode) => mode < 0 || mode >= Count ? 0 : mode;

        public static string Name(int mode)
        {
            if (mode == 1) return "set.vision1";
            if (mode == 2) return "set.vision2";
            if (mode == Tritan) return "set.vision3";
            return "set.vision0";
        }

        public static Color Noise(int mode, float level)
        {
            float t = level < 0f ? 0f : (level > 1f ? 1f : level);
            if (mode == 1) return Color.Lerp(new Color(0.2f, 0.45f, 0.95f), new Color(0.95f, 0.85f, 0.15f), t);
            if (mode == 2) return Color.Lerp(new Color(0.1f, 0.1f, 0.1f), Color.white, t);
            if (mode == Tritan) return Color.Lerp(new Color(0.15f, 0.7f, 0.7f), new Color(0.95f, 0.2f, 0.35f), t);
            return Color.Lerp(new Color(0.2f, 0.7f, 0.3f), new Color(0.8f, 0.15f, 0.1f), t);
        }

        public static Color Health(int mode)
        {
            if (mode == Tritan) return new Color(0.9f, 0.22f, 0.32f);
            if (mode == 1) return new Color(0.25f, 0.55f, 0.95f);
            if (mode == 2) return new Color(0.92f, 0.92f, 0.92f);
            return new Color(0.75f, 0.2f, 0.16f);
        }

        public static Color Ghost(int mode)
        {
            if (mode == Tritan) return new Color(0.45f, 0.12f, 0.18f);
            if (mode == 1) return new Color(0.12f, 0.28f, 0.55f);
            if (mode == 2) return new Color(0.45f, 0.45f, 0.45f);
            return new Color(0.45f, 0.18f, 0.14f);
        }

        public static Color Safe(int mode)
        {
            if (mode == Tritan) return new Color(0.2f, 0.72f, 0.72f);
            if (mode == 1) return new Color(0.95f, 0.85f, 0.2f);
            if (mode == 2) return new Color(0.55f, 0.55f, 0.55f);
            return new Color(0.35f, 0.62f, 0.38f);
        }

        public static Color Warn(int mode)
        {
            if (mode == Tritan) return new Color(1f, 0.6f, 0.7f);
            if (mode == 1) return new Color(0.98f, 0.92f, 0.55f);
            if (mode == 2) return Color.white;
            return new Color(0.95f, 0.55f, 0.25f);
        }

        public static Color Alarm(int mode)
        {
            if (mode == Tritan) return new Color(1f, 0.25f, 0.4f);
            if (mode == 1) return new Color(0.3f, 0.55f, 1f);
            if (mode == 2) return Color.white;
            return new Color(0.95f, 0.35f, 0.28f);
        }

        public static Color Ask(int mode)
        {
            if (mode == Tritan) return new Color(0.45f, 0.85f, 0.85f);
            if (mode == 1) return new Color(0.95f, 0.85f, 0.2f);
            if (mode == 2) return new Color(0.7f, 0.7f, 0.7f);
            return new Color(0.95f, 0.8f, 0.35f);
        }

        public static Color Hurt(int mode)
        {
            if (mode == Tritan) return new Color(0.42f, 0.05f, 0.12f, 0.82f);
            if (mode == 1) return new Color(0.08f, 0.16f, 0.4f, 0.82f);
            if (mode == 2) return new Color(0.2f, 0.2f, 0.2f, 0.82f);
            return new Color(0.45f, 0.08f, 0.06f, 0.82f);
        }

        public static Color Crit(int mode)
        {
            if (mode == Tritan) return new Color(0.3f, 1f, 0.95f);
            if (mode == 1) return new Color(0.3f, 0.7f, 1f);
            if (mode == 2) return Color.white;
            return new Color(1f, 0.85f, 0.2f);
        }
    }
}
