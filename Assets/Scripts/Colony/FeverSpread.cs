using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// An injury of two or more spreads once, to one other living colonist.
    /// Quarantine and the medic cot do not pass it on. Three is the ceiling.
    /// </summary>
    public static class FeverSpread
    {
        public const int Sick = 2;
        public const int Cap = 3;

        public static bool Source(int injury, string task, bool alive)
        {
            if (!alive || injury < Sick) return false;
            if (task == "Quarantine" || task == "Medic" || task == "Fallen" || task == "Left") return false;
            return true;
        }

        public static bool Catches(int injury, string task, bool alive, string id, string sourceId)
        {
            if (!alive || injury >= Cap) return false;
            if (string.IsNullOrEmpty(id) || id == sourceId) return false;
            if (task == "Quarantine" || task == "Fallen" || task == "Left") return false;
            return true;
        }

        public static int Apply(int injury)
        {
            if (injury < 0) injury = 0;
            int next = injury + 1;
            return next > Cap ? Cap : next;
        }

        public static bool Try(IList<ColonistDay> people)
        {
            if (people == null) return false;
            ColonistDay source = null;
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person != null && Source(person.injury, person.task, person.alive))
                {
                    source = person;
                    break;
                }
            }
            if (source == null) return false;
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || !Catches(person.injury, person.task, person.alive, person.id, source.id)) continue;
                person.injury = Apply(person.injury);
                return true;
            }
            return false;
        }
    }
}
