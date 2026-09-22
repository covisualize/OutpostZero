using System;
using System.Collections.Generic;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Spots already opened on a district. An empty mark stays empty. A held mark
    /// keeps the stacks that were left. Another district keeps its own list.
    /// The campaign save stays schema 1.
    /// </summary>
    public static class StreetLedger
    {
        public static string Mark(string role, float x, float z)
        {
            if (string.IsNullOrEmpty(role)) role = "crate";
            int sx = (int)Math.Round(x * 10d);
            int sz = (int)Math.Round(z * 10d);
            return role + "@" + sx + "," + sz;
        }

        public static string Note(string packed, string district, string mark)
        {
            packed = Drop(packed, district, mark);
            if (string.IsNullOrEmpty(district) || string.IsNullOrEmpty(mark)) return packed ?? "";
            var list = Split(packed);
            list.Add(district + "=" + mark);
            list.Sort(StringComparer.Ordinal);
            return Join(list);
        }

        public static string Hold(string packed, string district, string mark, string body)
        {
            if (string.IsNullOrEmpty(body)) return Note(packed, district, mark);
            packed = Drop(packed, district, mark);
            if (string.IsNullOrEmpty(district) || string.IsNullOrEmpty(mark)) return packed ?? "";
            body = body.Replace("|", ";").Replace("=", "").Replace("~", "");
            var list = Split(packed);
            list.Add(district + "=" + mark + "~" + body);
            list.Sort(StringComparer.Ordinal);
            return Join(list);
        }

        public static string Read(string packed, string district, string mark)
        {
            if (string.IsNullOrEmpty(packed) || string.IsNullOrEmpty(district) || string.IsNullOrEmpty(mark)) return null;
            string gone = district + "=" + mark;
            string prefix = gone + "~";
            var list = Split(packed);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == gone) return "";
                if (list[i].StartsWith(prefix, StringComparison.Ordinal)) return list[i].Substring(prefix.Length);
            }
            return null;
        }

        public static bool Has(string packed, string district, string mark)
        {
            if (string.IsNullOrEmpty(packed) || string.IsNullOrEmpty(district) || string.IsNullOrEmpty(mark)) return false;
            string key = district + "=" + mark;
            var list = Split(packed);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == key) return true;
            }
            return false;
        }

        public static int Count(string packed, string district)
        {
            if (string.IsNullOrEmpty(packed) || string.IsNullOrEmpty(district)) return 0;
            string prefix = district + "=";
            var list = Split(packed);
            int count = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].StartsWith(prefix, StringComparison.Ordinal)) count++;
            }
            return count;
        }

        private static string Drop(string packed, string district, string mark)
        {
            packed = packed ?? "";
            if (string.IsNullOrEmpty(district) || string.IsNullOrEmpty(mark)) return packed;
            string gone = district + "=" + mark;
            string prefix = gone + "~";
            var list = Split(packed);
            bool changed = false;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] != gone && !list[i].StartsWith(prefix, StringComparison.Ordinal)) continue;
                list.RemoveAt(i);
                changed = true;
            }
            if (!changed) return packed;
            return Join(list);
        }

        private static List<string> Split(string packed)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(packed)) return list;
            var bits = packed.Split('|');
            for (int i = 0; i < bits.Length; i++)
            {
                if (!string.IsNullOrEmpty(bits[i])) list.Add(bits[i]);
            }
            return list;
        }

        private static string Join(List<string> list)
        {
            if (list.Count == 0) return "";
            var text = list[0];
            for (int i = 1; i < list.Count; i++) text += "|" + list[i];
            return text;
        }
    }
}
