namespace OutpostZero.Colony
{
    /// <summary>
    /// A work shift adds wear. Rest takes it off, and a cot takes more.
    /// Past the tired line, the same shift pays one less.
    /// </summary>
    public static class ShiftWear
    {
        public const float Labor = 22f;
        public const float Lead = 8f;

        public static float After(float fatigue, string task, bool cot)
        {
            if (fatigue < 0f) fatigue = 0f;
            if (fatigue > 100f) fatigue = 100f;
            if (task == "Rest") return NightRest.Wake(fatigue, cot);
            if (task == "Lead") return Clamp(fatigue + Lead);
            if (IsLabor(task)) return Clamp(fatigue + Labor);
            return fatigue;
        }

        public static int Short(int paid, float fatigue)
        {
            if (paid <= 0) return 0;
            if (!OutpostZero.Player.NeedsPressure.Tired(fatigue)) return paid;
            int next = paid - 1;
            return next < 1 ? 1 : next;
        }

        private static bool IsLabor(string task)
        {
            return task == "Guard" || task == "Scavenge" || task == "Build"
                || task == "Cook" || task == "Medic" || task == "Clear";
        }

        private static float Clamp(float value)
        {
            if (value < 0f) return 0f;
            if (value > 100f) return 100f;
            return value;
        }
    }
}
