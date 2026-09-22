namespace OutpostZero.Colony
{
    /// <summary>
    /// The generator's tank. A new camp and an old save start with ten hours.
    /// After dusk a running generator drinks that tank. An empty tank stays dark.
    /// </summary>
    public static class FuelTank
    {
        public const float Start = 10f;
        public const float NightRate = 90f;
        public const float Dusk = 0.45f;

        public static float Clamp(float hours)
        {
            if (hours < 0f) return 0f;
            return hours;
        }

        public static bool Lit(bool built, float hours)
        {
            return built && hours > 0f;
        }

        public static float Drink(float hours, float deltaSeconds, bool running, float night)
        {
            hours = Clamp(hours);
            if (!running || night <= Dusk || deltaSeconds <= 0f) return hours;
            float next = hours - deltaSeconds / 3600f * NightRate;
            return next < 0f ? 0f : next;
        }

        public static float Pour(float hours, float added)
        {
            hours = Clamp(hours);
            if (added <= 0f) return hours;
            return hours + added;
        }

        public static int Pack(float hours)
        {
            int tenths = (int)System.Math.Round(Clamp(hours) * 10f);
            if (tenths < 0) return 0;
            return tenths;
        }

        public static float Unpack(int tenths, int known)
        {
            if (known == 0) return Start;
            if (tenths < 0) return 0f;
            return tenths / 10f;
        }

        public static string Label(float hours)
        {
            int tenths = Pack(hours);
            return (tenths / 10) + "." + (tenths % 10);
        }
    }
}
