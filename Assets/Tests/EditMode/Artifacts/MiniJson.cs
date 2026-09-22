using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace OutpostZero.Tests.EditMode.Artifacts
{
    /// <summary>
    /// Enough JSON for the manifest and sidecars without JsonUtility, so the artifact suite also runs off-engine.
    /// Objects become dictionaries, arrays lists, numbers doubles.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string text)
        {
            int at = 0;
            object value = Value(text, ref at);
            Skip(text, ref at);
            if (at != text.Length) throw new FormatException("trailing text at " + at);
            return value;
        }

        public static Dictionary<string, object> Object(object value) => value as Dictionary<string, object> ?? new Dictionary<string, object>();

        public static List<object> List(object value) => value as List<object> ?? new List<object>();

        public static string Str(Dictionary<string, object> node, string key) =>
            node != null && node.TryGetValue(key, out var value) ? value as string ?? "" : "";

        public static double Num(Dictionary<string, object> node, string key) =>
            node != null && node.TryGetValue(key, out var value) && value is double number ? number : 0d;

        public static List<string> Strings(Dictionary<string, object> node, string key)
        {
            var result = new List<string>();
            if (node == null || !node.TryGetValue(key, out var value)) return result;
            foreach (var item in List(value))
            {
                if (item is string text) result.Add(text);
            }
            return result;
        }

        private static object Value(string text, ref int at)
        {
            Skip(text, ref at);
            if (at >= text.Length) throw new FormatException("unexpected end");
            char c = text[at];
            if (c == '{') return ReadObject(text, ref at);
            if (c == '[') return ReadArray(text, ref at);
            if (c == '"') return ReadString(text, ref at);
            if (Word(text, ref at, "true")) return true;
            if (Word(text, ref at, "false")) return false;
            if (Word(text, ref at, "null")) return null;
            int start = at;
            while (at < text.Length && "+-0123456789.eE".IndexOf(text[at]) >= 0) at++;
            if (start == at) throw new FormatException("unexpected '" + c + "' at " + at);
            return double.Parse(text.Substring(start, at - start), CultureInfo.InvariantCulture);
        }

        private static Dictionary<string, object> ReadObject(string text, ref int at)
        {
            var node = new Dictionary<string, object>();
            at++;
            Skip(text, ref at);
            if (at < text.Length && text[at] == '}')
            {
                at++;
                return node;
            }
            while (true)
            {
                Skip(text, ref at);
                string key = ReadString(text, ref at);
                Skip(text, ref at);
                Expect(text, ref at, ':');
                node[key] = Value(text, ref at);
                Skip(text, ref at);
                if (at < text.Length && text[at] == ',')
                {
                    at++;
                    continue;
                }
                Expect(text, ref at, '}');
                return node;
            }
        }

        private static List<object> ReadArray(string text, ref int at)
        {
            var list = new List<object>();
            at++;
            Skip(text, ref at);
            if (at < text.Length && text[at] == ']')
            {
                at++;
                return list;
            }
            while (true)
            {
                list.Add(Value(text, ref at));
                Skip(text, ref at);
                if (at < text.Length && text[at] == ',')
                {
                    at++;
                    continue;
                }
                Expect(text, ref at, ']');
                return list;
            }
        }

        private static string ReadString(string text, ref int at)
        {
            Expect(text, ref at, '"');
            var builder = new StringBuilder();
            while (at < text.Length)
            {
                char c = text[at++];
                if (c == '"') return builder.ToString();
                if (c != '\\')
                {
                    builder.Append(c);
                    continue;
                }
                char e = text[at++];
                switch (e)
                {
                    case 'n': builder.Append('\n'); break;
                    case 't': builder.Append('\t'); break;
                    case 'r': builder.Append('\r'); break;
                    case 'b': builder.Append('\b'); break;
                    case 'f': builder.Append('\f'); break;
                    case 'u':
                        builder.Append((char)int.Parse(text.Substring(at, 4), NumberStyles.HexNumber));
                        at += 4;
                        break;
                    default: builder.Append(e); break;
                }
            }
            throw new FormatException("unterminated string");
        }

        private static bool Word(string text, ref int at, string word)
        {
            if (string.CompareOrdinal(text, at, word, 0, word.Length) != 0) return false;
            at += word.Length;
            return true;
        }

        private static void Expect(string text, ref int at, char c)
        {
            if (at >= text.Length || text[at] != c) throw new FormatException("expected '" + c + "' at " + at);
            at++;
        }

        private static void Skip(string text, ref int at)
        {
            while (at < text.Length && char.IsWhiteSpace(text[at])) at++;
        }
    }
}
