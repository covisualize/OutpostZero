namespace OutpostZero.Player
{
    /// <summary>
    /// A rail throws a short beam while aiming, and only while the cell still has charge.
    /// It lifts exposure, but it never reaches the full flashlight. The wide lamp still does that.
    /// </summary>
    public static class RailLamp
    {
        public const float Reach = 8f;
        public const float Cone = 12f;
        public const float Lift = 0.35f;
        public const float Cap = 0.85f;
        public const float Peak = 3.4f;

        public static bool Lit(bool aiming, bool hasRail, bool cellLive)
        {
            return aiming && hasRail && cellLive;
        }

        public static float Exposure(float bare, bool railLit, bool flashlight)
        {
            if (flashlight || !railLit) return bare;
            if (bare < 0f) bare = 0f;
            float next = bare + Lift;
            if (next > Cap) return Cap;
            if (next < 0f) return 0f;
            return next;
        }

        public static bool Beam(float distance, float angle, bool railLit)
        {
            if (!railLit) return false;
            if (distance < 0f) distance = 0f;
            if (angle < 0f) angle = 0f;
            return distance <= Reach && angle <= Cone;
        }
    }
}
