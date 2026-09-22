namespace OutpostZero.Colony
{
    /// <summary>
    /// Pulling a module up returns half its scrap, rounded down. Facing turns by a quarter turn.
    /// </summary>
    public static class ScrapRefund
    {
        public static int Half(int cost)
        {
            if (cost < 0) cost = 0;
            return cost / 2;
        }

        public static int Turn(int rotation)
        {
            if (rotation < 0) rotation = 0;
            int next = rotation + 90;
            if (next >= 360) next -= 360;
            return next;
        }
    }
}
