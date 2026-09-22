using System;
using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Each pair keeps its own opinion, from -100 to 100.
    /// Forty or more is a close friend. An empty book is all zeros.
    /// </summary>
    public static class KinBoard
    {
        public const int Floor = -100;
        public const int Cap = 100;
        public const int CloseAt = 40;
        public const int Warmth = 2;
        public const int Cold = -20;
        public const int Chill = 6;

        public static int Read(string book, string id)
        {
            if (string.IsNullOrEmpty(book) || string.IsNullOrEmpty(id)) return 0;
            string[] rows = book.Split('|');
            for (int i = 0; i < rows.Length; i++)
            {
                string row = rows[i];
                int colon = row.IndexOf(':');
                if (colon <= 0) continue;
                if (row.Substring(0, colon) != id) continue;
                return Clamp(Parse(row.Substring(colon + 1)));
            }
            return 0;
        }

        public static string Write(string book, string id, int score)
        {
            if (string.IsNullOrEmpty(id)) return book ?? "";
            score = Clamp(score);
            var rows = new List<string>();
            bool found = false;
            if (!string.IsNullOrEmpty(book))
            {
                string[] parts = book.Split('|');
                for (int i = 0; i < parts.Length; i++)
                {
                    string row = parts[i];
                    int colon = row.IndexOf(':');
                    if (colon <= 0) continue;
                    string key = row.Substring(0, colon);
                    if (key == id)
                    {
                        rows.Add(id + ":" + score);
                        found = true;
                    }
                    else rows.Add(row);
                }
            }
            if (!found) rows.Add(id + ":" + score);
            return string.Join("|", rows);
        }

        public static string Shift(string book, string id, int delta)
        {
            return Write(book, id, Read(book, id) + delta);
        }

        public static string ChillToward(string book, IList<ColonistDay> people, string selfId, bool leaderPresent, int leadership)
        {
            if (people == null || string.IsNullOrEmpty(selfId)) return book ?? "";
            int drop = MealTable.FeudShift(Chill, leaderPresent, leadership);
            if (drop <= 0) return book ?? "";
            for (int i = 0; i < people.Count; i++)
            {
                var other = people[i];
                if (other == null || !other.alive || other.id == selfId || string.IsNullOrEmpty(other.id)) continue;
                book = Shift(book, other.id, -drop);
            }
            return book ?? "";
        }

        public static bool Quarrel(IList<ColonistDay> people, bool leaderPresent)
        {
            if (leaderPresent || people == null) return false;
            int living = 0;
            for (int i = 0; i < people.Count; i++)
            {
                if (people[i] != null && people[i].alive) living++;
            }
            if (living < 2) return false;
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || !person.alive) continue;
                for (int j = 0; j < people.Count; j++)
                {
                    var other = people[j];
                    if (other == null || !other.alive || other.id == person.id) continue;
                    if (Read(person.kin, other.id) <= Cold) return true;
                }
            }
            return false;
        }

        public static bool Close(string book, string id)
        {
            return Read(book, id) >= CloseAt;
        }

        public static string Warm(string book, IList<ColonistDay> people, string selfId, string task, bool loner)
        {
            if (loner || people == null || string.IsNullOrEmpty(selfId)) return book ?? "";
            if (task == "Rest" || task == "Lead" || task == "Fallen" || task == "Left" || task == "Quarantine") return book ?? "";
            for (int i = 0; i < people.Count; i++)
            {
                var other = people[i];
                if (other == null || !other.alive || other.id == selfId) continue;
                if (other.task != task || string.IsNullOrEmpty(other.id)) continue;
                book = Shift(book, other.id, Warmth);
            }
            return book ?? "";
        }

        public static string Closest(string book)
        {
            if (string.IsNullOrEmpty(book)) return "";
            string bestId = "";
            int best = CloseAt - 1;
            string[] rows = book.Split('|');
            for (int i = 0; i < rows.Length; i++)
            {
                string row = rows[i];
                int colon = row.IndexOf(':');
                if (colon <= 0) continue;
                int score = Clamp(Parse(row.Substring(colon + 1)));
                if (score < CloseAt || score <= best) continue;
                best = score;
                bestId = row.Substring(0, colon);
            }
            return bestId;
        }

        public static string FallenId(IList<ColonistDay> people, string fallenName)
        {
            if (people == null || string.IsNullOrEmpty(fallenName)) return "";
            string first = First(fallenName);
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || string.IsNullOrEmpty(person.id)) continue;
                if (person.id == fallenName || person.id == first) return person.id;
                if (string.IsNullOrEmpty(person.name)) continue;
                if (person.name == fallenName || First(person.name) == first) return person.id;
            }
            return "";
        }

        public static bool Grieves(string bond, string kin, string fallenName, string fallenId)
        {
            string first = First(fallenName);
            if (first.Length > 0 && !string.IsNullOrEmpty(bond) && bond.IndexOf(first, StringComparison.Ordinal) >= 0) return true;
            if (Close(kin, fallenId)) return true;
            if (first.Length > 0 && Close(kin, first)) return true;
            return false;
        }

        private static string First(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int space = name.IndexOf(' ');
            return space < 0 ? name : name.Substring(0, space);
        }

        private static int Clamp(int score)
        {
            if (score < Floor) return Floor;
            if (score > Cap) return Cap;
            return score;
        }

        private static int Parse(string token)
        {
            if (string.IsNullOrEmpty(token)) return 0;
            int sign = 1;
            int start = 0;
            if (token[0] == '-')
            {
                sign = -1;
                start = 1;
            }
            int value = 0;
            for (int i = start; i < token.Length; i++)
            {
                char c = token[i];
                if (c < '0' || c > '9') break;
                value = value * 10 + (c - '0');
            }
            return value * sign;
        }
    }
}
