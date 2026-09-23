using System;
using System.Collections.Generic;
using System.Globalization;
using OutpostZero.Items;

namespace OutpostZero.Colony
{
    /// <summary>
    /// What a faction's stall holds on a given day. Each visit rolls the faction's loot table once, plus once
    /// more for every 8 days survived (up to 4 rolls), keeping only items on its table; trust adds the premium
    /// item. Each purchase takes one unit off the day's shelf. A faction whose table id names no loot table
    /// stocks each item once per roll.
    /// </summary>
    public static class StallShelf
    {
        public const int RollCap = 4;
        public const int DaysPerRoll = 8;
        public const string TablePrefix = "stall_";

        public static int Rolls(int day)
        {
            if (day <= 1) return 1;
            return Math.Min(RollCap, 1 + (day - 1) / DaysPerRoll);
        }

        public static string CodeTable(string faction) => CaravanBook.IsBuiltIn(faction) ? TablePrefix + faction : "";

        public static int PremiumCount(int rolls) => 1 + (Math.Max(1, rolls) - 1) / 2;

        /// <summary>Stable across runtimes, unlike <c>string.GetHashCode</c>.</summary>
        public static int Salt(string faction, int day, int roll)
        {
            unchecked
            {
                uint hash = 2166136261;
                string key = (faction ?? "") + "|" + day.ToString(CultureInfo.InvariantCulture) + "|" + roll.ToString(CultureInfo.InvariantCulture);
                for (int i = 0; i < key.Length; i++)
                {
                    hash ^= key[i];
                    hash *= 16777619;
                }
                return (int)(hash & 0x7fffffff);
            }
        }

        /// <summary>The day's shelf in table order, every table item listed (zero when the rolls missed it).</summary>
        public static List<KeyValuePair<string, int>> Roll(string faction, int day, int standing)
        {
            string[] stock = CaravanBook.Stock(faction);
            var counts = new Dictionary<string, int>();
            foreach (string item in stock) counts[item] = 0;
            int rolls = Rolls(day);
            string table = CaravanBook.Table(faction);
            bool known = !string.IsNullOrEmpty(table) && Has(table);
            for (int r = 0; r < rolls; r++)
            {
                if (!known)
                {
                    foreach (string item in stock) counts[item]++;
                    continue;
                }
                foreach (var grant in LootTables.Drops(table, Salt(faction, day, r)))
                    if (counts.ContainsKey(grant.ItemId)) counts[grant.ItemId] += grant.Count;
            }
            var shelf = new List<KeyValuePair<string, int>>();
            foreach (string item in stock) shelf.Add(new KeyValuePair<string, int>(item, counts[item]));
            string premium = CaravanBook.Premium(faction);
            if (standing >= CaravanBook.Trusted && !string.IsNullOrEmpty(premium) && !counts.ContainsKey(premium))
                shelf.Add(new KeyValuePair<string, int>(premium, PremiumCount(rolls)));
            return shelf;
        }

        public static int Left(List<KeyValuePair<string, int>> shelf, string sold, int day, string faction, string itemId)
        {
            if (shelf == null) return 0;
            foreach (var pair in shelf)
                if (pair.Key == itemId) return Math.Max(0, pair.Value - Sold(sold, day, faction, itemId));
            return 0;
        }

        /// <summary>How many of an item went today, from <c>day:faction:item=n,item=n</c>; another day or faction reads 0.</summary>
        public static int Sold(string packed, int day, string faction, string itemId)
        {
            if (!Today(packed, day, faction, out string body)) return 0;
            foreach (string part in body.Split(','))
            {
                int cut = part.IndexOf('=');
                if (cut <= 0 || part.Substring(0, cut) != itemId) continue;
                return int.TryParse(part.Substring(cut + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0 ? n : 0;
            }
            return 0;
        }

        /// <summary>Records one more sale; a sale on a new day or at another faction starts a fresh record.</summary>
        public static string MarkSold(string packed, int day, string faction, string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || !FactionTable.ValidId(faction)) return packed ?? "";
            var counts = new List<KeyValuePair<string, int>>();
            if (Today(packed, day, faction, out string body))
                foreach (string part in body.Split(','))
                {
                    int cut = part.IndexOf('=');
                    if (cut <= 0) continue;
                    if (int.TryParse(part.Substring(cut + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n > 0)
                        counts.Add(new KeyValuePair<string, int>(part.Substring(0, cut), n));
                }
            int at = counts.FindIndex(pair => pair.Key == itemId);
            if (at >= 0) counts[at] = new KeyValuePair<string, int>(itemId, counts[at].Value + 1);
            else counts.Add(new KeyValuePair<string, int>(itemId, 1));
            var bits = new List<string>();
            foreach (var pair in counts) bits.Add(pair.Key + "=" + pair.Value.ToString(CultureInfo.InvariantCulture));
            return day.ToString(CultureInfo.InvariantCulture) + ":" + faction + ":" + string.Join(",", bits);
        }

        private static bool Today(string packed, int day, string faction, out string body)
        {
            body = "";
            if (string.IsNullOrEmpty(packed)) return false;
            string head = day.ToString(CultureInfo.InvariantCulture) + ":" + faction + ":";
            if (!packed.StartsWith(head, StringComparison.Ordinal)) return false;
            body = packed.Substring(head.Length);
            return true;
        }

        private static bool Has(string table)
        {
            foreach (string id in LootTables.Ids)
                if (id == table) return true;
            return false;
        }
    }
}
