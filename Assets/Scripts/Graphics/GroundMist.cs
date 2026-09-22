using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Fog sits on the street and thickens in bad weather.
    /// A fog day hides the 70 m edge. Clear air stays open.
    /// Rain, fog, and clear sight stay on the weather surface.
    /// </summary>
    public static class GroundMist
    {
        public const float Day = 0.006f;
        public const float Night = 0.014f;
        public const float Edge = 70f;
        public const float Floor = 2.4f;
        public const float Veil = 0.08f;

        public static float Air(float night)
        {
            if (night < 0f) night = 0f;
            if (night > 1f) night = 1f;
            return Day + (Night - Day) * night;
        }

        public static float Sheet(WeatherKind kind)
        {
            if (kind == WeatherKind.Fog) return 0.022f;
            if (kind == WeatherKind.Rain) return 0.012f;
            return 0f;
        }

        public static float Pool(float height)
        {
            if (height >= Floor) return 0f;
            if (height < 0f) height = 0f;
            return (Floor - height) / Floor * 0.01f;
        }

        public static float Density(WeatherKind kind, float night, float height)
        {
            return Air(night) + Sheet(kind) + Pool(height);
        }

        public static float Factor(float density, float distance)
        {
            if (density < 0f) density = 0f;
            if (distance < 0f) distance = 0f;
            float x = density * distance;
            return (float)System.Math.Exp(-(double)(x * x));
        }

        public static bool Hides(float density, float distance)
        {
            return Factor(density, distance) <= Veil;
        }

        public static float Wind(WeatherKind kind)
        {
            if (kind == WeatherKind.Rain) return 0.65f;
            if (kind == WeatherKind.Fog) return 0.25f;
            return 0.08f;
        }

        public static Color Tint(WeatherKind kind, float night)
        {
            if (night < 0f) night = 0f;
            if (night > 1f) night = 1f;
            Color day = kind == WeatherKind.Rain
                ? new Color(0.42f, 0.5f, 0.52f)
                : new Color(0.55f, 0.62f, 0.6f);
            Color dark = new Color(0.05f, 0.07f, 0.12f);
            return Color.Lerp(day, dark, night);
        }
    }

    /// <summary>
    /// Burnt districts drop ash. The market stays a clear expedition.
    /// Rain and fog keep their own beds.
    /// </summary>
    public static class AshFall
    {
        public const int Flakes = 36;
        public const float Life = 4.2f;
        public const float Drift = 1.6f;

        public static bool Falls(string district)
        {
            return district == "ash_market";
        }

        public static string Bed(WeatherKind kind, string district)
        {
            if (kind == WeatherKind.Rain) return "rain";
            if (kind == WeatherKind.Fog) return "wind";
            if (Falls(district)) return "ash";
            return "";
        }
    }
}
