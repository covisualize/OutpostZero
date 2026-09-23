namespace OutpostZero.AI
{
    /// <summary>
    /// Each dead thing has a voice. A walker groans, a runner shrieks, a brute roars and stomps.
    /// Idle voices stay inside 22 m and only six of them speak at once.
    /// </summary>
    public static class ZombieVoice
    {
        public const int Cap = 6;
        public const float Hear = 22f;
        public const float Gap = 4.8f;
        public const float Hold = 0.7f;

        public static string Breed(string id, int ability, string vocals)
        {
            if (vocals == "walker" || vocals == "runner" || vocals == "brute") return vocals;
            return Breed(id, ability);
        }

        public static string Breed(string id, int ability)
        {
            if (ability == 2) return "brute";
            if (ability == 1) return "runner";
            if (string.IsNullOrEmpty(id)) return "walker";
            string name = id.ToLowerInvariant();
            if (name.IndexOf("brute") >= 0) return "brute";
            if (name.IndexOf("runner") >= 0) return "runner";
            return "walker";
        }

        public static string Idle(string breed)
        {
            if (breed == "runner") return "shriek";
            if (breed == "brute") return "roar";
            return "groan";
        }

        public static string Bite(string breed)
        {
            if (breed == "brute") return "stomp";
            return "snarl";
        }

        public static string Hurt(string breed)
        {
            if (breed == "brute") return "roar";
            if (breed == "runner") return "shriek";
            return "grunt";
        }

        public static string Death(string breed)
        {
            if (breed == "brute") return "roar";
            if (breed == "runner") return "shriek";
            return "groan";
        }

        public static bool IdleDue(float distance, int live, float now, float last)
        {
            if (distance > Hear) return false;
            if (live >= Cap) return false;
            if (last > 0f && now - last < Gap) return false;
            return true;
        }

        public static int Live(float now, float[] seats)
        {
            if (seats == null) return 0;
            int count = 0;
            int limit = seats.Length < Cap ? seats.Length : Cap;
            for (int i = 0; i < limit; i++)
            {
                if (seats[i] > now) count++;
            }
            return count;
        }

        public static void Seat(float[] seats, float now)
        {
            if (seats == null || seats.Length == 0) return;
            int limit = seats.Length < Cap ? seats.Length : Cap;
            for (int i = 0; i < limit; i++)
            {
                if (seats[i] <= now)
                {
                    seats[i] = now + Hold;
                    return;
                }
            }
            seats[0] = now + Hold;
        }
    }
}
