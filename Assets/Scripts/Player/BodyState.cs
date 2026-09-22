namespace OutpostZero.Player
{
    /// <summary>
    /// The leader's fatigue, bleed, and fever stay on the existing save.
    /// An old file has no fatigue mark, so the body keeps the value it already has.
    /// </summary>
    public static class BodyState
    {
        public const int FeverCap = 1810;

        public static int PackFatigue(float fatigue)
        {
            if (fatigue < 0f) fatigue = 0f;
            if (fatigue > 100f) fatigue = 100f;
            return (int)System.Math.Round(fatigue * 10f);
        }

        public static float UnpackFatigue(int tenths, int known)
        {
            if (known == 0) return -1f;
            if (tenths < 0) return 0f;
            if (tenths > 1000) return 100f;
            return tenths / 10f;
        }

        public static int PackBleed(bool bleeding)
        {
            return bleeding ? 1 : 0;
        }

        public static bool UnpackBleed(int stored)
        {
            return stored > 0;
        }

        public static int PackInfection(float seconds)
        {
            if (seconds < 0.05f) return 0;
            int tenths = (int)System.Math.Round(seconds * 10f);
            if (tenths < 0) return 0;
            return tenths > FeverCap ? FeverCap : tenths;
        }

        public static float UnpackInfection(int tenths)
        {
            if (tenths <= 0) return 0f;
            if (tenths > FeverCap) tenths = FeverCap;
            return tenths / 10f;
        }
    }
}
