namespace OutpostZero.AI
{
    /// <summary>
    /// After the target slips out of sight, the search walks three spots around the last
    /// place they were seen, then gives up. The schedule does not need a NavMesh.
    /// </summary>
    public static class SearchMemory
    {
        public const float Grace = 0.75f;
        public const int Points = 3;
        public const float Radius = 6f;
        public const float Span = 8f;

        public struct Sweep
        {
            public int Index;
            public float Left;
        }

        public static float Lose(float held, bool seen, float dt)
        {
            if (seen) return 0f;
            if (dt <= 0f) return held;
            float next = held + dt;
            return next > 3f ? 3f : next;
        }

        public static bool Forgotten(float held) => held >= Grace - 0.001f;

        public static Sweep Start()
        {
            return new Sweep { Index = 0, Left = Span / Points };
        }

        public static bool Done(Sweep sweep) => sweep.Index >= Points;

        public static Sweep Tick(Sweep sweep, bool arrived, float dt)
        {
            if (sweep.Index >= Points) return sweep;
            if (dt < 0f) dt = 0f;
            sweep.Left -= dt;
            if (!arrived && sweep.Left > 0f) return sweep;
            sweep.Index++;
            sweep.Left = sweep.Index >= Points ? 0f : Span / Points;
            return sweep;
        }

        public static void Offset(int index, out float x, out float z)
        {
            if (index == 1)
            {
                x = -2.2f;
                z = 3.8f;
                return;
            }
            if (index == 2)
            {
                x = -2.2f;
                z = -3.8f;
                return;
            }
            x = 4.5f;
            z = 0f;
        }
    }
}
