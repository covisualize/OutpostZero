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
    /// A second trait counts when it is the one that matters. The first trait still answers alone.
    /// The numbers come from each trait's <see cref="TraitTable"/> row, so a tuned trait asset changes them.
    /// </summary>
    public static class TraitHook
    {
        public const float Watch = 4f;
        public const float Sleeper = 3f;
        public const float PlainHunger = 18f;
        public const float GluttonHunger = 23.4f;
        public const int PlainWatch = 2;

        public static bool Holds(string trait, string aside, string name)
        {
            return Holds(trait, aside, null, name);
        }

        public static bool Holds(string trait, string aside, string mark, string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return trait == name || aside == name || mark == name;
        }

        public static int CookPlate(string trait, bool cooked)
        {
            return CookPlate(trait, null, cooked);
        }

        public static int CookPlate(string trait, string aside, bool cooked)
        {
            return CookPlate(trait, aside, null, cooked);
        }

        public static int CookPlate(string trait, string aside, string mark, bool cooked)
        {
            if (!cooked) return 0;
            int plate = 0;
            foreach (var row in Rows(trait, aside, mark)) if (row.CookPlate > plate) plate = row.CookPlate;
            return plate;
        }

        public static float Aim(string trait)
        {
            return Aim(trait, null);
        }

        public static float Aim(string trait, string aside)
        {
            return Aim(trait, aside, null);
        }

        public static float Aim(string trait, string aside, string mark)
        {
            float aim = 1f;
            foreach (var row in Rows(trait, aside, mark)) if (row.Aim < aim) aim = row.Aim;
            return aim;
        }

        public static int WatchCost(string trait)
        {
            return WatchCost(trait, null);
        }

        public static int WatchCost(string trait, string aside)
        {
            return WatchCost(trait, aside, null);
        }

        public static int WatchCost(string trait, string aside, string mark)
        {
            int worst = PlainWatch, best = PlainWatch;
            foreach (var row in Rows(trait, aside, mark))
            {
                if (row.WatchCost > worst) worst = row.WatchCost;
                if (row.WatchCost < best) best = row.WatchCost;
            }
            return worst > PlainWatch ? worst : best;
        }

        public static int WatchPay(string trait, int watch)
        {
            return WatchPay(trait, null, watch);
        }

        public static int WatchPay(string trait, string aside, int watch)
        {
            return WatchPay(trait, aside, null, watch);
        }

        public static int WatchPay(string trait, string aside, string mark, int watch)
        {
            if (watch <= 0) return 0;
            foreach (var row in Rows(trait, aside, mark)) if (!row.WatchPays) return 0;
            return watch;
        }

        public static float Warning(int towers, int guards, int watchful, int sleepers)
        {
            float time = RaidWarn.Seconds(towers, guards);
            if (time <= 0f) return 0f;
            if (watchful < 0) watchful = 0;
            if (sleepers < 0) sleepers = 0;
            time += watchful * Stretch("Watchful", Watch) + sleepers * Stretch("Light Sleeper", Sleeper);
            if (time > RaidWarn.Cap) return RaidWarn.Cap;
            return time;
        }

        public static float HungerDrop(string trait)
        {
            return HungerDrop(trait, null);
        }

        public static float HungerDrop(string trait, string aside)
        {
            return HungerDrop(trait, aside, null);
        }

        public static float HungerDrop(string trait, string aside, string mark)
        {
            float drop = PlainHunger;
            foreach (var row in Rows(trait, aside, mark)) if (row.Hunger > drop) drop = row.Hunger;
            return drop;
        }

        public static int RestGain(string trait, int rest)
        {
            return RestGain(trait, null, rest);
        }

        public static int RestGain(string trait, string aside, int rest)
        {
            return RestGain(trait, aside, null, rest);
        }

        public static int RestGain(string trait, string aside, string mark, int rest)
        {
            if (rest < 0) rest = 0;
            int cut = 0;
            foreach (var row in Rows(trait, aside, mark)) if (row.RestCut > cut) cut = row.RestCut;
            if (cut <= 0) return rest;
            int left = rest - cut;
            return left < 1 ? 1 : left;
        }

        public const float Owl = 2f;

        public static float NightStretch(float warning, int owls)
        {
            if (warning <= 0f) return 0f;
            if (owls < 0) owls = 0;
            float time = warning + owls * Stretch("Night Owl", Owl);
            if (time > RaidWarn.Cap) return RaidWarn.Cap;
            return time;
        }
    
        /// <summary>Extra scrap a scavenging shift brings for its best trait.</summary>
        public static int Haul(string trait, string aside, string mark)
        {
            int haul = 0;
            foreach (var row in Rows(trait, aside, mark)) if (row.Haul > haul) haul = row.Haul;
            return haul;
        }

        private static float Stretch(string trait, float fallback)
        {
            var row = TraitTable.For(trait);
            return row != null ? row.Warn : fallback;
        }

        private static System.Collections.Generic.IEnumerable<TraitTable.Row> Rows(string trait, string aside, string mark)
        {
            var first = TraitTable.For(trait);
            if (first != null) yield return first;
            if (aside != trait)
            {
                var second = TraitTable.For(aside);
                if (second != null) yield return second;
            }
            if (mark != trait && mark != aside)
            {
                var third = TraitTable.For(mark);
                if (third != null) yield return third;
            }
        }
    }
}
