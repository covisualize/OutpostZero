namespace OutpostZero.Player
{
    /// <summary>
    /// Aiming down sights shortens a step. A sprint is not allowed while the sights are up.
    /// </summary>
    public static class AimPace
    {
        public const float Fraction = 0.55f;

        public static bool AllowsSprint(bool aiming)
        {
            return !aiming;
        }

        public static float Pace(float speed, bool aiming)
        {
            if (speed < 0f) speed = 0f;
            if (!aiming) return speed;
            return speed * Fraction;
        }
    }
}
