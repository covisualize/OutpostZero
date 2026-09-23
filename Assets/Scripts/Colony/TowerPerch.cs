namespace OutpostZero.Colony
{
    /// <summary>
    /// A guard posted on a watchtower shoots from its platform and sees further over the wall.
    /// A bitten guard keeps the tower's reach bonus on top of the shorter bitten range.
    /// </summary>
    public static class TowerPerch
    {
        public const float Reach = 1.5f;
        public const float Eye = 3.2f;
        public const float Ground = 1.6f;

        public static float Range(float range, bool perched)
        {
            if (range <= 0f) return 0f;
            return perched ? range * Reach : range;
        }

        public static float Height(bool perched)
        {
            return perched ? Eye : Ground;
        }
    }
}
