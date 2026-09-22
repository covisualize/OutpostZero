using System;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A short dash that spends stamina and ignores hits for the opening of the roll.
    /// </summary>
    public static class DodgeClock
    {
        public const float Cost = 22f;
        public const float Duration = 0.28f;
        public const float Cooldown = 0.85f;
        public const float Speed = 11f;
        public const float IFrames = 0.22f;

        public static bool Ready(float stamina, float sinceLast)
        {
            return stamina >= Cost && sinceLast >= Cooldown;
        }

        public static void Direction(float moveX, float moveZ, float faceX, float faceZ, out float x, out float z)
        {
            float mag = moveX * moveX + moveZ * moveZ;
            if (mag > 0.0001f)
            {
                float scale = 1f / (float)Math.Sqrt(mag);
                x = moveX * scale;
                z = moveZ * scale;
                return;
            }

            float face = faceX * faceX + faceZ * faceZ;
            if (face > 0.0001f)
            {
                float scale = 1f / (float)Math.Sqrt(face);
                x = faceX * scale;
                z = faceZ * scale;
                return;
            }

            x = 0f;
            z = 1f;
        }

        public static bool Untouchable(float elapsed)
        {
            return elapsed >= 0f && elapsed < IFrames;
        }
    }
}
