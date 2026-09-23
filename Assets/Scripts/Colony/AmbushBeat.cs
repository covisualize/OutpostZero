namespace OutpostZero.Colony
{
    /// <summary>
    /// A hostile militia opens the street with extra bodies.
    /// A tight alive cap gets a smaller pack.
    /// </summary>
    public static class AmbushBeat
    {
        public const int Extra = 4;
        public const int Tight = 2;
        public const int TightCap = 16;

        public static int Bodies(bool ambush, int aliveCap)
        {
            if (!ambush) return 0;
            if (aliveCap < TightCap) return Tight;
            return Extra;
        }
    }
}
