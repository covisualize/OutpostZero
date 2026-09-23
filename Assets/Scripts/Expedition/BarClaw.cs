namespace OutpostZero.Expedition
{
    /// <summary>
    /// A walker claw takes one hit off a barred door. A charge spends the whole bar.
    /// </summary>
    public static class BarClaw
    {
        public const float Reach = 1.1f;
        public const float Gap = 0.8f;
        public const int Charge = 3;

        public static int Rake(int hitsLeft)
        {
            return DoorBar.After(hitsLeft);
        }

        public static int Rush(int hitsLeft)
        {
            if (hitsLeft <= 0) return 0;
            int left = hitsLeft;
            int n = 0;
            while (DoorBar.Holds(left) && n < Charge)
            {
                left = DoorBar.After(left);
                n++;
            }
            return left;
        }

        public static bool Opens(int hitsLeft, bool charge)
        {
            if (!DoorBar.Holds(hitsLeft)) return false;
            int after = charge ? Rush(hitsLeft) : Rake(hitsLeft);
            return !DoorBar.Holds(after);
        }

        public static string Rattle(string language)
        {
            if (string.IsNullOrEmpty(language)) return OutpostZero.Shell.Loc.T("door.rattle");
            return OutpostZero.Shell.Loc.T("door.rattle", language);
        }
    }
}
