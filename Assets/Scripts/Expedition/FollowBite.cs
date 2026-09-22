namespace OutpostZero.Expedition
{
    /// <summary>
    /// A zombie inside the swing can still bite the follower.
    /// The bite comes home on the roster, and it does not stack past three.
    /// </summary>
    public static class FollowBite
    {
        public const float Reach = 1.2f;
        public const float Gap = 2.4f;
        public const int Wound = 1;
        public const int Cap = 3;

        public static bool Due(float last, float now, float nearest)
        {
            if (nearest < 0f || nearest > Reach) return false;
            if (last <= 0f) return true;
            if (now < last) return false;
            return OutpostZero.Core.Tick.Past(now, last, Gap);
        }

        public static int After(int held)
        {
            if (held < 0) held = 0;
            if (held >= Cap) return Cap;
            int next = held + Wound;
            return next > Cap ? Cap : next;
        }

        public static int Bring(int held)
        {
            return OutpostZero.Colony.HomeSick.Carry(0, held);
        }

        public static string Line(string name, string language)
        {
            return OutpostZero.Shell.StreetAsk.Bitten(name, language);
        }
    }
}
