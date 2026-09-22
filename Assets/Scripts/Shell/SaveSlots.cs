namespace OutpostZero.Shell
{
    /// <summary>
    /// Five manual slots, one autosave, and the old single-file name.
    /// Newest is the later day, then the later hour, then the autosave when those tie.
    /// </summary>
    public static class SaveSlots
    {
        public const int ManualCount = 5;
        public const int AutoSlot = 5;
        public const int LegacySlot = -1;
        public const string LegacyFile = "outpost-zero-save.json";

        public struct Card
        {
            public int Slot;
            public int Day;
            public float Hour;
            public string Leader;
            public bool Occupied;
            public bool Auto;
        }

        public static int Manual(int slot)
        {
            if (slot < 0) return 0;
            if (slot >= ManualCount) return ManualCount - 1;
            return slot;
        }

        public static string FileName(int slot)
        {
            if (slot < 0) return LegacyFile;
            if (slot == AutoSlot) return "slot_auto.json";
            return "slot_" + Manual(slot) + ".json";
        }

        public static int Newest(Card[] cards)
        {
            int best = -1;
            if (cards == null) return best;
            for (int i = 0; i < cards.Length; i++)
            {
                if (!cards[i].Occupied) continue;
                if (best < 0 || Later(cards[i], cards[best])) best = i;
            }
            return best;
        }

        public static string Hash(string text)
        {
            uint hash = 2166136261;
            if (text != null)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    hash ^= text[i];
                    hash *= 16777619;
                }
            }
            return hash.ToString("x8");
        }

        private static bool Later(Card card, Card other)
        {
            if (card.Day != other.Day) return card.Day > other.Day;
            if (card.Hour != other.Hour) return card.Hour > other.Hour;
            return card.Slot > other.Slot;
        }
    }
}
