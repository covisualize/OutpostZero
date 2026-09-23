using System;
using System.Globalization;
using OutpostZero.Core;

namespace OutpostZero.Shell
{
    /// <summary>
    /// What a save slot shows beside its day and leader: time played, when it was written, and a picture.
    /// The clock only runs while a run is being played, not on the menu, a pause or an ending.
    /// </summary>
    public static class SaveStamp
    {
        public const int ThumbSide = 128;
        public const int ThumbQuality = 70;
        /// <summary>A thumbnail longer than this is dropped rather than bloating the save.</summary>
        public const int ThumbCap = 48 * 1024;
        public const string Format = "yyyy-MM-ddTHH:mm:ssZ";

        public static bool Counts(GameState state)
        {
            return state == GameState.CampManagement || state == GameState.ExpeditionActive || state == GameState.RaidActive
                || state == GameState.ExpeditionResults || state == GameState.SuccessionScreen;
        }

        public static float Tick(float played, float delta, GameState state)
        {
            if (played < 0f || float.IsNaN(played)) played = 0f;
            if (!Counts(state) || delta <= 0f || float.IsNaN(delta)) return played;
            return played + Math.Min(delta, 1f);
        }

        public static string Now(DateTime utc) => utc.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture);

        public static bool TryWhen(string stamp, out DateTime utc)
        {
            return DateTime.TryParseExact(stamp ?? "", Format, CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out utc);
        }

        /// <summary>"2h 14m", "14m", or "&lt;1m".</summary>
        public static string Played(float seconds)
        {
            if (seconds < 60f || float.IsNaN(seconds)) return "<1m";
            int minutes = (int)(seconds / 60f);
            if (minutes < 60) return minutes + "m";
            return minutes / 60 + "h " + (minutes % 60).ToString("00", CultureInfo.InvariantCulture) + "m";
        }

        /// <summary>The slot's second line: time played, then the local date and time it was written.</summary>
        public static string Line(float seconds, string stamp, TimeZoneInfo zone)
        {
            string line = Played(seconds);
            if (TryWhen(stamp, out var utc))
            {
                var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone ?? TimeZoneInfo.Local);
                line += "  " + local.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            }
            return line;
        }

        public static string Keep(string thumbnail)
        {
            if (string.IsNullOrEmpty(thumbnail) || thumbnail.Length > ThumbCap) return "";
            return thumbnail;
        }
    }
}
