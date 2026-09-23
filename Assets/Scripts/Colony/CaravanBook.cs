using System;
using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Four factions, a visit calendar, and prices that move with reputation.
    /// </summary>
    public static class CaravanBook
    {
        public static readonly string[] Ids = { "caravan", "militia", "clinic", "farmers" };
        public const int Trusted = 30;
        public const int HaggleCap = 10;

        public static string Display(string id)
        {
            switch (id)
            {
                case "militia": return "Iron Militia";
                case "clinic": return "The Clinic";
                case "farmers": return "Free Farmers";
                default: return "The Caravan";
            }
        }

        public static string[] Stock(string id)
        {
            switch (id)
            {
                case "militia": return new[] { "ammo_rifle", "ammo_shells", "ammo_smg" };
                case "clinic": return new[] { "medkit", "bandage" };
                case "farmers": return new[] { "canned_food", "water" };
                default: return new[] { "bandage", "ammo_9mm", "medkit" };
            }
        }

        /// <summary>What a faction adds to its table once the camp is trusted.</summary>
        public static string Premium(string id)
        {
            switch (id)
            {
                case "militia": return "pipe_bomb";
                case "clinic": return "antibiotics";
                case "farmers": return "raw_food";
                default: return "flare";
            }
        }

        public static string[] Stock(string id, int standing)
        {
            string[] table = Stock(id);
            if (standing < Trusted) return table;
            var list = new List<string>(table) { Premium(id) };
            return list.ToArray();
        }

        public static int BasePrice(string itemId)
        {
            switch (itemId)
            {
                case "antibiotics": return 16;
                case "pipe_bomb": return 12;
                case "flare": return 8;
                case "raw_food": return 3;
                case "medkit": return 14;
                case "ammo_rifle": return 9;
                case "ammo_smg": return 7;
                case "ammo_shells": return 8;
                case "ammo_9mm": return 5;
                case "water": return 6;
                case "canned_food": return 5;
                case "bandage": return 4;
                default:
                    int value = CraftBill.Value(itemId);
                    return value > 0 ? value : 6;
            }
        }

        public static int Price(string itemId, int standing, bool leader)
        {
            int clamped = Math.Max(-100, Math.Min(100, standing));
            float scale = 1f - (clamped / 100f) * 0.3f;
            if (leader) scale *= 0.95f;
            return Math.Max(1, (int)Math.Round(BasePrice(itemId) * scale, MidpointRounding.AwayFromZero));
        }

        /// <summary>The leader haggles one percent off per point of Leadership, up to ten.</summary>
        public static int Price(string itemId, int standing, int leadership)
        {
            int clamped = Math.Max(-100, Math.Min(100, standing));
            float scale = 1f - (clamped / 100f) * 0.3f;
            if (leadership > 0) scale *= 1f - Math.Min(leadership, HaggleCap) * 0.01f;
            return Math.Max(1, (int)Math.Round(BasePrice(itemId) * scale, MidpointRounding.AwayFromZero));
        }

        /// <summary>Buy-back pays half the base price, a little more for a trusted camp.</summary>
        public static int Offer(string itemId, int standing)
        {
            int clamped = Math.Max(-100, Math.Min(100, standing));
            float scale = 1f + (clamped / 100f) * 0.1f;
            return Math.Max(1, (int)Math.Round(BasePrice(itemId) * 0.5f * scale, MidpointRounding.AwayFromZero));
        }

        public static bool Sellable(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || itemId == "scrap" || itemId.StartsWith("print_", StringComparison.Ordinal)) return false;
            for (int i = 0; i < Ids.Length; i++)
            {
                if (Premium(Ids[i]) == itemId || Array.IndexOf(Stock(Ids[i]), itemId) >= 0) return true;
            }
            return CraftBill.Value(itemId) > 0;
        }

        public static bool Refuses(string id, int standing) => id == "militia" && standing < -20;

        public static bool Ambush(int standing) => standing < -40;

        public static bool Visits(int day)
        {
            if (day <= 0) return false;
            int cursor = 0;
            int step = 0;
            while (cursor < day && step < 500)
            {
                cursor += Gap(step);
                if (cursor == day) return true;
                step++;
            }
            return false;
        }

        public static string Visitor(int day)
        {
            if (!Visits(day)) return "";
            int seen = 0;
            int cursor = 0;
            int step = 0;
            while (cursor < day && step < 500)
            {
                cursor += Gap(step);
                if (cursor == day) return Ids[seen % Ids.Length];
                seen++;
                step++;
            }
            return "";
        }

        public static string Counterparty(int day, bool tradingPost)
        {
            string visitor = Visitor(day);
            if (!string.IsNullOrEmpty(visitor)) return visitor;
            return tradingPost ? "caravan" : "";
        }

        public static void Decay(int[] standing)
        {
            if (standing == null) return;
            for (int i = 0; i < standing.Length; i++)
            {
                if (standing[i] > 0) standing[i]--;
                else if (standing[i] < 0) standing[i]++;
            }
        }

        public static void Gift(int[] standing, string id)
        {
            int index = IndexOf(id);
            if (standing == null || index < 0 || index >= standing.Length) return;
            standing[index] = Math.Max(-100, Math.Min(100, standing[index] + 2));
        }

        public static void Shift(int[] standing, string id, int delta)
        {
            int index = IndexOf(id);
            if (standing == null || index < 0 || index >= standing.Length) return;
            standing[index] = Math.Max(-100, Math.Min(100, standing[index] + delta));
        }

        public static string Pack(int[] standing)
        {
            if (standing == null || standing.Length == 0) return "";
            var parts = new string[Math.Min(Ids.Length, standing.Length)];
            for (int i = 0; i < parts.Length; i++) parts[i] = Ids[i] + "=" + standing[i];
            return string.Join(",", parts);
        }

        public static void Unpack(string packed, int legacy, int[] standing)
        {
            if (standing == null || standing.Length < Ids.Length) return;
            for (int i = 0; i < Ids.Length; i++) standing[i] = 0;
            if (string.IsNullOrEmpty(packed))
            {
                standing[0] = Math.Max(-100, Math.Min(100, legacy));
                return;
            }
            string[] parts = packed.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                int cut = parts[i].IndexOf('=');
                if (cut <= 0) continue;
                int index = IndexOf(parts[i].Substring(0, cut));
                if (index < 0) continue;
                if (int.TryParse(parts[i].Substring(cut + 1), out int value))
                    standing[index] = Math.Max(-100, Math.Min(100, value));
            }
        }

        public static string MarkQuest(string packed, string id)
        {
            var done = new HashSet<string>(SplitQuests(packed));
            done.Add(id);
            var list = new List<string>(done);
            list.Sort(StringComparer.Ordinal);
            return string.Join(",", list);
        }

        public static bool QuestDone(string packed, string id)
        {
            if (string.IsNullOrEmpty(packed) || string.IsNullOrEmpty(id)) return false;
            string[] parts = SplitQuests(packed);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == id) return true;
            }
            return false;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < Ids.Length; i++)
            {
                if (Ids[i] == id) return i;
            }
            return -1;
        }

        private static int Gap(int step)
        {
            int[] gaps = { 3, 4, 5 };
            return gaps[step % gaps.Length];
        }

        private static string[] SplitQuests(string packed)
        {
            if (string.IsNullOrEmpty(packed)) return Array.Empty<string>();
            return packed.Split(',');
        }
    }
}
