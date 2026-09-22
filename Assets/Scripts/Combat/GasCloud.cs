namespace OutpostZero.Combat
{
    /// <summary>
    /// A broken toxic barrel leaves a cloud that hangs for twenty-five seconds.
    /// Anyone inside the four-meter circle moves slower. The cloud itself deals no damage.
    /// The blast on the barrel stays where it was.
    /// </summary>
    public static class GasCloud
    {
        public const float Radius = 4f;
        public const float Life = 25f;
        public const float Pace = 0.62f;
        public const float Hurt = 0f;

        public static bool Live(float age)
        {
            return age >= 0f && age < Life;
        }

        public static bool Inside(float dx, float dz)
        {
            return dx * dx + dz * dz <= Radius * Radius;
        }

        public static bool Covers(float x, float z, float originX, float originZ, float age)
        {
            if (!Live(age)) return false;
            return Inside(x - originX, z - originZ);
        }

        public static float Speed(float speed, bool inside)
        {
            if (speed < 0f) speed = 0f;
            if (!inside) return speed;
            return speed * Pace;
        }
    }
}
