namespace OutpostZero.Items
{
    /// <summary>
    /// A container asks the pack to open. The shell consumes the request on its next tick.
    /// </summary>
    public static class PackView
    {
        private static int ask;
        public const float NightmarePace = 0.15f;

        public static bool Showing { get; set; }

        /// <summary>Street time while the pack is open: stopped, or a crawl on Nightmare.</summary>
        public static float Pace(int difficulty)
        {
            return difficulty >= 3 ? NightmarePace : 0f;
        }

        public static void AskOpen()
        {
            ask++;
        }

        public static bool ConsumeOpen()
        {
            if (ask <= 0) return false;
            ask = 0;
            return true;
        }
    }
}
