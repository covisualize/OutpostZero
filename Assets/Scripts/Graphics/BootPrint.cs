namespace OutpostZero.Graphics
{
    /// <summary>
    /// Walking through blood loads the boots. The next six steps print it, then the soles dry.
    /// Gore off clears the load. A step just outside the smear does not reload them.
    /// </summary>
    public static class BootPrint
    {
        public const int Steps = 6;
        public const float Smear = 1.4f;
        public const float Offset = 0.12f;

        public static int Charge(int steps, bool through, int gore)
        {
            if (gore <= 0) return 0;
            if (through) return Steps;
            if (steps < 0) return 0;
            return steps;
        }

        public static bool Due(int steps, int gore)
        {
            return gore > 0 && steps > 0;
        }

        public static int Spend(int steps)
        {
            if (steps <= 0) return 0;
            return steps - 1;
        }

        public static float Side(int steps)
        {
            return (steps & 1) == 0 ? -Offset : Offset;
        }

        public static float Size(int gore)
        {
            if (gore >= 2) return 0.22f;
            return 0.16f;
        }

        public static bool Near(float x, float z, float stainX, float stainZ)
        {
            float dx = x - stainX;
            float dz = z - stainZ;
            float reach = Smear * Smear;
            return dx * dx + dz * dz <= reach;
        }

        public static bool Through(float x, float z, float[] stainX, float[] stainZ, int count)
        {
            if (stainX == null || stainZ == null || count <= 0) return false;
            int n = count;
            if (n > stainX.Length) n = stainX.Length;
            if (n > stainZ.Length) n = stainZ.Length;
            for (int i = 0; i < n; i++)
            {
                if (Near(x, z, stainX[i], stainZ[i])) return true;
            }
            return false;
        }
    }
}
