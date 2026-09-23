using System;
using System.Collections.Generic;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Stage 3 of the fever in camp is a choice. Quarantine keeps the survivor in bed and dies by the next
    /// evening unless a cot or a medic brings them back; mercy ends it now, and the camp grieves a death it chose
    /// less than one that took them.
    /// </summary>
    public static class FeverChoice
    {
        public const int FinalStage = 3;
        public const float Ease = 10f;
        public const string Cause = "mercy";

        public static bool Final(int injury) => injury >= FinalStage;

        public static bool Offered(bool alive, bool leader, int injury)
        {
            return alive && !leader && Final(injury);
        }

        public static float Loss(bool friend, bool memorial)
        {
            return Math.Max(0f, SuccessionLedger.Loss(friend, memorial) - Ease);
        }

        public static void Mourn(IList<ColonistDay> people, string fallenName, bool memorial)
        {
            if (people == null) return;
            string fallenId = KinBoard.FallenId(people, fallenName);
            for (int i = 0; i < people.Count; i++)
            {
                var person = people[i];
                if (person == null || !person.alive) continue;
                bool friend = KinBoard.Grieves(person.bond, person.kin, fallenName, fallenId);
                person.morale = Math.Max(0f, person.morale - Loss(friend, memorial));
            }
        }
    }
}
