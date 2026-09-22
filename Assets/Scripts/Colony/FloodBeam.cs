namespace OutpostZero.Colony
{
    /// <summary>
    /// A floodlight washes a circle when the generator is running. Inside that circle a crouch is no longer dark.
    /// </summary>
    public static class FloodBeam
    {
        public const float Radius = 14f;

        public static float Strength(float distance, bool powered)
        {
            if (!powered) return 0f;
            if (distance < 0f) distance = 0f;
            if (distance >= Radius) return 0f;
            return 1f - distance / Radius;
        }
    }
}
