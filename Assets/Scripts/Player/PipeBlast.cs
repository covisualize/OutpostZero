namespace OutpostZero.Player
{
    /// <summary>
    /// A pipe bomb bursts on impact. Anyone inside the radius takes the full blast.
    /// </summary>
    public static class PipeBlast
    {
        public const float Radius = 4.2f;
        public const float Damage = 42f;
        public const float Noise = 24f;
        public const float Fuse = 1.2f;
        public const float Shove = 1.6f;
        /// <summary>Metres per second a body the blast kills is thrown at point blank.</summary>
        public const float Throw = 9f;
        public const float Stun = 0.55f;

        public static bool Inside(float distance)
        {
            if (distance < 0f) distance = 0f;
            return distance <= Radius;
        }
    }
}
