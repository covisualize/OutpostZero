namespace OutpostZero.Player
{
    /// <summary>The countdown a status pill shows beside its label, as m:ss rounded up.</summary>
    public static class StatusTimer
    {
        public static float ToNextStage(float infection)
        {
            int stage = Affliction.Stage(infection);
            if (stage == 1) return Affliction.StageOne - infection;
            if (stage == 2) return Affliction.StageTwo - infection;
            return 0f;
        }

        /// <summary>Whole seconds shown, rounded up so a pill never reads 0:00 while it lasts; 0 hides the clock.</summary>
        public static int Shown(float seconds)
        {
            if (seconds <= 0f) return 0;
            int whole = (int)seconds;
            return seconds > whole ? whole + 1 : whole;
        }

        public static string Clock(int seconds)
        {
            if (seconds <= 0) return "";
            int minutes = seconds / 60;
            int rest = seconds % 60;
            return minutes + ":" + (rest < 10 ? "0" : "") + rest;
        }

        public static string Pill(string label, float seconds)
        {
            int shown = Shown(seconds);
            return shown > 0 ? label + " " + Clock(shown) : label;
        }
    }
}
