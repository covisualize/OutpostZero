namespace OutpostZero.Player
{
    /// <summary>
    /// A crouched takedown from behind reaches 1.2 meters, holds for 1.1 seconds, then makes a 1.5 meter noise.
    /// </summary>
    public static class QuietKill
    {
        public const float Reach = 1.2f;
        public const float Noise = 1.5f;
        public const float Windup = 1.1f;
        public const float Behind = 0.35f;

        public static bool InReach(float distance)
        {
            if (distance < 0f) distance = 0f;
            return distance <= Reach;
        }

        public static bool FromBehind(float dot)
        {
            return dot >= Behind;
        }

        public static bool Lands(float elapsed)
        {
            return elapsed >= Windup;
        }

        public static bool Victim(bool crouched, bool dead, bool alert, float distance, float dot)
        {
            if (!crouched || dead || alert) return false;
            return InReach(distance) && FromBehind(dot);
        }
    }
}
