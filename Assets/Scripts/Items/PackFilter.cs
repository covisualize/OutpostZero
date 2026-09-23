using OutpostZero.Core;

namespace OutpostZero.Items
{
    /// <summary>
    /// The pack's category tabs. Index zero shows everything; the rest follow ItemCategory.
    /// </summary>
    public static class PackFilter
    {
        public const int All = 0;

        public static int Count => System.Enum.GetValues(typeof(ItemCategory)).Length + 1;

        public static int Next(int current)
        {
            int next = current + 1;
            return next >= Count || next < 0 ? All : next;
        }

        public static bool Shows(int filter, ItemCategory category)
        {
            if (filter <= All || filter >= Count) return true;
            return (int)category == filter - 1;
        }

        public static string Key(int filter)
        {
            if (filter <= All || filter >= Count) return "pack.all";
            return "pack." + ((ItemCategory)(filter - 1)).ToString().ToLowerInvariant();
        }
    }
}
