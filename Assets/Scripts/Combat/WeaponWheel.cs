using System;

namespace OutpostZero.Combat
{
    /// <summary>
    /// Four guns around a stick. Up is the first slot. A short hold opens the wheel.
    /// </summary>
    public static class WeaponWheel
    {
        public const int Slots = 4;
        public const float Deadzone = 0.35f;
        public const float HoldSeconds = 0.16f;

        public static int Pick(float x, float y)
        {
            if (x * x + y * y < Deadzone * Deadzone) return -1;
            double angle = Math.Atan2(x, y);
            if (angle < 0d) angle += Math.PI * 2d;
            double shifted = angle + Math.PI * 0.25d;
            if (shifted >= Math.PI * 2d) shifted -= Math.PI * 2d;
            int sector = (int)(shifted / (Math.PI * 0.5d));
            if (sector < 0) return 0;
            if (sector > 3) return 3;
            return sector;
        }

        public static bool Shown(bool held, float seconds)
        {
            return held && seconds >= HoldSeconds;
        }

        public static int Release(int highlight, int current)
        {
            if (highlight < 0 || highlight >= Slots) return current;
            return highlight;
        }

        public static string Row(int slot, string name, bool hot)
        {
            return Row(slot, name, hot, "en");
        }

        public static string Row(int slot, string name, bool hot, string language)
        {
            string body = string.IsNullOrEmpty(name) ? Word(language) : name;
            string mark = hot ? ">" : " ";
            return mark + " " + (slot + 1) + "  " + body;
        }

        private static string Word(string language)
        {
            if (string.IsNullOrEmpty(language)) return Shell.Loc.T("wheel.empty");
            return Shell.Loc.T("wheel.empty", language);
        }
    }
}
