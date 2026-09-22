namespace OutpostZero.Colony
{
    /// <summary>
    /// The dead chew the boards on their side before they reach the camp.
    /// One posted biter replaces the abstract strike so a wall is not hit twice.
    /// </summary>
    public static class BoardBite
    {
        public const float Reach = 1.8f;
        public const int Chip = 2;
        public const float Gap = 1.6f;

        public static bool CrowdChews(int posted)
        {
            return posted > 0;
        }

        public static int Assign(int slot, int boards)
        {
            if (boards <= 0) return -1;
            if (slot < 0) slot = 0;
            return slot % boards;
        }

        public static void Rank(int[] integrity, int[] order)
        {
            if (integrity == null || order == null) return;
            int count = integrity.Length < order.Length ? integrity.Length : order.Length;
            for (int i = 0; i < count; i++) order[i] = i;
            for (int i = 1; i < count; i++)
            {
                int key = order[i];
                int j = i - 1;
                while (j >= 0 && integrity[order[j]] > integrity[key])
                {
                    order[j + 1] = order[j];
                    j--;
                }
                order[j + 1] = key;
            }
        }

        public static bool InReach(float zx, float zz, float bx, float bz)
        {
            float dx = zx - bx;
            float dz = zz - bz;
            return dx * dx + dz * dz <= Reach * Reach;
        }
    }
}
