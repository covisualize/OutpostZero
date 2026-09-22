namespace OutpostZero.Colony
{
    /// <summary>
    /// A street infection comes home as the leader's injury. The worse of the two stays.
    /// A fresh bite is one. A slowing fever is two. The last stage is the colony ceiling.
    /// </summary>
    public static class HomeSick
    {
        public static int Carry(int injury, int stage)
        {
            if (injury < 0) injury = 0;
            if (injury > FeverSpread.Cap) injury = FeverSpread.Cap;
            if (stage < 0) stage = 0;
            if (stage > FeverSpread.Cap) stage = FeverSpread.Cap;
            return injury > stage ? injury : stage;
        }

        public static bool Rises(int injury, int stage)
        {
            int held = injury < 0 ? 0 : injury;
            return Carry(injury, stage) > held;
        }

        public static string Line(int stage, string language)
        {
            if (stage <= 0) return "";
            return Word(stage <= 1 ? "home.bite" : "home.fever", language);
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T(key);
            return OutpostZero.Shell.Loc.T(key, language);
        }
    }
}
