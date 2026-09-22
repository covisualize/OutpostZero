using System;

namespace OutpostZero.AI
{
    /// <summary>
    /// Keeps a horde from standing in one footprint. The push is away from anyone inside the radius,
    /// and a crowded agent yields on the navigation priority scale (0 is the most stubborn).
    /// </summary>
    public static class CrowdSpace
    {
        public const float Radius = 1.4f;
        public const float MaxPush = 1.2f;
        public const float Slide = 2.5f;

        public static void Push(float x, float z, float[] othersX, float[] othersZ, int count, out float pushX, out float pushZ)
        {
            pushX = 0f;
            pushZ = 0f;
            if (othersX == null || othersZ == null || count <= 0) return;
            int n = count;
            if (n > othersX.Length) n = othersX.Length;
            if (n > othersZ.Length) n = othersZ.Length;
            for (int i = 0; i < n; i++)
            {
                float dx = x - othersX[i];
                float dz = z - othersZ[i];
                float dist2 = dx * dx + dz * dz;
                if (dist2 < 0.0001f || dist2 >= Radius * Radius) continue;
                float dist = (float)Math.Sqrt(dist2);
                float overlap = (Radius - dist) / Radius;
                pushX += dx / dist * overlap;
                pushZ += dz / dist * overlap;
            }
            float len2 = pushX * pushX + pushZ * pushZ;
            if (len2 <= MaxPush * MaxPush) return;
            float len = (float)Math.Sqrt(len2);
            pushX = pushX / len * MaxPush;
            pushZ = pushZ / len * MaxPush;
        }

        public static int Neighbors(float x, float z, float[] othersX, float[] othersZ, int count)
        {
            if (othersX == null || othersZ == null || count <= 0) return 0;
            int n = count;
            if (n > othersX.Length) n = othersX.Length;
            if (n > othersZ.Length) n = othersZ.Length;
            int near = 0;
            float limit = Radius * Radius;
            for (int i = 0; i < n; i++)
            {
                float dx = x - othersX[i];
                float dz = z - othersZ[i];
                if (dx * dx + dz * dz < limit) near++;
            }
            return near;
        }

        public static int Priority(int neighborCount)
        {
            if (neighborCount < 0) neighborCount = 0;
            int value = 30 + neighborCount * 8;
            if (value > 99) return 99;
            return value;
        }
    }
}
