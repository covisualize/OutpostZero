using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Ground-hugging fog drawn by the renderer's full-screen height fog pass.
    /// Density falls off exponentially above street level, so the fog pools
    /// in the road and thins on the rooftops. Bad weather thickens and lifts it.
    /// With the globals unset the pass draws nothing.
    /// </summary>
    public static class HeightFog
    {
        public const string ColorId = "_OzFogColor";
        public const string ParamsId = "_OzFogParams";
        public const string RangeId = "_OzFogRange";

        public const float Start = 6f;
        public const float End = 60f;
        public const float Base = 0f;
        public const float Opacity = 0.85f;
        public const float NightBoost = 0.012f;

        public static float Thickness(WeatherKind kind, float night)
        {
            night = Mathf.Clamp01(night);
            float a = kind == WeatherKind.Fog ? 0.05f
                : kind == WeatherKind.Storm ? 0.035f
                : kind == WeatherKind.Rain ? 0.025f
                : kind == WeatherKind.Overcast ? 0.015f
                : 0.006f;
            return a + NightBoost * night;
        }

        /// <summary>Height in metres over which the density drops by e.</summary>
        public static float Rise(WeatherKind kind)
        {
            if (kind == WeatherKind.Fog) return 6f;
            if (kind == WeatherKind.Storm) return 4f;
            if (kind == WeatherKind.Rain) return 3f;
            if (kind == WeatherKind.Overcast) return 2.5f;
            return 1.8f;
        }

        /// <summary>Optical depth along the view ray. The shader runs the same sum.</summary>
        public static float Depth(float thickness, float rise, float eyeY, float pointY, float distance)
        {
            if (thickness <= 0f || rise <= 0f) return 0f;
            float length = Mathf.Min(Mathf.Max(distance, 0f), End) - Start;
            if (length <= 0f) return 0f;
            float fromEye = Mathf.Exp(-(eyeY - Base) / rise);
            float dy = pointY - eyeY;
            float column = Mathf.Abs(dy) < 0.01f
                ? fromEye
                : rise * (fromEye - Mathf.Exp(-(pointY - Base) / rise)) / dy;
            return thickness * length * column;
        }

        public static float Amount(float thickness, float rise, float eyeY, float pointY, float distance)
        {
            return Opacity * (1f - Mathf.Exp(-Depth(thickness, rise, eyeY, pointY, distance)));
        }

        public static void Push(WeatherKind kind, float night)
        {
            Color tint = GroundMist.Tint(kind, night);
            Shader.SetGlobalColor(ColorId, new Color(tint.r, tint.g, tint.b, Opacity));
            Shader.SetGlobalVector(ParamsId, new Vector4(Thickness(kind, night), Rise(kind), Base, 0f));
            Shader.SetGlobalVector(RangeId, new Vector4(Start, End, 0f, 0f));
        }

        public static void Clear()
        {
            Shader.SetGlobalColor(ColorId, Color.clear);
            Shader.SetGlobalVector(ParamsId, Vector4.zero);
        }
    }
}
