namespace OutpostZero.Combat
{
    /// <summary>
    /// A round that misses still cracks past anyone standing close to its path.
    /// The one who was hit keeps the normal stagger. This is only the miss.
    /// </summary>
    public static class WhiffClock
    {
        public const float Reach = 1.1f;
        public const float Seconds = 0.45f;
        public const float Pace = 0.72f;

        public static bool Passes(float x0, float z0, float x1, float z1, float bodyX, float bodyZ, out float nearX, out float nearZ)
        {
            float bx = x1 - x0;
            float bz = z1 - z0;
            float len2 = bx * bx + bz * bz;
            float t = 0f;
            if (len2 > 0.0001f)
            {
                float ax = x0 - bodyX;
                float az = z0 - bodyZ;
                t = -(ax * bx + az * bz) / len2;
                if (t < 0f) t = 0f;
                if (t > 1f) t = 1f;
            }
            nearX = x0 + bx * t;
            nearZ = z0 + bz * t;
            float dx = nearX - bodyX;
            float dz = nearZ - bodyZ;
            return dx * dx + dz * dz <= Reach * Reach;
        }

        public static float Speed(float speed, bool flinching)
        {
            if (speed < 0f) speed = 0f;
            if (!flinching) return speed;
            return speed * Pace;
        }

        public static float Tick(float left, float dt)
        {
            if (left <= 0f) return 0f;
            if (dt <= 0f) return left;
            float next = left - dt;
            return next < 0f ? 0f : next;
        }
    }
}
