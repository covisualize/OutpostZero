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

        /// <summary>Footfall noise keeps the designed pace: a clip step speaks only once the interval is nearly up.</summary>
        public const float Slack = 0.08f;

        public static bool EventSpeaks(float now, float nextNoise)
        {
            return now >= nextNoise - Slack;
        }

        /// <summary>The timer emits footstep noise only while the rig has stopped sending footfalls.</summary>
        public static bool TimerOwnsNoise(float now, float lastEvent, float interval)
        {
            if (lastEvent <= 0f || now < lastEvent) return true;
            float quiet = interval > Hold ? interval * 1.5f : Hold;
            return now - lastEvent >= quiet;
        }
    }
}
