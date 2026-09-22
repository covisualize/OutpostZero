namespace OutpostZero.AI
{
    /// <summary>
    /// The dead that came with the raid leave the yard when the night ends.
    /// A body already down stays. The street horde is not this list.
    /// </summary>
    public static class RaidRecall
    {
        public static bool Leaves(bool fromRaid, bool dead)
        {
            return fromRaid && !dead;
        }

        public static int Count(bool[] fromRaid, bool[] dead)
        {
            if (fromRaid == null || dead == null) return 0;
            int count = fromRaid.Length < dead.Length ? fromRaid.Length : dead.Length;
            int left = 0;
            for (int i = 0; i < count; i++)
            {
                if (Leaves(fromRaid[i], dead[i])) left++;
            }
            return left;
        }
    }
}
