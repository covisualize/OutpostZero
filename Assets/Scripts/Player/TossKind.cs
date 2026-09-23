namespace OutpostZero.Player
{
    /// <summary>
    /// Which held item leaves the hand, read from the throwable rows. A cell, a medkit, and a meal stay in the pack.
    /// </summary>
    public static class TossKind
    {
        public const int None = 0;
        public const int Lure = 1;
        public const int Fire = 2;
        public const int Flare = 3;
        public const int Bomb = 4;

        public static int Of(string id)
        {
            var row = ThrowableTable.Of(id);
            return row != null ? row.Kind : None;
        }

        public static bool Throws(string id)
        {
            return Of(id) != None;
        }
    }
}
