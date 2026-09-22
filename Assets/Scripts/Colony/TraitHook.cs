namespace OutpostZero.Colony
{
    /// <summary>
    /// Traits that change the night and the plate.
    /// A watchtower is still required. Watchful and Light Sleeper only stretch a warning that already exists.
    /// A Glutton spends hunger faster. Every other trait keeps the old drop.
    /// </summary>
    public static class TraitHook
    {
        public const float Watch = 4f;
        public const float Sleeper = 3f;
        public const float PlainHunger = 18f;
        public const float GluttonHunger = 23.4f;

        public static float Warning(int towers, int guards, int watchful, int sleepers)
        {
            float time = RaidWarn.Seconds(towers, guards);
            if (time <= 0f) return 0f;
            if (watchful < 0) watchful = 0;
            if (sleepers < 0) sleepers = 0;
            time += watchful * Watch + sleepers * Sleeper;
            if (time > RaidWarn.Cap) return RaidWarn.Cap;
            return time;
        }

        public static float HungerDrop(string trait)
        {
            if (trait == "Glutton") return GluttonHunger;
            return PlainHunger;
        }
    }
}
