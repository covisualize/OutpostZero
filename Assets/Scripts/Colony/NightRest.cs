namespace OutpostZero.Colony
{
    /// <summary>
    /// A night that is actually slept takes the exhaustion off.
    /// A finished cot takes more. A raid that starts instead leaves the body as it was.
    /// </summary>
    public static class NightRest
    {
        public const float Bare = 40f;
        public const float Cot = 70f;

        public static float Amount(bool cot)
        {
            return cot ? Cot : Bare;
        }

        public static float Wake(float fatigue, bool cot)
        {
            if (fatigue < 0f) fatigue = 0f;
            if (fatigue > 100f) fatigue = 100f;
            float next = fatigue - Amount(cot);
            return next < 0f ? 0f : next;
        }
    }
}
