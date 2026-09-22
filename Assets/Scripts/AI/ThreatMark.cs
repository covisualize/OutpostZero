namespace OutpostZero.AI
{
    /// <summary>
    /// A chase is an exclamation mark. A search is a question mark. Idle zombies stay unmarked.
    /// </summary>
    public static class ThreatMark
    {
        public const float Hear = 28f;
        public const int Limit = 6;

        public static string Glyph(ZombieAI.ZombieState state)
        {
            if (state == ZombieAI.ZombieState.Chase || state == ZombieAI.ZombieState.Attack) return "!";
            if (state == ZombieAI.ZombieState.InvestigateNoise || state == ZombieAI.ZombieState.Searching) return "?";
            return "";
        }

        public static bool Near(float distance)
        {
            return distance >= 0f && distance <= Hear;
        }

        public static string Line(int bangs, int questions)
        {
            if (bangs < 0) bangs = 0;
            if (questions < 0) questions = 0;
            int shownBangs = bangs > 4 ? 4 : bangs;
            int room = Limit - shownBangs;
            if (room < 0) room = 0;
            int shownQuestions = questions > room ? room : questions;
            string text = "";
            for (int i = 0; i < shownBangs; i++)
            {
                if (text.Length > 0) text += " ";
                text += "!";
            }
            for (int i = 0; i < shownQuestions; i++)
            {
                if (text.Length > 0) text += " ";
                text += "?";
            }
            if (bangs + questions > shownBangs + shownQuestions) text += " +";
            return text;
        }
    }
}
