namespace OutpostZero.Colony
{
    /// <summary>
    /// What a colonist actually does in the yard. Hunger, thirst, injury, a collapse,
    /// and wear past the tired line pull them off the board assignment.
    /// The nudge keeps two people off the same tile.
    /// </summary>
    public static class CampRoutine
    {
        public static string Choose(string assigned, float hunger, float thirst, float morale, int injury)
        {
            return Choose(assigned, hunger, thirst, morale, injury, 0f);
        }

        public static string Choose(string assigned, float hunger, float thirst, float morale, int injury, float fatigue)
        {
            if (morale < 10f) return "Rest";
            if (injury >= 2 || assigned == "Quarantine") return "Medic";
            if (hunger < 45f || thirst < 40f) return "Cook";
            if (OutpostZero.Player.NeedsPressure.Tired(fatigue) && assigned != "Rest") return "Rest";
            if (assigned == "Clear") return "Clear";
            if (morale < 30f && assigned == "Scavenge") return "Rest";
            if (string.IsNullOrEmpty(assigned) || assigned == "Lead" || assigned == "Fallen") return "Rest";
            return assigned;
        }

        public static string Bark(string action, float morale)
        {
            return Bark(action, morale, 0f);
        }

        public static string Bark(string action, float morale, float fatigue)
        {
            if (morale < 10f) return "I can't do this.";
            if (action == "Visit") return "Good to see you.";
            if (action == "Rest" && OutpostZero.Player.NeedsPressure.Tired(fatigue)) return "My legs are done.";
            if (action == "Cook") return "Fire's lit.";
            if (action == "Guard") return "Watching the gate.";
            if (action == "Medic") return "Hold still.";
            if (action == "Scavenge") return "I'll check the piles.";
            if (action == "Build") return "I'll raise it.";
            if (action == "Clear") return "I'll haul them.";
            if (morale > 70f) return "We'll hold.";
            return "Resting.";
        }

        public static void Nudge(int index, out float x, out float z)
        {
            int slot = index < 0 ? 0 : index % 6;
            x = (slot - 2) * 0.55f;
            z = (slot % 2) * 0.4f;
        }
    }
}
