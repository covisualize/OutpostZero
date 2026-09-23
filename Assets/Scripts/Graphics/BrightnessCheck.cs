using System;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// The brightness calibration strip: three near-black marks lit by the same post exposure the slider drives.
    /// The UI overlay skips post, so each mark is shaded here instead of by the camera.
    /// </summary>
    public static class BrightnessCheck
    {
        /// <summary>Linear scene values; the faintest should only just show at the right setting.</summary>
        public static readonly float[] Marks = { 0.0024f, 0.0030f, 0.0060f };

        /// <summary>sRGB level below which a mark on black reads as black on a typical panel.</summary>
        public const float SeenFloor = 0.04f;

        public const int Target = 2;

        public static float Shade(float linear, float stored)
        {
            double lit = linear * Math.Pow(2.0, Presentation.Exposure(stored));
            if (lit <= 0.0) return 0f;
            if (lit >= 1.0) return 1f;
            double srgb = lit <= 0.0031308 ? 12.92 * lit : 1.055 * Math.Pow(lit, 1.0 / 2.4) - 0.055;
            return (float)srgb;
        }

        public static int Seen(float stored)
        {
            int count = 0;
            foreach (float mark in Marks)
                if (Shade(mark, stored) >= SeenFloor) count++;
            return count;
        }
    }
}
