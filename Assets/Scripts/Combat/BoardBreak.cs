namespace OutpostZero.Combat
{
    /// <summary>
    /// A wooden barricade fails under one brute charge. Concrete is not this number.
    /// </summary>
    public static class BoardBreak
    {
        public const float Wood = 40f;
        public const float ChargeHit = 45f;

        public static float Apply(float integrity, float hit)
        {
            if (integrity <= 0f) return 0f;
            if (hit < 0f) hit = 0f;
            float left = integrity - hit;
            return left < 0f ? 0f : left;
        }

        public static bool GivesWay(float integrity) => integrity <= 0f;
    }
}
