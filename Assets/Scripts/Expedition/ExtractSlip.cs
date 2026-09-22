namespace OutpostZero.Expedition
{
    /// <summary>
    /// The line on the extract screen: which street you left, and the kills and scrap against the quota.
    /// </summary>
    public static class ExtractSlip
    {
        public static string Line(string district, int kills, int killGoal, int scrap, int scrapGoal, string killWord, string scrapWord)
        {
            if (string.IsNullOrEmpty(district)) district = "Street";
            if (kills < 0) kills = 0;
            if (scrap < 0) scrap = 0;
            if (killGoal < 1) killGoal = 1;
            if (scrapGoal < 1) scrapGoal = 1;
            if (string.IsNullOrEmpty(killWord)) killWord = "kills";
            if (string.IsNullOrEmpty(scrapWord)) scrapWord = "scrap";
            return district + "  " + killWord + " " + kills + "/" + killGoal + "  " + scrapWord + " " + scrap + "/" + scrapGoal;
        }
    }
}
