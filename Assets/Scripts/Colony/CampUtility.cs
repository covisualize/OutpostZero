namespace OutpostZero.Colony
{
    /// <summary>
    /// A colonist's pick for the current camp hour. The hard rules in <see cref="CampRoutine"/> come first
    /// (a breakdown rests, the badly hurt see the medic, the starving eat, the worn out sleep); otherwise
    /// each action scores from needs, mood, the hour and who is around, and the best one holds until the hour turns.
    /// </summary>
    public static class CampUtility
    {
        public const string Eat = "Cook";
        public const string Sleep = "Rest";
        public const string Chat = "Visit";
        public const string Wander = "Wander";

        public const float NightFrom = 22f;
        public const float NightTo = 5f;
        public const float WanderReach = 5f;
        /// <summary>A guard's night post outscores bed until wear passes about 40.</summary>
        public const float NightWatch = 1.6f;
        public const float YardX = -14f;
        public const float YardZ = -10f;

        public struct Scores
        {
            public float Eat;
            public float Sleep;
            public float Work;
            public float Chat;
            public float Wander;
        }

        public static bool Night(float hour)
        {
            return hour >= NightFrom || hour < NightTo;
        }

        /// <summary>The same number for one colonist through one hour, and a new one when the hour turns.</summary>
        public static float Jitter(string id, float hour)
        {
            unchecked
            {
                uint h = 2166136261u;
                if (id != null)
                {
                    for (int i = 0; i < id.Length; i++)
                    {
                        h ^= id[i];
                        h *= 16777619u;
                    }
                }
                h ^= (uint)(int)System.Math.Floor(hour) * 0x9E3779B9u;
                h ^= h >> 16;
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;
                return (h & 0xFFFF) / 65535f;
            }
        }

        public static Scores Score(string assigned, float hunger, float thirst, float morale, float fatigue, float hour, bool friendNear, float jitter, float chatJitter)
        {
            var s = new Scores();
            float low = hunger < thirst ? hunger : thirst;
            s.Eat = low < 60f ? 0.4f + (60f - low) / 30f : 0f;

            bool night = Night(hour);
            s.Sleep = fatigue / 100f * 0.8f + (night ? 1.0f : 0f);

            bool works = !Idle(assigned);
            if (works)
            {
                float mood = ColonyDay.OutputScale(morale);
                s.Work = 0.9f * mood;
                if (night) s.Work *= assigned == "Guard" ? NightWatch : 0.2f;
            }

            if (friendNear && !OutpostZero.Player.NeedsPressure.Tired(fatigue))
                s.Chat = 0.2f + (morale < 50f ? 0.25f : 0f) + chatJitter * 0.4f;

            s.Wander = night ? 0f : 0.2f + jitter * 0.3f;
            return s;
        }

        public static string Best(Scores s, string assigned)
        {
            string best = Sleep;
            float top = s.Sleep;
            if (s.Work > top) { top = s.Work; best = assigned; }
            if (s.Eat > top) { top = s.Eat; best = Eat; }
            if (s.Chat > top) { top = s.Chat; best = Chat; }
            if (s.Wander > top) { best = Wander; }
            return best;
        }

        public static string Choose(string id, string assigned, float hunger, float thirst, float morale, int injury, float fatigue, float hour, bool friendNear)
        {
            string rule = CampRoutine.Choose(assigned, hunger, thirst, morale, injury, fatigue);
            if (Forced(rule, assigned)) return rule;
            return Best(Score(assigned, hunger, thirst, morale, fatigue, hour, friendNear, Jitter(id, hour), Jitter(id + "^", hour)), assigned);
        }

        /// <summary>The routine overrode the board: a need, a wound, a collapse, or the Clear job, which never waits.</summary>
        public static bool Forced(string rule, string assigned)
        {
            if (assigned == "Clear") return true;
            if (rule == assigned) return false;
            return !(rule == "Rest" && Idle(assigned));
        }

        private static bool Idle(string assigned)
        {
            return string.IsNullOrEmpty(assigned) || assigned == "Rest" || assigned == "Lead" || assigned == "Fallen";
        }

        /// <summary>Of two friends who both chat, the earlier id holds its spot and the other walks over.</summary>
        public static bool Hosts(string selfId, string friendId)
        {
            return string.CompareOrdinal(selfId ?? "", friendId ?? "") < 0;
        }

        /// <summary>A loose spot in the yard for a wander, held for the hour.</summary>
        public static void WanderSpot(string id, float hour, out float x, out float z)
        {
            float a = Jitter(id, hour) * 6.2831853f;
            float r = WanderReach * (0.4f + 0.6f * Jitter(id + "~", hour));
            x = YardX + (float)System.Math.Cos(a) * r;
            z = YardZ + (float)System.Math.Sin(a) * r;
        }
    }
}
