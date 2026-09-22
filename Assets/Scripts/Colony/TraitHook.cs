namespace OutpostZero.Colony
{
    /// <summary>
    /// Traits that change the night, the plate, the watch, and the shot.
    /// A watchtower is still required. Watchful and Light Sleeper only stretch a warning that already exists.
    /// A Glutton spends hunger faster. Every other trait keeps the old drop.
    /// A Cook adds four morale only when a meal was actually cooked. A Sharpshooter tightens the leader's shot to 0.8.
    /// A Brave watch costs no morale. A Cowardly watch costs six and adds no security.
    /// An Insomniac rests three less, and never below one. An Optimist works inspired above 60.
    /// A Night Owl stretches a warning that already exists by two seconds. No tower stays quiet.
    /// </summary>
    public static class TraitHook
    {
        public const float Watch = 4f;
        public const float Sleeper = 3f;
        public const float PlainHunger = 18f;
        public const float GluttonHunger = 23.4f;

        public static int CookPlate(string trait, bool cooked)
        {
            if (!cooked || trait != "Cook") return 0;
            return 4;
        }

        public static float Aim(string trait)
        {
            if (trait == "Sharpshooter") return 0.8f;
            return 1f;
        }

        public static int WatchCost(string trait)
        {
            if (trait == "Brave") return 0;
            if (trait == "Cowardly") return 6;
            return 2;
        }

        public static int WatchPay(string trait, int watch)
        {
            if (watch <= 0 || trait == "Cowardly") return 0;
            return watch;
        }

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

        public static int RestGain(string trait, int rest)
        {
            if (rest < 0) rest = 0;
            if (trait != "Insomniac") return rest;
            int cut = rest - 3;
            return cut < 1 ? 1 : cut;
        }

        public const float Owl = 2f;

        public static float NightStretch(float warning, int owls)
        {
            if (warning <= 0f) return 0f;
            if (owls < 0) owls = 0;
            float time = warning + owls * Owl;
            if (time > RaidWarn.Cap) return RaidWarn.Cap;
            return time;
        }
    }
}
