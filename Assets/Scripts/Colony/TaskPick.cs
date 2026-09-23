namespace OutpostZero.Colony
{
    /// <summary>
    /// What a survivor left to their own call takes up each morning. Each task scores the survivor's skill,
    /// the traits that lean toward it, and how badly the camp needs it; the wounded, the worn out and the
    /// miserable rest. Ties go to the earlier task in <see cref="Tasks"/>.
    /// </summary>
    public static class TaskPick
    {
        public const string Auto = "Auto";
        public static readonly string[] Tasks = { "Guard", "Cook", "Medic", "Build", "Craft", "Scavenge", "Rest" };

        public struct Camp
        {
            public int FoodPerHead;
            public int Raw;
            public int Injured;
            public bool RaidLikely;
            public bool WorkWaiting;
            public int Scrap;
            public int Orders;
            public bool Bench;
        }

        public static string Choose(Survivor survivor, Camp camp)
        {
            if (survivor == null || !survivor.alive) return "Rest";
            if (survivor.injury > 0 || survivor.morale < 30f || OutpostZero.Player.NeedsPressure.Tired(survivor.fatigue)) return "Rest";
            string best = "Rest";
            int bestScore = Score(survivor, camp, "Rest");
            for (int i = 0; i < Tasks.Length; i++)
            {
                int score = Score(survivor, camp, Tasks[i]);
                if (score <= bestScore) continue;
                best = Tasks[i];
                bestScore = score;
            }
            return best;
        }

        public static int Score(Survivor s, Camp camp, string task)
        {
            switch (task)
            {
                case "Guard":
                    return s.combat + (camp.RaidLikely ? 4 : 0) + Lean(s, "Brave", 2) + Lean(s, "Watchful", 2) + Lean(s, "Sharpshooter", 2)
                        + Lean(s, "Night Owl", 1) - Lean(s, "Cowardly", 6);
                case "Cook":
                    if (camp.Raw <= 0) return -10;
                    return s.cooking + (camp.FoodPerHead < 2 ? 4 : 0) + Lean(s, "Cook", 3) + Lean(s, "Glutton", 1);
                case "Medic":
                    if (camp.Injured <= 0) return -10;
                    return s.medicine + 3 + camp.Injured + Lean(s, "Field Medic", 3);
                case "Build":
                    if (!camp.WorkWaiting) return -10;
                    return s.engineering + 2 + Lean(s, "Engineer", 3) + Lean(s, "Steady Hands", 1);
                case "Craft":
                    if (camp.Orders <= 0 || !camp.Bench) return -10;
                    return s.engineering + 2 + camp.Orders / 2 + Lean(s, "Engineer", 2) + Lean(s, "Steady Hands", 2);
                case "Scavenge":
                    return s.scavenge + (camp.Scrap < 20 ? 2 : 0) + Lean(s, "Scrounger", 3) + Lean(s, "Loner", 1) - Lean(s, "Cowardly", 1);
                case "Rest":
                    return 1 + Lean(s, "Insomniac", -1) + (s.morale < 45f ? 3 : 0);
            }
            return -10;
        }

        private static int Lean(Survivor s, string trait, int weight)
        {
            return TraitHook.Holds(s.trait, s.aside, s.mark, trait) ? weight : 0;
        }
    }
}
