namespace OutpostZero.Combat
{
    /// <summary>
    /// A takedown, a throw, and a gun on the ground follow the language.
    /// English keeps Takedown, Pipe bomb burst, and Took Tactical 9mm Pistol.
    /// </summary>
    public static class FightSay
    {
        public static string Start(string language) => Word("fight.start", language);
        public static string Slip(string language) => Word("fight.slip", language);
        public static string Down(string language) => Word("fight.down", language);
        public static string Burst(string language) => Word("fight.burst", language);
        public static string Flare(string language) => Word("fight.flare", language);
        public static string Impact(bool molotov, string language) => Word(molotov ? "fight.molotov" : "fight.lure", language);

        public static string Hazard(HazardKind kind, string language)
        {
            if (kind == HazardKind.Toxic) return Word("fight.toxic", language);
            if (kind == HazardKind.Oil) return Word("fight.oil", language);
            return Word("fight.barrel", language);
        }
        public static string Held(string language) => Word("fight.held", language);
        public static string Swap(string language) => Word("fight.swap", language);
        public static string Boards(string language) => Word("fight.boards", language);
        public static string Roster(string language) => Word("fight.roster", language);
        public static string Tower(string language) => Word("fight.tower", language);
        public static string Ledger(string language) => Word("fight.ledger", language);

        public static string Took(string id, string name, string language)
        {
            return Word("fight.took", language) + " " + Gun(id, name, language);
        }

        public static string Lift(string id, string name, string language)
        {
            string word = string.IsNullOrEmpty(language) ? Shell.Loc.T("camp.take") : Shell.Loc.T("camp.take", language);
            return word + " " + Gun(id, name, language);
        }

        public static string Gun(string id, string name, string language)
        {
            string key = "fight.gun." + (id ?? "").ToLowerInvariant();
            string line = Word(key, language);
            if (line != key && !string.IsNullOrEmpty(line)) return line;
            if (!string.IsNullOrEmpty(name)) return name;
            return id ?? "";
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Shell.Loc.T(key);
            return Shell.Loc.T(key, language);
        }
    }
}
