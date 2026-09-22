namespace OutpostZero.Player
{
    /// <summary>
    /// Hunger slows recovery, thirst shrinks the stamina pool, and exhaustion slows the aim.
    /// The opening buffer of hunger and thirst lasts about ten minutes.
    /// </summary>
    public static class NeedsPressure
    {
        public const float HungryAt = 25f;
        public const float DryAt = 25f;
        public const float TiredAt = 75f;
        public const float HungryRegen = 0.85f;
        public const float DryStamina = 0.8f;
        public const float TiredAim = 0.62f;
        public const float HungerPerSecond = 55f / 600f;
        public const float ThirstPerSecond = 50f / 600f;
        public const float FatiguePerSecond = 55f / 600f;

        public static bool Hungry(float hunger) => hunger < HungryAt;

        public static bool Dry(float thirst) => thirst < DryAt;

        public static bool Tired(float fatigue) => fatigue > TiredAt;

        public static float Regen(float hunger) => Hungry(hunger) ? HungryRegen : 1f;

        public static float StaminaCap(float thirst, float max)
        {
            if (max < 1f) max = 1f;
            return Dry(thirst) ? max * DryStamina : max;
        }

        public static float Aim(float fatigue) => Tired(fatigue) ? TiredAim : 1f;
    }
}
