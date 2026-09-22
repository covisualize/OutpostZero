namespace OutpostZero.Combat
{
    /// <summary>
    /// Sights pull a shot group tighter. Hip fire keeps the open angle.
    /// </summary>
    public static class SightGroup
    {
        public const float Tight = 0.62f;

        public static float Angle(float spread, bool aiming)
        {
            if (spread < 0f) spread = 0f;
            if (!aiming) return spread;
            return spread * Tight;
        }
    }
}
