namespace OutpostZero.AI
{
    /// <summary>
    /// Timed beats on an expedition: an alley of walkers, then a brute ambush after 5 minutes.
    /// The cursor moves past a beat even when the tier is too low to spawn it.
    /// The runner pack is a night beat instead: once per street, from 90 s in, when it is night.
    /// </summary>
    public static class HordeSchedule
    {
        public const float RunnerAfter = 90f;

        public static float Advance(float elapsed, float cursor, int tier, out string kind)
        {
            kind = "";
            if (cursor < 45f && elapsed >= 45f)
            {
                kind = "alley";
                return 45f;
            }
            if (cursor < 300f && elapsed >= 300f)
            {
                if (tier >= 3) kind = "brute";
                return 300f;
            }
            return cursor;
        }

        public static bool RunnerPack(float elapsed, bool night, int tier, bool released)
        {
            return !released && night && tier >= 2 && elapsed >= RunnerAfter;
        }

        public static int Count(string kind)
        {
            if (kind == "alley") return 8;
            if (kind == "runners") return 4;
            if (kind == "brute") return 1;
            return 0;
        }

        public static string Prefer(string kind)
        {
            if (kind == "runners") return "Runner";
            if (kind == "brute") return "Brute";
            return "";
        }
    }
}
