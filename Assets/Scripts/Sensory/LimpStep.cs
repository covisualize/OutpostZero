namespace OutpostZero.Sensory
{
    /// <summary>
    /// A camp wound makes the step easier to hear. A clear leader keeps the ground's reach.
    /// </summary>
    public static class LimpStep
    {
        public const float Bite = 1.15f;
        public const float Fever = 1.4f;
        public const float Critical = 1.7f;

        public static float Radius(float radius, int injury)
        {
            if (radius < 0f) radius = 0f;
            if (injury <= 0) return radius;
            float scale = injury == 1 ? Bite : injury == 2 ? Fever : Critical;
            return radius * scale;
        }
    }
}
