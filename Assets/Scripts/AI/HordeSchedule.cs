namespace OutpostZero.AI
{
    /// <summary>
    /// Timed beats on an expedition: an alley of walkers, a runner pack, then a brute.
    /// The cursor moves past a beat even when the tier is too low to spawn it.
    /// </summary>
    public static class HordeSchedule
    {
        public static float Advance(float elapsed, float cursor, int tier, out string kind)
        {
            kind = "";
            if (cursor < 45f && elapsed >= 45f)
            {
                kind = "alley";
                return 45f;
            }
            if (cursor < 90f && elapsed >= 90f)
            {
                if (tier >= 2) kind = "runners";
                return 90f;
            }
            if (cursor < 300f && elapsed >= 300f)
            {
                if (tier >= 3) kind = "brute";
                return 300f;
            }
            return cursor;
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
