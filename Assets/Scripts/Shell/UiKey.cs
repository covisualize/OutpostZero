namespace OutpostZero.Shell
{
    /// <summary>
    /// A running FNV-1a hash over the values a panel shows. The UI rebuilds a panel only when its key moves,
    /// and hashing avoids building a signature string on every refresh.
    /// </summary>
    public struct UiKey
    {
        private const uint Seed = 2166136261;
        private const uint Prime = 16777619;
        private uint hash;
        private bool started;

        public int Value => (int)(started ? hash : Seed);

        public void Add(int value)
        {
            if (!started)
            {
                hash = Seed;
                started = true;
            }
            unchecked
            {
                hash = (hash ^ (uint)value) * Prime;
            }
        }

        public void Add(bool value) => Add(value ? 1 : 2);

        public void Add(float value) => Add((int)System.Math.Round(value * 100f));

        public void Add(string value)
        {
            if (value == null)
            {
                Add(-1);
                return;
            }
            for (int i = 0; i < value.Length; i++) Add(value[i]);
            Add(value.Length);
        }
    }
}
