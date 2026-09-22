namespace OutpostZero.Expedition
{
    /// <summary>
    /// A short window after someone steps through a door. Chasers already at that
    /// threshold cross once, and they spread so they do not share one point.
    /// </summary>
    public static class DoorCross
    {
        public const float Reach = 8f;
        public const float Window = 2.5f;
        public const float PackGap = 0.7f;

        public static float FromX { get; private set; }
        public static float FromZ { get; private set; }
        public static float ToX { get; private set; }
        public static float ToZ { get; private set; }
        public static float When { get; private set; } = -100f;

        private static int stamp;

        public static void Note(float fromX, float fromZ, float toX, float toZ, float time)
        {
            FromX = fromX;
            FromZ = fromZ;
            ToX = toX;
            ToZ = toZ;
            When = time;
            stamp++;
        }

        public static void Clear()
        {
            stamp = 0;
            When = -100f;
            FromX = 0f;
            FromZ = 0f;
            ToX = 0f;
            ToZ = 0f;
        }

        public static bool ShouldFollow(int seen, float x, float z, float time, out int generation)
        {
            generation = stamp;
            if (seen == stamp) return false;
            float since = time - When;
            if (since < 0f || since > Window) return false;
            float dx = x - FromX;
            float dz = z - FromZ;
            return dx * dx + dz * dz <= Reach * Reach;
        }

        public static void Slot(int index, out float x, out float z)
        {
            int slot = index < 0 ? 0 : index % 6;
            x = ((slot % 3) - 1) * PackGap;
            z = slot < 3 ? 0f : PackGap;
        }
    }
}
