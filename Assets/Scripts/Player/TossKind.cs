namespace OutpostZero.Player
{
    /// <summary>
    /// Which held item leaves the hand. A cell, a medkit, and a meal stay in the pack.
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
            if (id == "noise_lure") return Lure;
            if (id == "molotov") return Fire;
            if (id == "flare") return Flare;
            if (id == "pipe_bomb") return Bomb;
            return None;
        }

        public static bool Throws(string id)
        {
            return Of(id) != None;
        }
    }
}
