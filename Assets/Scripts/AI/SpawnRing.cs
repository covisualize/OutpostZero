using System;

namespace OutpostZero.AI
{
    /// <summary>
    /// Spawns stay 14 m out, clear of the sanctuary yard, and off the camera's forward arc.
    /// </summary>
    public static class SpawnRing
    {
        public const float MinDistance = 14f;

        public static bool Allowed(float x, float z, float playerX, float playerZ)
        {
            float dx = x - playerX;
            float dz = z - playerZ;
            if (dx * dx + dz * dz < MinDistance * MinDistance) return false;
            if (x <= -6f && z <= -4f) return false;
            return true;
        }

        public static bool InFront(float camX, float camZ, float fwdX, float fwdZ, float x, float z)
        {
            float flen = (float)Math.Sqrt(fwdX * fwdX + fwdZ * fwdZ);
            if (flen < 0.2f) return false;
            float dx = x - camX;
            float dz = z - camZ;
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            if (len < 0.001f) return true;
            float dot = (dx * (fwdX / flen) + dz * (fwdZ / flen)) / len;
            return dot > 0.35f;
        }
    }
}
