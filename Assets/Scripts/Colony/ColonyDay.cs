using System;
using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// One colonist's vitals for a camp day. The roster copies these onto <see cref="Survivor"/>.
    /// </summary>
    public class ColonistDay
    {
        public string id;
        public string trait;
        public string aside = "";
        public string mark = "";
        public string task;
        public string bond;
        public string kin = "";
        public string name = "";
        public bool alive = true;
        public bool leader;
        public float morale = 70f;
        public float hunger = 78f;
        public float thirst = 78f;
        public int opinion = 18;
        public int injury;
        public int leadership;
    }

    /// <summary>
    /// Daily camp rules: stores refill hunger and thirst, mood changes output,
    /// and six events (argument, friendship, breakdown, recovery, grief, celebration)
    /// fall out of those numbers.
    /// </summary>
    public static class ColonyDay
    {
        public static string Mood(float morale)
        {
            return Mood(morale, null);
        }

        public static string Mood(float morale, string trait, string aside)
        {
            return Mood(morale, trait, aside, null);
        }

        public static string Mood(float morale, string trait, string aside, string mark)
        {
            if (mark == "Optimist" || aside == "Optimist") return Mood(morale, "Optimist");
            return Mood(morale, trait);
        }

        public static string Mood(float morale, string trait)
        {
            if (trait == "Optimist" && morale > 60f && morale <= 70f) return "Inspired";
            if (morale > 70f) return "Inspired";
            if (morale < 10f) return "Breakdown";
            if (morale < 30f) return "Depressed";
            return "Steady";
        }

        public static float OutputScale(float morale)
        {
            return OutputScale(morale, null);
        }

        public static float OutputScale(float morale, string trait, string aside)
        {
            return OutputScale(morale, trait, aside, null);
        }

        public static float OutputScale(float morale, string trait, string aside, string mark)
        {
            if (mark == "Optimist" || aside == "Optimist") return OutputScale(morale, "Optimist");
            return OutputScale(morale, trait);
        }

        public static float OutputScale(float morale, string trait)
        {
            if (trait == "Optimist" && morale > 60f && morale >= 10f) return 1.1f;
            if (morale > 70f) return 1.1f;
            if (morale < 10f) return 0f;
            if (morale < 30f) return 0.7f;
            return 1f;
        }

        public static string[] Simulate(IList<ColonistDay> people, ref int food, ref int water, bool cot, bool expeditionWon, string fallenName)
        {
            return Simulate(people, ref food, ref water, cot, expeditionWon, fallenName, 0);
        }

        public static string[] Simulate(IList<ColonistDay> people, ref int food, ref int water, bool cot, bool expeditionWon, string fallenName, int bodies)
        {
            int raw = 0;
            return Simulate(people, ref food, ref water, cot, expeditionWon, fallenName, bodies, ref raw);
        }

        public static string[] Simulate(IList<ColonistDay> people, ref int food, ref int water, bool cot, bool expeditionWon, string fallenName, int bodies, ref int raw)
        {
            var events = new List<string>();
            if (people == null) return Array.Empty<string>();
            int stain = YardDead.MoodHit(bodies);

            bool anyCook = false;
            bool medic = false;
            bool volatilePresent = false;
            int living = 0;
            bool leaderPresent = false;
            int leadSkill = 0;
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || !person.alive) continue;
                living++;
                if (person.leader)
                {
                    leaderPresent = true;
                    leadSkill = person.leadership;
                }
                if (person.task == "Cook") anyCook = true;
                if (person.task == "Medic") medic = true;
                if (TraitHook.Holds(person.trait, person.aside, person.mark, "Volatile")) volatilePresent = true;
            }

            string fallenFirst = FirstName(fallenName);
            string fallenId = KinBoard.FallenId(people, fallenName);
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || !person.alive) continue;

                person.morale -= stain;
                float hungerBefore = person.hunger;
                person.hunger = Clamp(person.hunger - TraitHook.HungerDrop(person.trait, person.aside, person.mark));
                person.thirst = Clamp(person.thirst - 22f);

                int beforeFood = food;
                MealTable.Serve(ref food, ref raw, ref person.hunger, ref person.morale);
                if (beforeFood > food && anyCook) person.morale += 4f;
                if (water > 0 && person.thirst < 92f)
                {
                    water--;
                    person.thirst = Clamp(person.thirst + 40f);
                }

                if (person.hunger < 25f) person.morale -= 12f;
                if (person.thirst < 25f) person.morale -= 12f;
                if (person.hunger >= 70f && person.thirst >= 70f) person.morale += 4f;

                if (person.task == "Rest" && !person.leader)
                {
                    person.morale += MealTable.RestMood(cot, person.injury <= 0);
                }

                if (expeditionWon) person.morale += 10f;

                if (!string.IsNullOrEmpty(fallenFirst))
                {
                    if (KinBoard.Grieves(person.bond, person.kin, fallenName, fallenId))
                    {
                        person.morale -= 40f;
                        if (BondMark.Partner(KinBoard.Read(person.kin, fallenId))) person.morale -= BondMark.PartnerGrief;
                        Once(events, "grief");
                    }
                    else person.morale -= 25f;
                }

                int opinionBefore = person.opinion;
                int bitter = WorstCoworker(people, person);
                if (SharesWork(people, person) && !TraitHook.Holds(person.trait, person.aside, person.mark, "Loner"))
                {
                    person.opinion += 2;
                    person.kin = KinBoard.Warm(person.kin, people, person.id, person.task, false);
                }
                if (BondMark.Rival(bitter)) person.opinion -= BondMark.RivalCut;
                if (TraitHook.Holds(person.trait, person.aside, person.mark, "Volatile"))
                {
                    person.opinion -= MealTable.FeudShift(6, leaderPresent, leadSkill);
                    person.kin = KinBoard.ChillToward(person.kin, people, person.id, leaderPresent, leadSkill);
                }
                if (opinionBefore < 40 && person.opinion >= 40) Once(events, "friendship");

                if (person.injury > 0 && (cot || medic))
                {
                    person.injury--;
                    if (person.injury == 0) Once(events, "recovery");
                }
                else if (person.injury >= 3)
                {
                    if (person.task == "Quarantine")
                    {
                        person.alive = false;
                        person.task = "Fallen";
                        continue;
                    }
                    person.task = "Quarantine";
                }

                person.morale = Clamp(person.morale);
                if (hungerBefore < 30f && person.hunger >= 55f && person.morale >= 30f) Once(events, "recovery");
            }

            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || !person.alive) continue;
                if (person.morale < 10f)
                {
                    Once(events, "breakdown");
                    int roll = Stable(person.id) % 3;
                    if (roll == 0)
                    {
                        person.alive = false;
                        person.task = "Left";
                    }
                    else if (roll == 1)
                    {
                        var other = OtherLiving(people, person);
                        if (other != null) other.morale = Clamp(other.morale - 8f);
                    }
                    else person.task = "Rest";
                }
                else if (person.morale < 30f && person.task != "Rest" && person.task != "Lead" && person.task != "Fallen" && person.task != "Quarantine")
                {
                    person.task = "Rest";
                }
            }

            if (MealTable.Argument(volatilePresent, living, leaderPresent) || KinBoard.Quarrel(people, leaderPresent)) Once(events, "argument");
            if (expeditionWon && Average(people) > 70f) Once(events, "celebration");
            return events.ToArray();
        }

        public static string[] RewardReturn(IList<ColonistDay> people)
        {
            if (people == null) return Array.Empty<string>();
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || !person.alive) continue;
                person.morale = Clamp(person.morale + 10f);
            }
            if (Average(people) > 70f) return new[] { "celebration" };
            return Array.Empty<string>();
        }

        private static int WorstCoworker(IList<ColonistDay> people, ColonistDay self)
        {
            if (self == null || people == null) return 0;
            int worst = 0;
            bool any = false;
            for (int i = 0; i < people.Count; i++)
            {
                var other = people[i];
                if (other == null || !other.alive || other.id == self.id) continue;
                if (other.task != self.task || string.IsNullOrEmpty(other.id)) continue;
                int score = KinBoard.Read(self.kin, other.id);
                if (!any || score < worst)
                {
                    worst = score;
                    any = true;
                }
            }
            return any ? worst : 0;
        }

        private static bool SharesWork(IList<ColonistDay> people, ColonistDay self)
        {
            if (self.task == "Rest" || self.task == "Lead" || self.task == "Fallen" || self.task == "Left" || self.task == "Quarantine") return false;
            int count = 0;
            for (int i = 0; i < people.Count; i++)
            {
                var other = people[i];
                if (other == null || !other.alive) continue;
                if (other.task == self.task) count++;
            }
            return count >= 2;
        }

        private static ColonistDay OtherLiving(IList<ColonistDay> people, ColonistDay self)
        {
            for (int i = 0; i < people.Count; i++)
            {
                var other = people[i];
                if (other == null || other == self || !other.alive) continue;
                return other;
            }
            return null;
        }

        private static float Average(IList<ColonistDay> people)
        {
            float sum = 0f;
            int count = 0;
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || !person.alive) continue;
                sum += person.morale;
                count++;
            }
            return count == 0 ? 0f : sum / count;
        }

        private static void Once(List<string> events, string name)
        {
            if (!events.Contains(name)) events.Add(name);
        }

        private static float Clamp(float value)
        {
            if (value < 0f) return 0f;
            if (value > 100f) return 100f;
            return value;
        }

        private static string FirstName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int space = name.IndexOf(' ');
            return space < 0 ? name : name.Substring(0, space);
        }

        private static int Stable(string id)
        {
            int hash = 0;
            if (string.IsNullOrEmpty(id)) return 0;
            for (int i = 0; i < id.Length; i++) hash = hash * 31 + id[i];
            if (hash < 0) hash = -hash;
            return hash;
        }
    }
}
