using OutpostZero.Shell;

namespace OutpostZero.Items
{
    /// <summary>
    /// Four consumable pockets. Weapon keys stay on 1-4, so these pockets answer 5-8.
    /// </summary>
    public static class ItemBelt
    {
        public const int Count = 4;

        public static string[] Fresh()
        {
            return new[] { "", "", "", "" };
        }

        public static bool Fits(string id)
        {
            var record = ItemCatalog.Find(id);
            if (record == null) return false;
            return record.Use != ItemUse.None && record.Use != ItemUse.Ammo && record.Use != ItemUse.Material;
        }

        public static int Toggle(string[] slots, string id)
        {
            if (slots == null || slots.Length < Count || !Fits(id)) return -1;
            for (int i = 0; i < Count; i++)
            {
                if (slots[i] == id)
                {
                    slots[i] = "";
                    return i;
                }
            }
            for (int i = 0; i < Count; i++)
            {
                if (string.IsNullOrEmpty(slots[i]))
                {
                    slots[i] = id;
                    return i;
                }
            }
            return -2;
        }

        public static string IdAt(string[] slots, int index)
        {
            if (slots == null || index < 0 || index >= Count || index >= slots.Length) return "";
            return slots[index] ?? "";
        }

        public static string Mark(string[] slots, string id)
        {
            if (slots == null || string.IsNullOrEmpty(id)) return "";
            for (int i = 0; i < Count && i < slots.Length; i++)
            {
                if (slots[i] == id) return (i + 5).ToString();
            }
            return "";
        }

        public static string Line(string[] slots)
        {
            string text = "";
            for (int i = 0; i < Count; i++)
            {
                if (i > 0) text += "   ";
                string id = IdAt(slots, i);
                var record = string.IsNullOrEmpty(id) ? null : ItemCatalog.Find(id);
                string name = string.IsNullOrEmpty(id) ? "-" : Loc.Item(id);
                text += (i + 5) + " " + name;
            }
            return text;
        }
    }
}
