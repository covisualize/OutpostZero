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

        public static string Place(string id, string language)
        {
            if (string.IsNullOrEmpty(id)) return Word("result.street", language);
            string named = string.IsNullOrEmpty(language) ? OutpostZero.Shell.Loc.District(id) : OutpostZero.Shell.Loc.District(id, language);
            return string.IsNullOrEmpty(named) ? id : named;
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}
