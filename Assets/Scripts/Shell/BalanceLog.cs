using System.Globalization;
using System.Text;

namespace OutpostZero.Shell
{
    /// <summary>
    /// One CSV row per expedition and one per day, for the balance pass. Invariant culture so a
    /// comma-decimal locale never splits a number across two columns.
    /// </summary>
    public static class BalanceLog
    {
        public const string ExpeditionFile = "expeditions.csv";
        public const string DayFile = "days.csv";

        public const string ExpeditionHeader = "run,day,district,difficulty,weather,leader,end,kills,kill_goal,scrap,scrap_goal,seconds,ammo_start,ammo_end,items_start,items_end,health_end,hunger_end,thirst_end,fatigue_end,infection_end,colonists_alive,deaths_total";
        public const string DayHeader = "run,day,colonists_alive,deaths_total,morale_avg,food,water,scrap,shots,expeditions,kills_total";

        public struct ExpeditionRow
        {
            public string Run;
            public int Day;
            public string District;
            public int Difficulty;
            public string Weather;
            public string Leader;
            public string End;
            public int Kills;
            public int KillGoal;
            public int Scrap;
            public int ScrapGoal;
            public float Seconds;
            public int AmmoStart;
            public int AmmoEnd;
            public int ItemsStart;
            public int ItemsEnd;
            public float Health;
            public float Hunger;
            public float Thirst;
            public float Fatigue;
            public float Infection;
            public int Alive;
            public int Deaths;

            public int AmmoSpent => AmmoStart > AmmoEnd ? AmmoStart - AmmoEnd : 0;
        }

        public struct DayRow
        {
            public string Run;
            public int Day;
            public int Alive;
            public int Deaths;
            public float Morale;
            public int Food;
            public int Water;
            public int Scrap;
            public int Shots;
            public int Expeditions;
            public int Kills;
        }

        public static string Line(ExpeditionRow r)
        {
            return Join(Cell(r.Run), N(r.Day), Cell(r.District), N(r.Difficulty), Cell(r.Weather), Cell(r.Leader), Cell(r.End),
                N(r.Kills), N(r.KillGoal), N(r.Scrap), N(r.ScrapGoal), F(r.Seconds), N(r.AmmoStart), N(r.AmmoEnd),
                N(r.ItemsStart), N(r.ItemsEnd), F(r.Health), F(r.Hunger), F(r.Thirst), F(r.Fatigue), F(r.Infection), N(r.Alive), N(r.Deaths));
        }

        public static string Line(DayRow r)
        {
            return Join(Cell(r.Run), N(r.Day), N(r.Alive), N(r.Deaths), F(r.Morale), N(r.Food), N(r.Water), N(r.Scrap), N(r.Shots), N(r.Expeditions), N(r.Kills));
        }

        /// <summary>One campaign across saves and sessions: its world seed and difficulty, e.g. "s4821-d2".</summary>
        public static string RunId(int seed, int difficulty)
        {
            return "s" + seed.ToString(CultureInfo.InvariantCulture) + "-d" + difficulty.ToString(CultureInfo.InvariantCulture);
        }

        public static string Cell(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            bool quote = text.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
            string clean = text.Replace("\"", "\"\"");
            return quote ? "\"" + clean + "\"" : clean;
        }

        private static string N(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string F(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        private static string Join(params string[] cells)
        {
            var text = new StringBuilder();
            for (int i = 0; i < cells.Length; i++)
            {
                if (i > 0) text.Append(',');
                text.Append(cells[i]);
            }
            return text.ToString();
        }
    }
}
