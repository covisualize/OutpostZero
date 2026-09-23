using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace OutpostZero.Player
{
    /// <summary>
    /// What the leader carries between saves: the pack, the belt pockets, and each weapon slot with its
    /// rounds. Written as <c>gear/belt/arms/active</c>, where gear is the pack's <c>id*count+...</c>
    /// list, belt its four pocket ids joined by commas, and arms <c>id:magazine:reserve,...</c> in slot order.
    /// </summary>
    public static class LeaderKit
    {
        public const string SaveId = "leader_kit";

        public struct Arm
        {
            public string Id;
            public int Magazine;
            public int Reserve;
        }

        public struct Kit
        {
            public string Gear;
            public string[] Belt;
            public List<Arm> Arms;
            public int Active;
        }

        public static string Pack(Kit kit)
        {
            var belt = new string[Items.ItemBelt.Count];
            for (int i = 0; i < belt.Length; i++)
                belt[i] = Clean(kit.Belt != null && i < kit.Belt.Length ? kit.Belt[i] : "");
            var arms = new List<string>();
            if (kit.Arms != null)
            {
                foreach (var arm in kit.Arms)
                {
                    string id = Clean(arm.Id);
                    if (id.Length == 0) continue;
                    arms.Add(id + ":" + Mathf.Max(0, arm.Magazine).ToString(CultureInfo.InvariantCulture) + ":" + Mathf.Max(0, arm.Reserve).ToString(CultureInfo.InvariantCulture));
                }
            }
            return (kit.Gear ?? "").Replace("/", "") + "/" + string.Join(",", belt) + "/" + string.Join(",", arms) + "/" + Mathf.Max(0, kit.Active).ToString(CultureInfo.InvariantCulture);
        }

        public static bool TryUnpack(string packed, out Kit kit)
        {
            kit = new Kit { Gear = "", Belt = Items.ItemBelt.Fresh(), Arms = new List<Arm>(), Active = 0 };
            if (string.IsNullOrEmpty(packed)) return false;
            string[] parts = packed.Split('/');
            if (parts.Length != 4) return false;
            kit.Gear = parts[0];
            string[] pockets = parts[1].Split(',');
            for (int i = 0; i < kit.Belt.Length && i < pockets.Length; i++) kit.Belt[i] = pockets[i];
            if (parts[2].Length > 0)
            {
                foreach (string entry in parts[2].Split(','))
                {
                    string[] bits = entry.Split(':');
                    if (bits.Length != 3 || bits[0].Length == 0) continue;
                    if (!int.TryParse(bits[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int magazine)) continue;
                    if (!int.TryParse(bits[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int reserve)) continue;
                    kit.Arms.Add(new Arm { Id = bits[0], Magazine = Mathf.Max(0, magazine), Reserve = Mathf.Max(0, reserve) });
                }
            }
            int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int active);
            kit.Active = kit.Arms.Count == 0 ? 0 : Mathf.Clamp(active, 0, kit.Arms.Count - 1);
            return true;
        }

        private static string Clean(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            return id.Replace("/", "").Replace(",", "").Replace(":", "");
        }
    }
}
