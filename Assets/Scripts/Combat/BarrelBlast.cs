namespace OutpostZero.Combat
{
    /// <summary>
    /// A red barrel's blast: 120 damage at the barrel falling off in a straight line to 20 at six metres,
    /// heard 45 metres away. Other barrels in the ring go off after their own 0.15 to 0.4 second delay.
    /// </summary>
    public static class BarrelBlast
    {
        public const float Radius = 6f;
        public const float Near = 120f;
        public const float Far = 20f;
        public const float Noise = 45f;
        public const float ChainMin = 0.15f;
        public const float ChainMax = 0.4f;

        public static float Damage(float distance)
        {
            if (distance < 0f) distance = 0f;
            if (distance > Radius) return 0f;
            return Near + (Far - Near) * (distance / Radius);
        }

        /// <summary>A barrel's chain delay from its own id, so the same street goes off in the same order.</summary>
        public static float ChainDelay(int id)
        {
            unchecked
            {
                uint h = (uint)id * 2654435761u;
                h ^= h >> 16;
                h *= 2246822519u;
                h ^= h >> 13;
                float unit = (h & 0xFFFFFF) / (float)0x1000000;
                return ChainMin + (ChainMax - ChainMin) * unit;
            }
        }
    }
}
