namespace OutpostZero.Colony
{
    /// <summary>
    /// Dusk is 20:00 and dawn is 05:00. A watch that steps into that window,
    /// or a sleep that would skip it, is the night the raid belongs to.
    /// </summary>
    public static class RaidWatch
    {
        public const float Dusk = 20f;
        public const float Dawn = 5f;

        public static bool Night(float hour)
        {
            if (hour < 0f) return false;
            float wrapped = hour >= 24f ? hour % 24f : hour;
            return wrapped >= Dusk || wrapped < Dawn;
        }

        public static bool Crosses(float fromHour, float added)
        {
            if (Night(fromHour)) return true;
            if (added <= 0f) return false;
            float end = fromHour + added;
            if (fromHour < Dusk && end >= Dusk) return true;
            if (end >= 24f) return true;
            return false;
        }
    }
}
