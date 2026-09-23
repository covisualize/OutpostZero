using System.Collections.Generic;
using System.Text;

namespace OutpostZero.Shell
{
    /// <summary>
    /// The string table as key,en,es rows for translators. Fields are quoted when they hold a
    /// comma, quote or line break, so the file opens cleanly in a spreadsheet. A translator adds a
    /// column headed with a language code (and an optional <see cref="NameRow"/> row naming it);
    /// <see cref="Import"/> turns each such column into a language pack.
    /// </summary>
    public static class LocCsv
    {
        public const string Header = "key,en,es";
        public static readonly string[] Languages = { "en", "es" };
        public const string NameRow = "@name";

        public static string Export()
        {
            var columns = new List<string>(Languages);
            columns.AddRange(Loc.Packs);
            var builder = new StringBuilder();
            builder.Append(Header);
            for (int i = Languages.Length; i < columns.Count; i++) builder.Append(',').Append(Field(columns[i]));
            builder.Append('\n');
            if (columns.Count > Languages.Length)
            {
                builder.Append(NameRow).Append(",English,Español");
                for (int i = Languages.Length; i < columns.Count; i++) builder.Append(',').Append(Field(Loc.PackName(columns[i])));
                builder.Append('\n');
            }
            var keys = new List<string>(Loc.Keys);
            keys.Sort(System.StringComparer.Ordinal);
            foreach (var key in keys)
            {
                builder.Append(Field(key));
                foreach (var language in columns)
                    builder.Append(',').Append(Field(Loc.Has(key, language) ? Loc.Raw(key, language) : ""));
                builder.Append('\n');
            }
            return builder.ToString();
        }

        public class Pack
        {
            public string Code;
            public string Name;
            public readonly Dictionary<string, string> Lines = new Dictionary<string, string>();
        }

        /// <summary>A pack code is a lowercase ISO 639 code, optionally with a region or script: ru, pt-br, zh-hans.</summary>
        public static bool ValidCode(string code)
        {
            if (string.IsNullOrEmpty(code) || code == "en" || code == "es" || code == PseudoLoc.Code) return false;
            int dash = code.IndexOf('-');
            string head = dash < 0 ? code : code.Substring(0, dash);
            if (head.Length < 2 || head.Length > 3) return false;
            for (int i = 0; i < code.Length; i++)
            {
                char c = code[i];
                bool letter = c >= 'a' && c <= 'z';
                if (!letter && !(c == '-' && i == dash && i < code.Length - 1)) return false;
            }
            return dash < 0 || code.Length - dash - 1 >= 2 && code.Length - dash - 1 <= 4;
        }

        /// <summary>
        /// Reads translator columns from an exported sheet. The en and es columns are ignored. A blank cell
        /// leaves the English line in place. Unknown keys and lines whose {tokens} differ from English are
        /// dropped and reported, so a typo cannot break a key prompt.
        /// </summary>
        public static List<Pack> Import(string text, List<string> problems)
        {
            var packs = new List<Pack>();
            var rows = Parse(text);
            if (rows.Count == 0 || rows[0].Length < 2 || rows[0][0] != "key" || rows[0][1] != "en")
            {
                problems?.Add("the first row must start with key,en");
                return packs;
            }
            var columns = new Dictionary<int, Pack>();
            for (int c = 2; c < rows[0].Length; c++)
            {
                string code = rows[0][c].Trim().ToLowerInvariant().Replace('_', '-');
                if (code == "es" || code.Length == 0) continue;
                if (!ValidCode(code))
                {
                    problems?.Add("column " + (c + 1) + " has an unusable language code '" + rows[0][c] + "'");
                    continue;
                }
                var pack = new Pack { Code = code, Name = code.ToUpperInvariant() };
                columns[c] = pack;
                packs.Add(pack);
            }
            for (int r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                if (row.Length == 0 || row[0].Length == 0) continue;
                string key = row[0];
                foreach (var column in columns)
                {
                    if (column.Key >= row.Length) continue;
                    string line = row[column.Key];
                    if (line.Length == 0) continue;
                    if (key == NameRow)
                    {
                        column.Value.Name = line.Trim();
                        continue;
                    }
                    if (!Loc.Has(key, "en"))
                    {
                        problems?.Add(column.Value.Code + ": unknown key " + key);
                        continue;
                    }
                    if (!SameTokens(Loc.Raw(key, "en"), line))
                    {
                        problems?.Add(column.Value.Code + ": " + key + " changes a {token}");
                        continue;
                    }
                    column.Value.Lines[key] = line;
                }
            }
            return packs;
        }

        public static bool SameTokens(string english, string translated)
        {
            var want = Tokens(english);
            var have = Tokens(translated);
            want.Sort(System.StringComparer.Ordinal);
            have.Sort(System.StringComparer.Ordinal);
            if (want.Count != have.Count) return false;
            for (int i = 0; i < want.Count; i++)
                if (want[i] != have[i]) return false;
            return true;
        }

        private static List<string> Tokens(string text)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(text)) return list;
            int at = 0;
            while ((at = text.IndexOf('{', at)) >= 0)
            {
                int end = text.IndexOf('}', at);
                if (end < 0) break;
                list.Add(text.Substring(at, end - at + 1));
                at = end + 1;
            }
            return list;
        }

        public static string Field(string value)
        {
            value = value ?? "";
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public static List<string[]> Parse(string text)
        {
            var rows = new List<string[]>();
            if (string.IsNullOrEmpty(text)) return rows;
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else if (c == '"') quoted = false;
                    else field.Append(c);
                    continue;
                }
                if (c == '"') quoted = true;
                else if (c == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\n' || c == '\r')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row.ToArray());
                    row.Clear();
                }
                else field.Append(c);
            }
            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row.ToArray());
            }
            return rows;
        }
    }
}
