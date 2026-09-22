using System;
using System.Collections.Generic;

namespace OutpostZero.Shell
{
    /// <summary>
    /// Spots already emptied on a district. A later trip to that street finds them empty.
    /// Another district keeps its own list. The campaign save stays schema 1.
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
            packed = packed ?? "";
            if (string.IsNullOrEmpty(district) || string.IsNullOrEmpty(mark)) return packed;
            if (Has(packed, district, mark)) return packed;
            var list = Split(packed);
            list.Add(district + "=" + mark);
            list.Sort(StringComparer.Ordinal);
            return Join(list);
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
