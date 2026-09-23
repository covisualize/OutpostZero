namespace OutpostZero.Graphics
{
    /// <summary>
    /// The leader's death drains the colour out of the frame as the death camera closes in.
    /// </summary>
    public static class DeathVeil
    {
        public const float Drained = -85f;

        public static float Saturation(float weight)
        {
            if (weight <= 0f) return 0f;
            if (weight >= 1f) return Drained;
            return Drained * weight;
        }
    }
}
