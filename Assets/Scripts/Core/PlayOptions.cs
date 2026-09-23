using System;

namespace OutpostZero.Core
{
    /// <summary>
    /// Aim pull, stance edges, and the frame cap. A stored 0 keeps the old defaults:
    /// no aim pull, hold to crouch, and the frame rate follows VSync.
    /// </summary>
    public static class PlayOptions
    {
        public static float Yaw(float facingX, float facingZ, float toX, float toZ, int strength, float dt)
        {
            if (strength <= 0 || dt <= 0f) return 0f;
            float lenT = (float)Math.Sqrt(toX * toX + toZ * toZ);
            float lenF = (float)Math.Sqrt(facingX * facingX + facingZ * facingZ);
            if (lenT < 0.5f || lenT > 18f || lenF < 0.2f) return 0f;
            float nx = toX / lenT;
            float nz = toZ / lenT;
            float fx = facingX / lenF;
            float fz = facingZ / lenF;
            float dot = fx * nx + fz * nz;
            float gate = strength >= 2 ? 0.65f : 0.85f;
            if (dot < gate) return 0f;
            float cross = fz * nx - fx * nz;
            float degrees = (float)Math.Atan2(cross, dot) * (180f / (float)Math.PI);
            float step = degrees * (strength >= 2 ? 0.55f : 0.28f) * dt;
            float cap = (strength >= 2 ? 140f : 70f) * dt;
            if (step > cap) return cap;
            if (step < -cap) return -cap;
            return step;
        }

        public static float StickY(float y, bool invert)
        {
            return invert ? -y : y;
        }

        public static bool Stance(bool held, bool pressed, bool latched, int mode)
        {
            if (mode == 1) return pressed ? !latched : latched;
            return held;
        }

        public static int FrameTarget(int stored, bool vsync)
        {
            if (stored == 1) return 30;
            if (stored == 2) return 60;
            if (stored == 3) return 120;
            if (stored == 4) return -1;
            return vsync ? -1 : 60;
        }

        public static int NextFrame(int stored)
        {
            if (stored < 0 || stored > 4) return 0;
            return (stored + 1) % 5;
        }

        public static readonly float[] Scales = { 0f, 0.5f, 0.67f, 0.75f, 0.85f, 1f };

        public static int ScaleStep(int stored) => stored < 0 || stored >= Scales.Length ? 0 : stored;

        public static int NextScale(int stored) => (ScaleStep(stored) + 1) % Scales.Length;

        /// <summary>
        /// Step 0 follows the quality tier; every other step overrides it.
        /// </summary>
        public const float UiScaleMin = 0.8f;
        public const float UiScaleMax = 1.5f;

        public static float UiScale(float stored)
        {
            if (float.IsNaN(stored) || stored <= 0f) return 1f;
            return stored < UiScaleMin ? UiScaleMin : stored > UiScaleMax ? UiScaleMax : stored;
        }

        public const float SensitivityMin = 0.5f;
        public const float SensitivityMax = 2f;

        public static float Sensitivity(float stored)
        {
            if (float.IsNaN(stored) || stored <= 0f) return 1f;
            return stored < SensitivityMin ? SensitivityMin : stored > SensitivityMax ? SensitivityMax : stored;
        }

        /// <summary>Camp overview pan per second, before the camera's own edge speed.</summary>
        public static void Pan(float x, float z, float sensitivity, bool invert, out float px, out float pz)
        {
            float scale = Sensitivity(sensitivity);
            px = x * scale;
            pz = StickY(z, invert) * scale;
        }

        public static float RenderScale(int stored, float tierScale)
        {
            int step = ScaleStep(stored);
            return step == 0 ? tierScale : Scales[step];
        }

        public static string ScaleName(int stored, string language)
        {
            int step = ScaleStep(stored);
            if (step == 0) return language == null ? OutpostZero.Shell.Loc.T("scale.auto") : OutpostZero.Shell.Loc.T("scale.auto", language);
            return (int)Math.Round(Scales[step] * 100f) + "%";
        }

        public static string FrameName(int stored)
        {
            return FrameName(stored, "en");
        }

        public static string FrameName(int stored, string language)
        {
            string key = stored == 1 ? "frame.30"
                : stored == 2 ? "frame.60"
                : stored == 3 ? "frame.120"
                : stored == 4 ? "frame.uncapped"
                : "frame.auto";
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}
