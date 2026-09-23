namespace OutpostZero.Colony
{
    /// <summary>
    /// The leader's practiced skills change the street.
    /// Below the fourth shift the numbers stay what they were.
    /// </summary>
    public static class FieldHand
    {
        public static float Spread(int skill)
        {
            return Practice.Bonus(skill) > 0 ? 0.85f : 1f;
        }

        public static float Reload(int skill)
        {
            return Practice.Bonus(skill) > 0 ? 0.8f : 1f;
        }

        public static int Scrap(int skill)
        {
            return Practice.Bonus(skill);
        }

        public static int Medkit(int skill)
        {
            return 50 + Practice.Bonus(skill) * 6;
        }

        public static string Dose(int skill)
        {
            return Dose(skill, "en");
        }

        public static string Dose(int skill, string language)
        {
            int hp = Medkit(skill) + HandDepth.Heal(skill);
            return Word("dose.kit", language) + "  +" + hp + " " + Word("dose.hp", language);
        }

        public static string Fail(string language)
        {
            return Word("dose.fail", language);
        }

        public static string Breaks(string language)
        {
            return Word("dose.break", language);
        }

        public static string Relief(string id, string language)
        {
            if (string.IsNullOrEmpty(language)) return Shell.Loc.Item(id);
            return Shell.Loc.Item(id, language);
        }

        public static string Spent(string id, string language)
        {
            return Word("dose.spent", language) + " " + Relief(id, language);
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Shell.Loc.T(key);
            return Shell.Loc.T(key, language);
        }
    }
}
