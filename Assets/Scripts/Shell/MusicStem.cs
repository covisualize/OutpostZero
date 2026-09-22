using OutpostZero.Core;

namespace OutpostZero.Shell
{
    public enum MusicTheme
    {
        Camp,
        Street,
        Raid
    }

    /// <summary>
    /// Three beds follow the yard. Camp stays thin. The street adds percussion, then combat.
    /// A raid keeps a combat floor so the night is already loud.
    /// </summary>
    public static class MusicStem
    {
        public const float Window = 8f;
        public const int StreakAt = 3;

        public static MusicTheme Theme(GameState state)
        {
            if (state == GameState.RaidActive) return MusicTheme.Raid;
            if (state == GameState.CampManagement || state == GameState.MainMenu) return MusicTheme.Camp;
            return MusicTheme.Street;
        }

        public static void Gains(MusicTheme theme, float tension, out float drone, out float percussion, out float combat)
        {
            if (tension < 0f) tension = 0f;
            if (tension > 100f) tension = 100f;
            drone = theme == MusicTheme.Raid ? 0.55f : theme == MusicTheme.Camp ? 0.22f : 0.35f;
            float perc = tension <= 25f ? 0f : (tension - 25f) / 50f;
            if (perc > 1f) perc = 1f;
            percussion = theme == MusicTheme.Camp ? perc * 0.25f : perc;
            float fight = tension <= 60f ? 0f : (tension - 60f) / 30f;
            if (fight > 1f) fight = 1f;
            combat = theme == MusicTheme.Camp ? 0f : fight;
            if (theme == MusicTheme.Raid && combat < 0.45f) combat = 0.45f;
        }

        public static int Tally(int count, float since, float now, float window)
        {
            if (count < 0) count = 0;
            if (window < 0f) window = 0f;
            if (count == 0 || now - since > window) return 1;
            int next = count + 1;
            return next > 9 ? 9 : next;
        }

        public static bool Streak(int count)
        {
            return count >= StreakAt;
        }

        public static string Cue(string moment)
        {
            if (moment == "kill") return "stinger_kill";
            if (moment == "death") return "stinger_death";
            if (moment == "extract") return "stinger_extract";
            if (moment == "raid") return "stinger_raid";
            if (moment == "dawn") return "stinger_dawn";
            return "";
        }
    }
}
