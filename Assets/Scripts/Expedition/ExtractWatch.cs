namespace OutpostZero.Expedition
{
    /// <summary>
    /// Extraction is a three-second hold. A zombie within 20 m drops the hold.
    /// </summary>
    public static class ExtractWatch
    {
        public const float HoldSeconds = 3f;
        public const float ThreatRadius = 20f;

        public static float Advance(float held, float dt, bool inside, bool threatened)
        {
            if (!inside || threatened) return 0f;
            float next = held + dt;
            return next > HoldSeconds ? HoldSeconds : next;
        }

        public static bool Ready(float held)
        {
            return held >= HoldSeconds - 0.001f;
        }

        public static bool Threatened(float playerX, float playerZ, float[] zombieX, float[] zombieZ)
        {
            if (zombieX == null || zombieZ == null) return false;
            int count = zombieX.Length < zombieZ.Length ? zombieX.Length : zombieZ.Length;
            float reach = ThreatRadius * ThreatRadius;
            for (int i = 0; i < count; i++)
            {
                float dx = zombieX[i] - playerX;
                float dz = zombieZ[i] - playerZ;
                if (dx * dx + dz * dz <= reach) return true;
            }
            return false;
        }
    }
}
