using OutpostZero.Core;

namespace OutpostZero.Colony
{
    public enum DayPhase
    {
        Morning,
        Day,
        Evening,
        Night
    }

    /// <summary>
    /// Morning runs from dawn (05:00) to 10:00, the Day expedition window to 17:00, Evening to dusk (20:00),
    /// and Night, when raids come, back round to dawn.
    /// </summary>
    public static class ClockPhase
    {
        public const float DayStarts = 10f;
        public const float EveningStarts = 17f;

        public static DayPhase Of(float hour)
        {
            if (RaidWatch.Night(hour)) return DayPhase.Night;
            float wrapped = hour % 24f;
            if (wrapped < 0f) wrapped += 24f;
            if (wrapped < DayStarts) return DayPhase.Morning;
            if (wrapped < EveningStarts) return DayPhase.Day;
            return DayPhase.Evening;
        }

        /// <summary>
        /// Only the camp autosaves on a phase turn. A street or raid in progress saves when it ends,
        /// so a save taken mid-fight can never undo a death.
        /// </summary>
        public static bool Autosaves(GameState state)
        {
            return state == GameState.CampManagement;
        }

        public static string Name(DayPhase phase, string language)
        {
            string key = "clock.phase." + phase.ToString().ToLowerInvariant();
            return string.IsNullOrEmpty(language) ? Shell.Loc.T(key) : Shell.Loc.T(key, language);
        }
    }
}
