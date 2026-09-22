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

        public static bool Inside(float distance)
        {
            if (distance < 0f) distance = 0f;
            return distance <= Radius;
        }
    }
}
