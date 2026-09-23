namespace OutpostZero.Shell
{
    /// <summary>
    /// An open pitch sits between 0.95 and 1.05.
    /// The next one steps away when it would repeat the last.
    /// </summary>
    public static class PitchGate
    {
        public const float Low = 0.95f;
        public const float High = 1.05f;
        public const float Step = 0.02f;

        public static float Next(float last, float roll)
        {
            if (roll < 0f) roll = 0f;
            if (roll > 1f) roll = 1f;
            float pitch = Low + roll * (High - Low);
            if (last <= 0f) return pitch;
            float gap = pitch - last;
            if (gap < 0f) gap = -gap;
            if (gap >= Step) return pitch;
            if (pitch + Step <= High) return pitch + Step;
            return pitch - Step;
        }
    }
}
