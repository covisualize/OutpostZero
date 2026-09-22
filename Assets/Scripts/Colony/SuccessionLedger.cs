using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Permadeath handoff: grief, a memorial line, and a corpse that can be found again.
    /// </summary>
    public static class SuccessionLedger
    {
        public const float CampLoss = 25f;
        public const float FriendLoss = 40f;
        public const int MercyScrap = 18;
        public const int MercyInjury = 2;

        public class Memorial
        {
            public string name;
            public int day;
            public int kills;
            public string cause;
            public string district;
        }

        public class CorpseMark
        {
            public string district;
            public float x;
            public float y;
            public float z;
            public string name;
            public string gear;
            public bool recovered;
        }

        public static void Grieve(IList<ColonistDay> people, string fallenName)
        {
            if (people == null) return;
            string fallenId = KinBoard.FallenId(people, fallenName);
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || !person.alive) continue;
                bool friend = KinBoard.Grieves(person.bond, person.kin, fallenName, fallenId);
                person.morale = Math.Max(0f, person.morale - (friend ? FriendLoss : CampLoss));
            }
        }

        public static string Outcome(bool merciful, int livingAfterDeath)
        {
            if (merciful) return "wounded";
            if (livingAfterDeath > 0) return "succession";
            return "wiped";
        }

        public static void NextMorning(int day, out int nextDay, out float nextHour)
        {
            nextDay = Math.Max(1, day) + 1;
            nextHour = 6.5f;
        }

        public static string Card(Memorial row)
        {
            if (row == null) return "";
            return row.name + "  day " + row.day + "  kills " + row.kills + "  " + row.cause;
        }

        public static string FirstName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int space = name.IndexOf(' ');
            return space < 0 ? name : name.Substring(0, space);
        }

        public static string PackMemorials(IList<Memorial> rows)
        {
            if (rows == null || rows.Count == 0) return "";
            var parts = new string[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                parts[i] = Clean(row.name) + "^" + row.day + "^" + row.kills + "^" + Clean(row.cause) + "^" + Clean(row.district);
            }
            return string.Join(";", parts);
        }

        public static List<Memorial> UnpackMemorials(string packed)
        {
            var list = new List<Memorial>();
            if (string.IsNullOrEmpty(packed)) return list;
            string[] rows = packed.Split(';');
            for (int i = 0; i < rows.Length; i++)
            {
                string[] fields = rows[i].Split('^');
                if (fields.Length < 5) continue;
                int.TryParse(fields[1], out int day);
                int.TryParse(fields[2], out int kills);
                list.Add(new Memorial
                {
                    name = fields[0],
                    day = day,
                    kills = kills,
                    cause = fields[3],
                    district = fields[4]
                });
            }
            return list;
        }

        public static string PackCorpses(IList<CorpseMark> rows)
        {
            if (rows == null || rows.Count == 0) return "";
            var parts = new string[rows.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                parts[i] = string.Join("^", new[]
                {
                    Clean(row.district),
                    row.x.ToString("0.###", CultureInfo.InvariantCulture),
                    row.y.ToString("0.###", CultureInfo.InvariantCulture),
                    row.z.ToString("0.###", CultureInfo.InvariantCulture),
                    Clean(row.name),
                    CleanGear(row.gear),
                    row.recovered ? "1" : "0"
                });
            }
            return string.Join(";", parts);
        }

        public static List<CorpseMark> UnpackCorpses(string packed)
        {
            var list = new List<CorpseMark>();
            if (string.IsNullOrEmpty(packed)) return list;
            string[] rows = packed.Split(';');
            for (int i = 0; i < rows.Length; i++)
            {
                string[] fields = rows[i].Split('^');
                if (fields.Length < 7) continue;
                float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
                float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
                float.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z);
                list.Add(new CorpseMark
                {
                    district = fields[0],
                    x = x,
                    y = y,
                    z = z,
                    name = fields[4],
                    gear = fields[5],
                    recovered = fields[6] == "1"
                });
            }
            return list;
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("^", "").Replace(";", "");
        }

        private static string CleanGear(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            return value.Replace("^", "").Replace(";", "");
        }
    }

    public class FallenGear : MonoBehaviour, IInteractable
    {
        private int index = -1;
        private string gear = "";
        private bool taken;

        public string Prompt => taken ? string.Empty : StreetAsk.Gear(null);

        public void Configure(int corpseIndex, string packedGear)
        {
            index = corpseIndex;
            gear = packedGear ?? "";
        }

        public bool CanInteract(PlayerInventory inventory) => !taken && inventory != null;

        public void Interact(PlayerInventory inventory)
        {
            if (!CanInteract(inventory)) return;
            taken = true;
            inventory.RestoreGear(gear);
            SurvivorRoster.Instance?.Recover(index);
            GameplayFeedback.Toast(StreetAsk.Kept(!string.IsNullOrEmpty(gear), null));
            Destroy(gameObject);
        }
    }
}
