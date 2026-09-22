using System.Collections.Generic;
using System.Text;

namespace OutpostZero.Shell
{
    /// <summary>
    /// The string table as key,en,es rows for translators. Fields are quoted when they hold a
    /// comma, quote or line break, so the file opens cleanly in a spreadsheet.
    /// </summary>
    public static class LocCsv
    {
        public const string Header = "key,en,es";
        public static readonly string[] Languages = { "en", "es" };

        public static string Export()
        {
            var builder = new StringBuilder();
            builder.Append(Header).Append('\n');
            var keys = new List<string>(Loc.Keys);
            keys.Sort(System.StringComparer.Ordinal);
            foreach (var key in keys)
            {
                builder.Append(Field(key));
                foreach (var language in Languages)
                    builder.Append(',').Append(Field(Loc.Has(key, language) ? Loc.Raw(key, language) : ""));
                builder.Append('\n');
            }
            return builder.ToString();
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
