using System;
using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>What the camp looks like at dawn, for deciding which random events can happen.</summary>
    public struct CampEventContext
    {
        public int Day;
        public bool Room;
        public bool Rain;
        public bool Feud;
        public bool Healthy;
        public bool MerchantAway;
        public bool Generator;
        public bool Water;
    }

    /// <summary>
    /// One random camp event: its weight and the conditions it waits on. What it does is in code keyed by
    /// <see cref="Id"/>; <see cref="CampEventBook"/> replaces the built-in rows with the authored assets.
    /// </summary>
    public struct CampEventRow
    {
        public string Id;
        public int Weight;
        public int MinDay;
        public string Module;
        public bool NeedsRain;
        public bool NeedsRoom;
        public bool NeedsFeud;
        public bool NeedsHealthy;
        public bool NeedsMerchantAway;
        public string Key;
        public string Fallback;
    }

    public static class CampEventTable
    {
        public const string Stranger = "stranger";
        public const string Argument = "argument";
        public const string Generator = "generator";
        public const string Sickness = "sickness";
        public const string RainFill = "rain_fill";
        public const string Merchant = "merchant";
        public const string Sighting = "sighting";
        public const string Cache = "cache";

        /// <summary>Chance in 100 that a dawn brings an event at all.</summary>
        public const int DailyChance = 55;
        public const int FeudAt = -10;
        public const int RepairScrap = 3;
        public const int RepairTape = 1;
        public const int RepairSkill = 3;
        public const int RainWater = 3;
        public const int CacheScrap = 3;
        public const int CacheCloth = 1;
        public const float ArgumentMood = 6f;
        public const int ArgumentKin = 5;

        public static readonly CampEventRow[] Code =
        {
            Row(Stranger, 3, 2, "", false, true, false, false, false, "event.stranger", "A stranger at the gate asks to join."),
            Row(Argument, 3, 2, "", false, false, true, false, false, "event.argument", "An old grudge boils over into a shouting match."),
            Row(Generator, 2, 3, "Generator", false, false, false, false, false, "event.generator", "The generator coughs and dies. It needs an engineer and parts."),
            Row(Sickness, 2, 3, "", false, false, false, true, false, "event.sickness", "Someone woke up burning with fever."),
            Row(RainFill, 3, 2, "Water", true, false, false, false, false, "event.rain_fill", "Rain filled the water collector overnight."),
            Row(Merchant, 2, 2, "", false, false, false, false, true, "event.merchant", "A caravan pulls up outside the gate."),
            Row(Sighting, 2, 4, "", false, false, false, false, false, "event.sighting", "The watch saw a pack moving close. Expect them tonight."),
            Row(Cache, 3, 2, "", false, false, false, false, false, "event.cache", "A scavenger's stash turned up by the fence."),
        };

        private static CampEventRow[] rows;

        public static bool FromAsset { get; private set; }
        public static CampEventRow[] Rows => rows ?? Code;

        public static CampEventRow Row(string id, int weight, int minDay, string module, bool rain, bool room, bool feud, bool healthy, bool merchantAway, string key, string fallback)
        {
            return new CampEventRow
            {
                Id = id,
                Weight = weight,
                MinDay = minDay,
                Module = module ?? "",
                NeedsRain = rain,
                NeedsRoom = room,
                NeedsFeud = feud,
                NeedsHealthy = healthy,
                NeedsMerchantAway = merchantAway,
                Key = key,
                Fallback = fallback,
            };
        }

        public static void Use(IEnumerable<CampEventRow> authored)
        {
            var list = new List<CampEventRow>();
            var seen = new HashSet<string>();
            if (authored != null)
                foreach (var row in authored)
                    if (!string.IsNullOrEmpty(row.Id) && row.Weight > 0 && seen.Add(row.Id)) list.Add(row);
            rows = list.Count > 0 ? list.ToArray() : null;
            FromAsset = rows != null;
        }

        public static void Clear()
        {
            rows = null;
            FromAsset = false;
        }

        public static CampEventRow Find(string id)
        {
            foreach (var row in Rows)
                if (row.Id == id) return row;
            return default;
        }

        public static bool Eligible(CampEventRow row, CampEventContext camp)
        {
            if (row.Weight <= 0 || camp.Day < row.MinDay) return false;
            if (row.Module == "Generator" && !camp.Generator) return false;
            if (row.Module == "Water" && !camp.Water) return false;
            if (row.NeedsRain && !camp.Rain) return false;
            if (row.NeedsRoom && !camp.Room) return false;
            if (row.NeedsFeud && !camp.Feud) return false;
            if (row.NeedsHealthy && !camp.Healthy) return false;
            if (row.NeedsMerchantAway && !camp.MerchantAway) return false;
            return true;
        }

        /// <summary>The same seed and day always roll the same event. An empty id means a quiet dawn.</summary>
        public static string Roll(CampEventContext camp, int seed)
        {
            int mix = Mix(seed, camp.Day);
            if (mix % 100 >= DailyChance) return "";
            int total = 0;
            foreach (var row in Rows)
                if (Eligible(row, camp)) total += row.Weight;
            if (total <= 0) return "";
            int pick = Mix(mix, 7919) % total;
            foreach (var row in Rows)
            {
                if (!Eligible(row, camp)) continue;
                if (pick < row.Weight) return row.Id;
                pick -= row.Weight;
            }
            return "";
        }

        public static bool CanRepair(int bestEngineering, int scrap, int tape)
        {
            return bestEngineering >= RepairSkill && scrap >= RepairScrap && tape >= RepairTape;
        }

        /// <summary>A carried generator part drops straight in: no engineer, scrap or tape needed.</summary>
        public static bool CanRepair(int bestEngineering, int scrap, int tape, int parts)
        {
            return parts > 0 || CanRepair(bestEngineering, scrap, tape);
        }

        public static int Mix(int seed, int salt)
        {
            unchecked
            {
                uint h = (uint)seed * 2654435761u ^ (uint)salt * 40503u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                h *= 3266489917u;
                h ^= h >> 16;
                return (int)(h & 0x7fffffff);
            }
        }
    }
}
