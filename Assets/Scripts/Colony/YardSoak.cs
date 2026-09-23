using OutpostZero.Graphics;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Rain and a storm soak the yard. A cook, a medic, and a rest stay under the roof.
    /// </summary>
    public static class YardSoak
    {
        public const int RainCut = 1;
        public const float RainDrag = 6f;
        public const float StormDrag = 14f;
        public const float MoodRain = 2f;
        public const float MoodStorm = 4f;

        public static bool Outdoor(string task)
        {
            return task == "Scavenge" || task == "Build" || task == "Guard" || task == "Clear";
        }

        public static bool Soaked(WeatherKind kind)
        {
            return kind == WeatherKind.Rain || kind == WeatherKind.Storm;
        }

        public static int Keep(int paid, string task, WeatherKind kind)
        {
            if (paid <= 0) return 0;
            if (!Outdoor(task) || !Soaked(kind)) return paid;
            if (kind == WeatherKind.Storm) return paid / 2;
            int next = paid - RainCut;
            return next < 1 ? 1 : next;
        }

        public static float Wear(float fatigue, string task, WeatherKind kind)
        {
            if (fatigue < 0f) fatigue = 0f;
            if (fatigue > 100f) fatigue = 100f;
            if (!Outdoor(task)) return fatigue;
            float extra = kind == WeatherKind.Storm ? StormDrag : kind == WeatherKind.Rain ? RainDrag : 0f;
            float next = fatigue + extra;
            if (next > 100f) return 100f;
            return next;
        }

        public static float Mood(string task, WeatherKind kind)
        {
            if (!Outdoor(task)) return 0f;
            if (kind == WeatherKind.Storm) return MoodStorm;
            if (kind == WeatherKind.Rain) return MoodRain;
            return 0f;
        }
    }
}
