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
        public string task;
        public string bond;
        public bool alive = true;
        public bool leader;
        public float morale = 70f;
        public float hunger = 78f;
        public float thirst = 78f;
        public int opinion = 18;
        public int injury;
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
            if (morale > 70f) return "Inspired";
            if (morale < 10f) return "Breakdown";
            if (morale < 30f) return "Depressed";
            return "Steady";
        }

        public static float OutputScale(float morale)
        {
            if (morale > 70f) return 1.1f;
            if (morale < 10f) return 0f;
            if (morale < 30f) return 0.7f;
            return 1f;
        }

        public static string[] Simulate(IList<ColonistDay> people, ref int food, ref int water, bool cot, bool expeditionWon, string fallenName)
        {
            var events = new List<string>();
            if (people == null) return Array.Empty<string>();

            bool anyCook = false;
            bool medic = false;
            bool volatilePresent = false;
            int living = 0;
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || !person.alive) continue;
                living++;
                if (person.task == "Cook") anyCook = true;
                if (person.task == "Medic") medic = true;
                if (person.trait == "Volatile") volatilePresent = true;
            }

            string fallenFirst = FirstName(fallenName);
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || !person.alive) continue;

                float hungerBefore = person.hunger;
                person.hunger = Clamp(person.hunger - 18f);
                person.thirst = Clamp(person.thirst - 22f);

                if (food > 0 && person.hunger < 92f)
                {
                    food--;
                    person.hunger = Clamp(person.hunger + 48f);
                    if (anyCook) person.morale += 4f;
                }
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
                    person.morale += cot ? 6f : -5f;
                }

                if (expeditionWon) person.morale += 10f;

                if (!string.IsNullOrEmpty(fallenFirst))
                {
                    if (!string.IsNullOrEmpty(person.bond) && person.bond.IndexOf(fallenFirst, StringComparison.Ordinal) >= 0)
                    {
                        person.morale -= 40f;
                        Once(events, "grief");
                    }
                    else person.morale -= 25f;
                }

                int opinionBefore = person.opinion;
                if (SharesWork(people, person)) person.opinion += 2;
                if (person.trait == "Volatile") person.opinion -= 6;
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

            if (living >= 2 && volatilePresent) Once(events, "argument");
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
