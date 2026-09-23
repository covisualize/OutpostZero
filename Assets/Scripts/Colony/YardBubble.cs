namespace OutpostZero.Colony
{
    /// <summary>
    /// When a yard colonist's bark bubble shows. Each colonist speaks for a few seconds in a fixed cycle, offset by
    /// a hash of their id so the camp never talks in chorus. Someone breaking down speaks twice as often.
    /// </summary>
    public static class YardBubble
    {
        public const float Period = 14f;
        public const float Shown = 3.5f;
        public const float Height = 2.35f;

        public static bool Up(string id, float time, float morale)
        {
            if (string.IsNullOrEmpty(id) || time < 0f) return false;
            float period = morale < 10f ? Period * 0.5f : Period;
            float phase = (Hash(id) % 1400u) / 100f;
            float at = (time + phase) % period;
            return at < Shown;
        }

        private static uint Hash(string text)
        {
            uint hash = 2166136261u;
            for (int i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 16777619u;
            }
            return hash;
        }
    }
}
