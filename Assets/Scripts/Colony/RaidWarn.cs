namespace OutpostZero.Colony
{
    /// <summary>
    /// A finished watchtower sees the raid before it reaches the gate.
    /// A guard on duty stretches that warning. No tower means the night starts at once.
    /// </summary>
    public static class RaidWarn
    {
        public const float Tower = 8f;
        public const float FirstGuard = 6f;
        public const float ExtraGuard = 2f;
        public const float Cap = 28f;

        public static float Seconds(int towers, int guards)
        {
            if (towers < 1) return 0f;
            if (guards < 0) guards = 0;
            float time = towers * Tower;
            if (guards > 0) time += FirstGuard + (guards - 1) * ExtraGuard;
            if (time > Cap) return Cap;
            return time;
        }
    }
}
