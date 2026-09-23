using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// The frame's base grade and the layers laid over it: cool-teal shadows, a night lift,
    /// a red, draining edge as the leader's health falls, and a warmer camp.
    /// </summary>
    public static class ScreenGrade
    {
        public const float BaseSaturation = -20f;
        public const float Contrast = 15f;
        public const float Aberration = 0.05f;
        public const float HurtFrom = 0.5f;
        public const float HurtDrain = -30f;
        public const float HurtEdge = 0.22f;

        public static readonly Vector4 Shadows = new Vector4(0.9f, 1.02f, 1.1f, 0f);
        public static readonly Vector4 Midtones = new Vector4(1f, 1f, 1f, 0f);
        public static readonly Vector4 Highlights = new Vector4(1.05f, 1f, 0.94f, 0f);
        public static readonly Color HurtTint = new Color(0.45f, 0.02f, 0.02f);

        /// <summary>0 at or above half health, 1 at zero.</summary>
        public static float Hurt(float fraction)
        {
            if (fraction >= HurtFrom) return 0f;
            if (fraction <= 0f) return 1f;
            return 1f - fraction / HurtFrom;
        }

        public static float Saturation(float hurt, float death)
        {
            float value = BaseSaturation + HurtDrain * Mathf.Clamp01(hurt) + DeathVeil.Saturation(death);
            return value < -100f ? -100f : value;
        }

        public static float Vignette(float baseIntensity, float hurt)
        {
            float value = baseIntensity + HurtEdge * Mathf.Clamp01(hurt);
            return value > 1f ? 1f : value;
        }

        public static Color VignetteColor(float hurt) => Color.Lerp(Color.black, HurtTint, Mathf.Clamp01(hurt));

        /// <summary>Night lifts the blacks toward blue and pulls the midtones down; w is the offset channel.</summary>
        public static Vector4 Lift(float night)
        {
            float n = Mathf.Clamp01(night);
            return new Vector4(1f - 0.06f * n, 1f - 0.02f * n, 1f + 0.08f * n, 0.02f * n);
        }

        public static Vector4 Gamma(float night)
        {
            float n = Mathf.Clamp01(night);
            return new Vector4(1f - 0.04f * n, 1f, 1f + 0.04f * n, -0.08f * n);
        }

        public static Vector4 Gain(float night)
        {
            float n = Mathf.Clamp01(night);
            return new Vector4(1f - 0.05f * n, 1f - 0.02f * n, 1f + 0.03f * n, 0f);
        }

        /// <summary>Camp reads warmer than the street: the filter leans amber.</summary>
        public static void Warm(bool camp, float red, float green, float blue, out float r, out float g, out float b)
        {
            r = red;
            g = green;
            b = blue;
            if (!camp) return;
            r = Mathf.Min(1f, red * 1.04f);
            g = green * 0.98f;
            b = blue * 0.88f;
        }

        public static bool AberrationOn(int tier) => tier >= 1;
    }
}
