namespace OutpostZero.Items
{
    /// <summary>
    /// A container asks the pack to open. The shell consumes the request on its next tick.
    /// </summary>
    public static class PackView
    {
        private static int ask;

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
