namespace OutpostZero.Core
{
    /// <summary>
    /// Six display sizes. Zero stays on the monitor's current mode so an old save does not resize the window.
    /// </summary>
    public static class DisplayModes
    {
        public const int Count = 6;

        public static int Next(int mode)
        {
            if (mode < 0 || mode >= Count) return 1;
            return (mode + 1) % Count;
        }

        public static string Name(int mode)
        {
            switch (mode)
            {
                case 1: return "1280 x 720";
                case 2: return "1600 x 900";
                case 3: return "1920 x 1080";
                case 4: return "2560 x 1440";
                case 5: return "3840 x 2160";
                default: return "Native";
            }
        }

        public static bool Size(int mode, out int width, out int height)
        {
            switch (mode)
            {
                case 1: width = 1280; height = 720; return true;
                case 2: width = 1600; height = 900; return true;
                case 3: width = 1920; height = 1080; return true;
                case 4: width = 2560; height = 1440; return true;
                case 5: width = 3840; height = 2160; return true;
                default: width = 0; height = 0; return false;
            }
        }
    }
}
