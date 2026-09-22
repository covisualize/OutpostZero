using System;

namespace OutpostZero.AI
{
    /// <summary>
    /// A slab hides the player from a threat on its far side.
    /// Yaw 0 faces +Z, the same way a Unity yaw turns a prop.
    /// </summary>
    public static class CoverSight
    {
        public const float Reach = 1.8f;

        public static float Scale(float playerX, float playerZ, float threatX, float threatZ, float coverX, float coverZ, float yawDeg, bool crouching)
        {
            float dx = playerX - coverX;
            float dz = playerZ - coverZ;
            if (dx * dx + dz * dz > Reach * Reach) return 1f;
            float rad = yawDeg * (float)(Math.PI / 180.0);
            float fx = (float)Math.Sin(rad);
            float fz = (float)Math.Cos(rad);
            float playerSide = dx * fx + dz * fz;
            float tx = threatX - coverX;
            float tz = threatZ - coverZ;
            float threatSide = tx * fx + tz * fz;
            if (playerSide > 0.2f || threatSide < 0.45f) return 1f;
            return crouching ? 0.4f : 0.62f;
        }

        public static float Best(float playerX, float playerZ, float threatX, float threatZ, float[] coverX, float[] coverZ, float[] yaw, bool crouching)
        {
            float scale = 1f;
            if (coverX == null || coverZ == null || yaw == null) return scale;
            int count = coverX.Length;
            if (coverZ.Length < count) count = coverZ.Length;
            if (yaw.Length < count) count = yaw.Length;
            for (int i = 0; i < count; i++)
            {
                float next = Scale(playerX, playerZ, threatX, threatZ, coverX[i], coverZ[i], yaw[i], crouching);
                if (next < scale) scale = next;
            }
            return scale;
        }
    }
}
