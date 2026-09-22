using System;

namespace OutpostZero.Colony
{
    /// <summary>
    /// A raid steps out past the approach, spread along the boards, not around the leader.
    /// </summary>
    public static class RaidDrop
    {
        public const float Out = 8f;
        public const float Spread = 1.6f;

        public static void Point(string approach, int slot, int count, out float x, out float z)
        {
            RaidPlan.AnchorOf(approach, out float ax, out float az);
            float len = (float)Math.Sqrt(ax * ax + az * az);
            float dx = ax;
            float dz = az;
            if (len < 0.01f)
            {
                dx = 0f;
                dz = -1f;
                len = 1f;
            }
            float ox = ax + dx / len * Out;
            float oz = az + dz / len * Out;
            float px = -dz / len;
            float pz = dx / len;
            if (count < 1) count = 1;
            if (slot < 0) slot = 0;
            if (slot >= count) slot = count - 1;
            float along = (slot - (count - 1) * 0.5f) * Spread;
            x = ox + px * along;
            z = oz + pz * along;
        }
    }
}
