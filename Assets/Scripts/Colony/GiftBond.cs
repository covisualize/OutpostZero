namespace OutpostZero.Colony
{
    /// <summary>
    /// A shared meal lifts both sides by eight. It will not feed the same person twice in one offer,
    /// and it spends one cooked meal. One hundred is as close as the book goes.
    /// </summary>
    public static class GiftBond
    {
        public const int Lift = 8;
        public const int Cost = 1;

        public static bool Can(string fromId, string toId, bool fromAlive, bool toAlive, int food)
        {
            if (!fromAlive || !toAlive) return false;
            if (food < Cost) return false;
            if (string.IsNullOrEmpty(fromId) || string.IsNullOrEmpty(toId)) return false;
            return fromId != toId;
        }

        public static int Score(int opinion)
        {
            int next = opinion + Lift;
            if (next > KinBoard.Cap) return KinBoard.Cap;
            if (next < KinBoard.Floor) return KinBoard.Floor;
            return next;
        }

        public static string Give(string book, string otherId)
        {
            return KinBoard.Shift(book, otherId, Lift);
        }
    }
}
