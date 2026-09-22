namespace OutpostZero.Combat
{
    /// <summary>
    /// Sustained fire opens the group. Letting go of the trigger walks it back.
    /// </summary>
    public static class RecoilBloom
    {
        public const float HeatPerShot = 7f;
        public const float HeatMax = 100f;
        public const float CoolPerSecond = 28f;

        public static float AfterShot(float heat)
        {
            float next = heat + HeatPerShot;
            return next > HeatMax ? HeatMax : next;
        }

        public static float Cool(float heat, float dt)
        {
            if (heat <= 0f || dt <= 0f) return heat < 0f ? 0f : heat;
            float next = heat - CoolPerSecond * dt;
            return next < 0f ? 0f : next;
        }

        public static float Spread(float baseAngle, float multiplier, float heat)
        {
            if (baseAngle < 0f) baseAngle = 0f;
            if (multiplier < 0f) multiplier = 0f;
            if (heat < 0f) heat = 0f;
            return baseAngle * multiplier * (1f + heat / 80f);
        }
    }
}
