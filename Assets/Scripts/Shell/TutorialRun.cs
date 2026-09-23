namespace OutpostZero.Shell
{
    /// <summary>
    /// One scripted body on the tutorial street, placed relative to where the player starts.
    /// </summary>
    public struct TutorialBeat
    {
        public float Ahead;
        public float Side;
        public string Variant;

        public TutorialBeat(float ahead, float side, string variant)
        {
            Ahead = ahead;
            Side = side;
            Variant = variant;
        }
    }

    /// <summary>
    /// The first expedition while the tutorial is unfinished: a quiet street with a few walkers set
    /// where the lessons need them, no timed waves, no gunfire reinforcements and no ambush.
    /// </summary>
    public static class TutorialRun
    {
        public const int Cap = 4;
        public const int KillGoal = 2;
        public const int ScrapGoal = 3;
        public const float Tension = 0f;
        public const float Near = 12f;

        public static readonly TutorialBeat[] Beats =
        {
            new TutorialBeat(14f, -3f, "Walker"),
            new TutorialBeat(24f, 4f, "Walker"),
            new TutorialBeat(34f, 0f, "Walker")
        };

        public static bool Applies(bool lessonsFinished, bool endless) => !lessonsFinished && !endless;

        public static bool Close(float distance) => distance <= Near;

        public static int Scrap(int districtGoal) => districtGoal < ScrapGoal ? districtGoal : ScrapGoal;

        public static bool Point(int beat, float originX, float originZ, float forwardX, float forwardZ, out float x, out float z)
        {
            x = originX;
            z = originZ;
            if (beat < 0 || beat >= Beats.Length) return false;
            float length = (float)System.Math.Sqrt(forwardX * forwardX + forwardZ * forwardZ);
            if (length < 0.0001f)
            {
                forwardX = 0f;
                forwardZ = 1f;
                length = 1f;
            }
            float fx = forwardX / length;
            float fz = forwardZ / length;
            var b = Beats[beat];
            x = originX + fx * b.Ahead + fz * b.Side;
            z = originZ + fz * b.Ahead - fx * b.Side;
            return true;
        }
    }
}
