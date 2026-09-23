namespace OutpostZero.Sensory
{
    /// <summary>
    /// Rain dulls a noise by fifteen percent. Clear weather leaves it alone.
    /// A sound that falls to the floor goes quiet. Thunder still covers a shot on its own.
    /// </summary>
    public static class RainMask
    {
        public const float Cover = 0.15f;

        public static float Heard(float perceived, bool rain)
        {
            if (!rain || perceived <= 0f) return perceived;
            float next = perceived * (1f - Cover);
            if (next <= HearGate.Floor) return 0f;
            return next;
        }
    }
}
