namespace OutpostZero.Combat
{
    /// <summary>
    /// A shot in the shin slows the chase. The head stays a critical hit, and the chest stays a normal one.
    /// A brute keeps more of its pace. The limp lasts a few seconds and then the gait comes back.
    /// </summary>
    public static class LimbCut
    {
        public const float LegFloor = 0.05f;
        public const float LegTop = 0.62f;
        public const float Pace = 0.55f;
        public const float BrutePace = 0.78f;
        public const float Seconds = 4f;

        public static bool Leg(float hitY, float bodyY)
        {
            float height = hitY - bodyY;
            return height >= LegFloor && height <= LegTop;
        }

        public static float Speed(float speed, bool limping, bool brute)
        {
            if (speed < 0f) speed = 0f;
            if (!limping) return speed;
            return speed * (brute ? BrutePace : Pace);
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
