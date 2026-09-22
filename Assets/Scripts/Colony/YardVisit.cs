namespace OutpostZero.Colony
{
    /// <summary>
    /// Two close friends who are both resting share a spot.
    /// The later name walks over. Wear past the tired line keeps them on the cot.
    /// </summary>
    public static class YardVisit
    {
        public const float Beside = 0.8f;

        public static string Host(string selfId, string action, string kin, float fatigue, string[] ids, string[] actions, bool[] present)
        {
            if (action != "Rest") return "";
            if (OutpostZero.Player.NeedsPressure.Tired(fatigue)) return "";
            if (string.IsNullOrEmpty(selfId) || ids == null) return "";
            string host = "";
            int count = ids.Length;
            for (int i = 0; i < count; i++)
            {
                string other = ids[i];
                if (string.IsNullOrEmpty(other) || other == selfId) continue;
                if (present != null && (i >= present.Length || !present[i])) continue;
                if (actions == null || i >= actions.Length || actions[i] != "Rest") continue;
                if (string.CompareOrdinal(selfId, other) <= 0) continue;
                if (!KinBoard.Close(kin, other)) continue;
                if (host.Length == 0 || string.CompareOrdinal(other, host) < 0) host = other;
            }
            return host;
        }

        public static void Stand(float hostX, float hostZ, out float x, out float z)
        {
            x = hostX + Beside;
            z = hostZ;
        }
    }
}
