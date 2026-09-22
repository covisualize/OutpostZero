namespace OutpostZero.Player
{
    /// <summary>
    /// A footstep from the clip owns the sound for a short beat.
    /// The timer keeps walking when no clip has spoken.
    /// </summary>
    public static class StepGate
    {
        public const float Hold = 0.22f;

        public static bool AllowTimer(float now, float lastEvent)
        {
            if (lastEvent <= 0f) return true;
            if (now < lastEvent) return true;
            return now - lastEvent >= Hold;
        }
    }
}
