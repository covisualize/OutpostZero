namespace OutpostZero.Player
{
    /// <summary>
    /// Bleeding takes one health a second until it is bandaged, so ninety seconds costs ninety health.
    /// Infection moves from a fresh bite, to a slowing fever, to death. Antibiotics work before the last stage.
    /// </summary>
    public static class Affliction
    {
        public const float BleedPerSecond = 1f;
        public const float StageOne = 90f;
        public const float StageTwo = 180f;
        public const float PainTotal = 20f;
        public const float PainSpan = 20f;
        public const float BandageHeal = 10f;
        public const float AdrenalineSeconds = 6f;
        public const float AdrenalineSprint = 1.18f;

        public static int Stage(float seconds)
        {
            if (seconds < 0.05f) return 0;
            if (seconds < StageOne) return 1;
            if (seconds < StageTwo) return 2;
            return 3;
        }

        public static bool AntibioticsWork(int stage)
        {
            return stage == 1 || stage == 2;
        }

        public static bool Fatal(float seconds)
        {
            return Stage(seconds) >= 3;
        }

        public static float Advance(float seconds, float dt)
        {
            if (seconds < 0.05f) return seconds < 0f ? 0f : seconds;
            if (dt <= 0f) return seconds;
            float next = seconds + dt;
            float ceiling = StageTwo + 1f;
            return next > ceiling ? ceiling : next;
        }

        public static float Bite(float seconds)
        {
            if (seconds < 0.05f) return 0.05f;
            return Advance(seconds, 25f);
        }

        public static float BleedLoss(float seconds)
        {
            if (seconds <= 0f) return 0f;
            return BleedPerSecond * seconds;
        }

        public static float PainHeal(float elapsed)
        {
            if (elapsed <= 0f) return 0f;
            float span = elapsed > PainSpan ? PainSpan : elapsed;
            return PainTotal * (span / PainSpan);
        }

        public static string Label(int stage)
        {
            if (stage <= 0) return "";
            if (stage == 1) return "Infection I";
            if (stage == 2) return "Infection II";
            return "Infection III";
        }
    }
}
