namespace OutpostZero.Colony
{
    /// <summary>
    /// Opinion becomes a name once it is strong enough.
    /// Eighty or more is a partner. Forty is a friend. Minus forty is a rival.
    /// </summary>
    public static class BondMark
    {
        public const int PartnerAt = 80;
        public const int FriendAt = 40;
        public const int RivalAt = -40;
        public const int PartnerGrief = 12;
        public const int RivalCut = 4;

        public static string Kind(int score)
        {
            if (score >= PartnerAt) return "Partner";
            if (score >= FriendAt) return "Friend";
            if (score <= RivalAt) return "Rival";
            return "";
        }

        public static bool Partner(int score)
        {
            return score >= PartnerAt;
        }

        public static bool Rival(int score)
        {
            return score <= RivalAt;
        }
    }
}
