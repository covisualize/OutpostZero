namespace OutpostZero.Shell
{
    /// <summary>
    /// Whether the results screen offers Retry. Extraction clears the district just left, so Retry heads for the
    /// next one on the board, and only while that one can still be entered.
    /// </summary>
    public static class ResultsRetry
    {
        public static bool Open(bool hasNext, bool nextCleared, bool endless, bool campaignWon)
        {
            if (!hasNext) return false;
            if (endless) return true;
            return !campaignWon && !nextCleared;
        }
    }
}
