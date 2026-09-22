namespace OutpostZero.Expedition
{
    /// <summary>
    /// A follower who sees the dead up close cries out.
    /// The cry waits six seconds and carries sixteen meters. A quiet street stays quiet.
    /// </summary>
    public static class StraggleCall
    {
        public const float Near = 7f;
        public const float Gap = 6f;
        public const float Radius = 16f;

        public static bool Due(float last, float now, float nearest)
        {
            if (nearest < 0f || nearest > Near) return false;
            if (last <= 0f) return true;
            if (now < last) return false;
            return now - last >= Gap;
        }
    }
}
