using System.Collections.Generic;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Local board of finished runs. Longer survival ranks first, then kills, then streets.
    /// Eight rows fit on the end screen. The campaign save stays schema 1.
    /// </summary>
    public static class RunBoard
    {
        public const int Limit = 8;

        public struct Run
        {
            public string Name;
            public int Day;
            public int Kills;
            public int Lost;
            public int Cleared;
            public int Won;
        }

        public static Run Make(string name, int day, int kills, int lost, int cleared, bool won)
        {
            if (string.IsNullOrEmpty(name)) name = "Leader";
            if (day < 1) day = 1;
            if (kills < 0) kills = 0;
            if (lost < 0) lost = 0;
            if (cleared < 0) cleared = 0;
            return new Run
            {
                Name = Clean(name),
                Day = day,
                Kills = kills,
                Lost = lost,
                Cleared = cleared,
                Won = won ? 1 : 0
            };
        }

        public static bool Outranks(Run left, Run right)
        {
            if (left.Day != right.Day) return left.Day > right.Day;
            if (left.Kills != right.Kills) return left.Kills > right.Kills;
            if (left.Cleared != right.Cleared) return left.Cleared > right.Cleared;
            return left.Won > right.Won;
        }

        public static Run[] Insert(Run[] existing, Run next)
        {
            var list = new List<Run>();
            if (existing != null)
            {
                for (int i = 0; i < existing.Length; i++) list.Add(existing[i]);
            }
            int index = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                if (!Outranks(list[i], next))
                {
                    index = i;
                    break;
                }
            }
            list.Insert(index, next);
            if (list.Count > Limit) list.RemoveRange(Limit, list.Count - Limit);
            return list.ToArray();
        }

        public static string Line(Run run)
        {
            return Line(run, "en");
        }

        public static string Line(Run run, string language)
        {
            string mark = run.Won == 1 ? Word("board.held", language) : Word("board.fell", language);
            return mark + "  " + Word("board.day", language) + " " + run.Day
                + "  " + Word("board.kills", language) + " " + run.Kills
                + "  " + Word("board.lost", language) + " " + run.Lost
                + "  " + Word("board.streets", language) + " " + run.Cleared;
        }

        private static string Word(string key, string language)
        {
            if (string.IsNullOrEmpty(language)) return Loc.T(key);
            return Loc.T(key, language);
        }

        public static string Pack(Run[] runs)
        {
            if (runs == null || runs.Length == 0) return "";
            var parts = new string[runs.Length];
            for (int i = 0; i < runs.Length; i++)
            {
                var run = runs[i];
                parts[i] = Clean(run.Name) + "^" + run.Day + "^" + run.Kills + "^" + run.Lost + "^" + run.Cleared + "^" + run.Won;
            }
            return string.Join("|", parts);
        }

        public static Run[] Unpack(string packed)
        {
            if (string.IsNullOrEmpty(packed)) return new Run[0];
            var rows = packed.Split('|');
            var list = new List<Run>();
            for (int i = 0; i < rows.Length; i++)
            {
                if (string.IsNullOrEmpty(rows[i])) continue;
                var fields = rows[i].Split('^');
                if (fields.Length < 6) continue;
                int.TryParse(fields[1], out int day);
                int.TryParse(fields[2], out int kills);
                int.TryParse(fields[3], out int lost);
                int.TryParse(fields[4], out int cleared);
                int.TryParse(fields[5], out int won);
                list.Add(Make(fields[0], day, kills, lost, cleared, won != 0));
                if (list.Count >= Limit) break;
            }
            return list.ToArray();
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Leader";
            return value.Replace("^", " ").Replace("|", " ").Replace("\n", " ").Replace("\r", " ");
        }
    }
}
