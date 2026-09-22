namespace OutpostZero.Combat
{
    /// <summary>
    /// A blade on a wall rings farther than a clean swing. A crouch keeps even the ring closer.
    /// </summary>
    public static class BladeClang
    {
        public const float Reach = 9f;
        public const float Crouch = 0.55f;

        public static float Radius(bool crouch)
        {
            return crouch ? Reach * Crouch : Reach;
        }
    }
}
