using System.Text;

namespace OutpostZero.Shell
{
    /// <summary>
    /// The street readout speaks the active language.
    /// English keeps the same words the board already used.
    /// </summary>
    public static class StreetHud
    {
        public static string Needs(int hunger, int thirst, int fatigue, string language)
        {
            if (hunger < 0) hunger = 0;
            if (thirst < 0) thirst = 0;
            if (fatigue < 0) fatigue = 0;
            return Word("hud.hunger", language) + " " + hunger
                + "  " + Word("hud.thirst", language) + " " + thirst
                + "  " + Word("hud.fatigue", language) + " " + fatigue;
        }

        public static string Flag(string key, string language)
        {
            return Word(key, language);
        }

        public static string Exposure(int percent, string language)
        {
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            return Word("hud.exposure", language) + " " + percent + "%";
        }

        public static string Watch(int contacts, string language)
        {
            if (contacts < 0) contacts = 0;
            return Word("hud.watch", language) + " " + contacts;
        }

        public static string Infection(int stage, string language)
        {
            if (stage <= 0) return "";
            if (stage == 1) return Word("hud.infect1", language);
            if (stage == 2) return Word("hud.infect2", language);
            return Word("hud.infect3", language);
        }

        public static string Quota(int kills, int killGoal, int scrap, int scrapGoal, string language)
        {
            if (kills < 0) kills = 0;
            if (scrap < 0) scrap = 0;
            if (killGoal < 1) killGoal = 1;
            if (scrapGoal < 1) scrapGoal = 1;
            return Word("hud.kills", language) + " " + kills + "/" + killGoal
                + "   " + Word("hud.scrap", language) + " " + scrap + "/" + scrapGoal;
        }

        public static string Tension(int value, string state, string language)
        {
            if (value < 0) value = 0;
            if (value > 100) value = 100;
            return Word("hud.tension", language) + " " + value + "  " + Mood(state, language);
        }

        public static string Mood(string state, string language)
        {
            if (state == "BuildUp") return Word("hud.state.build", language);
            if (state == "Peak") return Word("hud.state.peak", language);
            if (state == "Relax") return Word("hud.state.relax", language);
            return Word("hud.state.calm", language);
        }

        public static string Hold(int seconds, string language)
        {
            if (seconds < 0) seconds = 0;
            return Word("hud.hold", language) + " " + seconds + "s";
        }

        public static string Raid(int seconds, string language)
        {
            if (seconds < 0) seconds = 0;
            return Word("hud.raid", language) + " " + seconds + "s";
        }

        public static string Hit(string sector, string language)
        {
            if (sector == "back") return Word("hud.hit.back", language);
            if (sector == "left") return Word("hud.hit.left", language);
            if (sector == "right") return Word("hud.hit.right", language);
            return Word("hud.hit.front", language);
        }

        public static string None(string language)
        {
            return Word("hud.noweapon", language);
        }

        public static string Ammo(string name, int current, int reserve, bool reloading, int fillPercent, bool low, string language)
        {
            if (current < 0) current = 0;
            if (reserve < 0) reserve = 0;
            if (fillPercent < 0) fillPercent = 0;
            if (fillPercent > 100) fillPercent = 100;
            var builder = new StringBuilder();
            builder.Append(string.IsNullOrEmpty(name) ? Word("hud.noweapon", language) : name);
            builder.Append("   ").Append(current).Append(" / ").Append(reserve);
            if (reloading) builder.Append("  ").Append(Word("hud.reload", language)).Append(' ').Append(fillPercent).Append('%');
            else if (low) builder.Append("  ").Append(Word("hud.low", language));
            return builder.ToString();
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Loc.T(key);
            return Loc.T(key, language);
        }
    }
}
