namespace OutpostZero.Graphics
{
    /// <summary>
    /// Ash in the lungs makes the player cough. A crouch holds it longer and keeps it closer.
    /// A clear yard stays quiet.
    /// </summary>
    public static class AshCough
    {
        public const float Gap = 7f;
        public const float CrouchGap = 11f;
        public const float Radius = 8f;

        public static bool Due(bool ash, bool crouch, float last, float now)
        {
            if (!ash) return false;
            float gap = crouch ? CrouchGap : Gap;
            if (last <= 0f) return true;
            if (now < last) return false;
            return now - last >= gap;
        }

        public static float Carry(bool crouch)
        {
            return Carry(crouch, Radius);
        }

        public static float Carry(bool crouch, float radius)
        {
            if (radius < 0f) radius = 0f;
            return crouch ? radius * 0.5f : radius;
        }
    }
}
