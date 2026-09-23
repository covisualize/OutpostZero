using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// The hurt, poison and camp looks as their own volumes over the base grade. URP blends each
    /// overridden parameter toward the higher volume's value by its weight, so a volume holds the look
    /// at full strength and its weight carries how much of it shows. Order, lowest first:
    /// base 20, aim depth 21, toxic 22, sanctuary 23, damage 24.
    /// </summary>
    public static class GradeLayers
    {
        public const float ToxicPriority = 22f;
        public const float SanctuaryPriority = 23f;
        public const float DamagePriority = 24f;

        /// <summary>Weight change per second as a look fades in or out.</summary>
        public const float FadeRate = 1.5f;

        public const string SanctuaryPath = "PostFX/OutpostZero_Sanctuary";
        public const string ToxicPath = "PostFX/OutpostZero_Toxic";
        public const string DamagePath = "PostFX/OutpostZero_Damage";

        public static float Fade(float weight, bool on, float dt)
        {
            float target = on ? 1f : 0f;
            float step = FadeRate * Mathf.Max(0f, dt);
            if (weight < target) return Mathf.Min(target, weight + step);
            return Mathf.Max(target, weight - step);
        }

        /// <summary>What URP does to one overridden parameter as it passes a volume.</summary>
        public static float Blend(float below, float target, float weight) => below + (target - below) * Mathf.Clamp01(weight);

        public static Color Blend(Color below, Color target, float weight) => Color.Lerp(below, target, Mathf.Clamp01(weight));

        public static Color SanctuaryFilter(Color below)
        {
            ScreenGrade.Warm(true, below.r, below.g, below.b, out float r, out float g, out float b);
            return new Color(r, g, b);
        }

        public static Color ToxicFilter(Color below)
        {
            PoisonVeil.Tint(true, below.r, below.g, below.b, out float r, out float g, out float b);
            return new Color(r, g, b);
        }

        public static float ToxicVignette(float below) => PoisonVeil.Shade(below, true);

        /// <summary>The damage volume's weight is the hurt amount itself, so its values are the full-hurt look.</summary>
        public static float DamageVignette(float below) => ScreenGrade.Vignette(below, 1f);

        public static float DamageSaturation(float death) => ScreenGrade.Saturation(1f, death);

        public static Color DamageEdge => ScreenGrade.VignetteColor(1f);
    }
}
