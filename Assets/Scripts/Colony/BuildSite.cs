namespace OutpostZero.Colony
{
    /// <summary>
    /// A new module is a site until a builder puts in the hours. An older save has no site
    /// flag, and that still counts as finished.
    /// </summary>
    public static class BuildSite
    {
        public static int Need(string kind)
        {
            switch (kind)
            {
                case "Barricade":
                case "Spikes":
                case "Oil":
                    return 1;
                case "Cot":
                case "Water":
                case "Crate":
                    return 2;
                case "Workbench":
                case "Generator":
                case "Purifier":
                    return 3;
                case "Watchtower":
                case "Farm":
                    return 4;
                case "TradingPost":
                case "Turret":
                    return 5;
                default:
                    return 2;
            }
        }

        public static int Shift(string trait, float morale)
        {
            if (morale < 10f) return 0;
            if (trait != null && trait.IndexOf("Engineer", System.StringComparison.Ordinal) >= 0) return 2;
            return 1;
        }

        public static bool Ready(int site, int integrity)
        {
            return site == 0 && integrity > 0;
        }

        public static float Bulk(int hours)
        {
            return hours > 0 ? 0.75f : 0.45f;
        }

        public static void Work(int site, int hours, int need, int pace, out int nextSite, out int nextHours, out bool finished)
        {
            nextSite = site == 0 ? 0 : 1;
            nextHours = hours < 0 ? 0 : hours;
            finished = false;
            if (site == 0) return;
            if (need < 1) need = 1;
            if (pace < 0) pace = 0;
            nextHours += pace;
            if (nextHours < need) return;
            nextHours = need;
            nextSite = 0;
            finished = true;
        }
    }
}
