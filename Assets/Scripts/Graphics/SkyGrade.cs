using UnityEngine;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Afternoon stays bright. Full night is a 0.15 moon and a deep blue ambient.
    /// The street also darkens once seventy percent of the expedition quota is in.
    /// </summary>
    public static class SkyGrade
    {
        public const float DaySun = 1.15f;
        public const float Moon = 0.15f;
        public const float DayExposure = 1.05f;
        public const float NightExposure = 0.35f;
        public const float DuskAt = 0.70f;

        public static readonly Color DaySunColor = new Color(1f, 0.96f, 0.9f);
        public static readonly Color MoonColor = new Color(0.45f, 0.55f, 0.85f);
        public static readonly Color DaySky = new Color(0.62f, 0.66f, 0.74f);
        public static readonly Color DayEquator = new Color(0.36f, 0.38f, 0.44f);
        public static readonly Color DayGround = new Color(0.18f, 0.17f, 0.16f);
        public static readonly Color NightSky = new Color(0.03f, 0.05f, 0.08f);
        public static readonly Color NightEquator = new Color(0.02f, 0.03f, 0.05f);
        public static readonly Color NightGround = new Color(0.01f, 0.01f, 0.02f);

        public static float ClampNight(float night)
        {
            if (night < 0f) return 0f;
            if (night > 1f) return 1f;
            return night;
        }

        public static float Sun(float night)
        {
            float t = ClampNight(night);
            return DaySun + (Moon - DaySun) * t;
        }

        public static Color SunTint(float night)
        {
            return Color.Lerp(DaySunColor, MoonColor, ClampNight(night));
        }

        public static Color Sky(float night)
        {
            return Color.Lerp(DaySky, NightSky, ClampNight(night));
        }

        public static Color Equator(float night)
        {
            return Color.Lerp(DayEquator, NightEquator, ClampNight(night));
        }

        public static Color Ground(float night)
        {
            return Color.Lerp(DayGround, NightGround, ClampNight(night));
        }

        public static float Exposure(float night)
        {
            float t = ClampNight(night);
            return DayExposure + (NightExposure - DayExposure) * t;
        }

        public static float Ratio(float done, float goal)
        {
            if (goal <= 0f) return 0f;
            if (done < 0f) done = 0f;
            float t = done / goal;
            if (t > 1f) return 1f;
            return t;
        }

        public static float Job(float kills, float goalKills, float scrap, float goalScrap)
        {
            return (Ratio(kills, goalKills) + Ratio(scrap, goalScrap)) * 0.5f;
        }

        public static float JobNight(float progress)
        {
            if (progress <= DuskAt) return 0f;
            if (progress >= 1f) return 1f;
            return (progress - DuskAt) / (1f - DuskAt);
        }
    }
}
