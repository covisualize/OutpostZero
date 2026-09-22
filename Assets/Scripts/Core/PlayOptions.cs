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

        public static string FrameName(int stored)
        {
            if (stored == 1) return "30 fps";
            if (stored == 2) return "60 fps";
            if (stored == 3) return "120 fps";
            if (stored == 4) return "Uncapped";
            return "Auto";
        }
    }
}
