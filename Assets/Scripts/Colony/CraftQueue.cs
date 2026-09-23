using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Bench orders the leader leaves for the camp. Materials are spent when the order goes in; a survivor
    /// on the Craft task works through them at the end of the day, a skilled hand faster.
    /// Only recipes that make a thing can be queued. Fitting a mod, bracing a wall or mending the generator
    /// happens at once, because it needs the object in front of the leader.
    /// </summary>
    public static class CraftQueue
    {
        public const string Task = "Craft";
        public const int Cap = 6;

        public static bool Orderable(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return false;
            switch (recipeId)
            {
                case "suppressor":
                case "rail":
                case "optic":
                case "extended_mag":
                case "repair_kit":
                case "barricade_kit":
                case "radio_spare":
                    return false;
            }
            return CraftBill.TryOf(recipeId, out _);
        }

        public static List<string> Unpack(string packed)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(packed)) return list;
            foreach (var part in packed.Split(','))
            {
                if (Orderable(part) && list.Count < Cap) list.Add(part);
            }
            return list;
        }

        public static string Pack(IList<string> orders)
        {
            if (orders == null || orders.Count == 0) return "";
            return string.Join(",", orders);
        }

        public static bool CanAdd(IReadOnlyList<string> orders, string recipeId)
        {
            return Orderable(recipeId) && (orders == null || orders.Count < Cap);
        }

        /// <summary>Orders one worker finishes in a shift: none when too low to work, one, or two from Engineering 4.</summary>
        public static int Hands(float morale, int engineering, bool bench)
        {
            if (!bench || morale < 10f) return 0;
            return engineering >= 4 ? 2 : 1;
        }

        public static string Line(IReadOnlyList<string> orders, System.Func<string, string> name)
        {
            if (orders == null || orders.Count == 0) return "";
            var parts = new string[orders.Count];
            for (int i = 0; i < orders.Count; i++) parts[i] = name != null ? name(orders[i]) : orders[i];
            return string.Join(", ", parts);
        }
    }
}
