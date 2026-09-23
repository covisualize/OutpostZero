using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using OutpostZero.Core;
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
        public const float MemorialEase = 10f;

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

        public const char ArmMark = '@';

        /// <summary>
        /// The fallen leader's personal weapon (the one in hand) stays on the body, unless it is the only one
        /// they carried: the heir still walks out armed.
        /// </summary>
        public static int Personal(IList<LeaderKit.Arm> arms, int active)
        {
            if (arms == null || arms.Count < 2) return -1;
            return Mathf.Clamp(active, 0, arms.Count - 1);
        }

        /// <summary>Adds the weapon to the body's packed gear as <c>@id:magazine:reserve</c>, which the pack's reader skips.</summary>
        public static string WithArm(string gear, LeaderKit.Arm arm)
        {
            string id = (arm.Id ?? "").Replace("+", "").Replace(":", "").Replace("^", "").Replace(";", "").Replace(ArmMark.ToString(), "");
            if (id.Length == 0) return gear ?? "";
            string token = ArmMark + id + ":" + Mathf.Max(0, arm.Magazine).ToString(CultureInfo.InvariantCulture) + ":" + Mathf.Max(0, arm.Reserve).ToString(CultureInfo.InvariantCulture);
            return string.IsNullOrEmpty(gear) ? token : gear + "+" + token;
        }

        public static bool SplitArm(string gear, out string pack, out LeaderKit.Arm arm)
        {
            pack = gear ?? "";
            arm = new LeaderKit.Arm { Id = "" };
            if (string.IsNullOrEmpty(gear)) return false;
            var kept = new List<string>();
            bool found = false;
            foreach (string part in gear.Split('+'))
            {
                if (part.Length > 1 && part[0] == ArmMark && !found)
                {
                    string[] bits = part.Substring(1).Split(':');
                    if (bits.Length == 3 && bits[0].Length > 0
                        && int.TryParse(bits[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int magazine)
                        && int.TryParse(bits[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int reserve))
                    {
                        arm = new LeaderKit.Arm { Id = bits[0], Magazine = Mathf.Max(0, magazine), Reserve = Mathf.Max(0, reserve) };
                        found = true;
                        continue;
                    }
                }
                if (part.Length > 0) kept.Add(part);
            }
            pack = string.Join("+", kept);
            return found;
        }

        public static void Grieve(IList<ColonistDay> people, string fallenName) => Grieve(people, fallenName, false);

        /// <summary>A finished memorial wall gives the camp somewhere to put a name, so each loss cuts less deep.</summary>
        public static float Loss(bool friend, bool memorial)
        {
            float loss = friend ? FriendLoss : CampLoss;
            return memorial ? loss - MemorialEase : loss;
        }

        public static void Grieve(IList<ColonistDay> people, string fallenName, bool memorial)
        {
            if (people == null) return;
            string fallenId = KinBoard.FallenId(people, fallenName);
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || !person.alive) continue;
                bool friend = KinBoard.Grieves(person.bond, person.kin, fallenName, fallenId);
                person.morale = Math.Max(0f, person.morale - Loss(friend, memorial));
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
            bool armed = SuccessionLedger.SplitArm(gear, out string pack, out var arm);
            inventory.RestoreGear(pack);
            if (armed) HandBack(inventory, arm);
            SurvivorRoster.Instance?.Recover(index);
            GameplayFeedback.Toast(StreetAsk.Kept(!string.IsNullOrEmpty(gear), null));
            Destroy(gameObject);
        }

        /// <summary>The fallen leader's gun goes into the hand; one of the same type already carried is left on the ground in its place.</summary>
        private void HandBack(PlayerInventory inventory, LeaderKit.Arm arm)
        {
            var player = inventory.GetComponent<PlayerController>();
            if (player != null && player.TakeFromGround(arm.Id, arm.Magazine, arm.Reserve, out string leftId, out int leftMag, out int leftReserve))
            {
                if (string.IsNullOrEmpty(leftId)) return;
                arm = new LeaderKit.Arm { Id = leftId, Magazine = leftMag, Reserve = leftReserve };
            }
            var dropped = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dropped.name = "GroundWeapon_Fallen";
            dropped.transform.position = transform.position + new Vector3(0.6f, 0.05f, 0f);
            dropped.transform.localScale = new Vector3(0.6f, 0.1f, 0.16f);
            dropped.layer = GameLayers.Interactable;
            dropped.AddComponent<GroundWeapon>().Configure(arm.Id, arm.Magazine, arm.Reserve);
        }
    }
}
