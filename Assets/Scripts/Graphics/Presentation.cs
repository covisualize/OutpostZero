using OutpostZero.Core;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Gore, comfort toggles, brightness, and directional captions.
    /// A stored 0 keeps the old default so a schema-1 save does not flip them off.
    /// </summary>
    public static class Presentation
    {
        public static int Gore(int stored)
        {
            if (stored <= 1) return 1;
            if (stored == 2) return 2;
            return 0;
        }

        public static string GoreName(int stored)
        {
            int level = Gore(stored);
            if (level <= 0) return "Off";
            if (level >= 2) return "Heavy";
            return "Standard";
        }

        public static int NextGore(int stored)
        {
            int level = Gore(stored);
            if (level == 1) return 2;
            if (level == 2) return 3;
            return 1;
        }

        public static bool HitStop(int stored)
        {
            return stored != 2;
        }

        public static bool DamageNumbers(int stored)
        {
            return stored != 2;
        }

        public static int ToggleOff(int stored)
        {
            return HitStop(stored) ? 2 : 1;
        }

        public static float Opacity(float stored)
        {
            if (stored <= 0f) return 1f;
            if (stored < 0.45f) return 0.45f;
            if (stored > 1f) return 1f;
            return stored;
        }

        public static float Brightness(float stored)
        {
            if (stored <= 0f) return 1f;
            if (stored < 0.6f) return 0.6f;
            if (stored > 1.4f) return 1.4f;
            return stored;
        }

        public static float Exposure(float stored)
        {
            return 0.15f + (Brightness(stored) - 1f) * 0.8f;
        }

        public static bool MotionBlur(int stored)
        {
            return stored == 1;
        }

        public static string Compass(float dx, float dz, string language)
        {
            return Say("dir." + Heading(dx, dz), language);
        }

        public static string Heading(float dx, float dz)
        {
            if (dx * dx + dz * dz < 0.04f) return "here";
            float ax = dx < 0f ? -dx : dx;
            float az = dz < 0f ? -dz : dz;
            bool north = dz >= 0f;
            bool east = dx >= 0f;
            if (az >= ax * 2f) return north ? "north" : "south";
            if (ax >= az * 2f) return east ? "east" : "west";
            if (north) return east ? "northeast" : "northwest";
            return east ? "southeast" : "southwest";
        }

        public static string Sound(NoiseType type)
        {
            switch (type)
            {
                case NoiseType.ZombieScream: return "scream";
                case NoiseType.GunshotLoud:
                case NoiseType.GunshotQuiet: return "gunshot";
                case NoiseType.Explosion: return "explosion";
                case NoiseType.Thunder: return "thunder";
                case NoiseType.ObjectBroken: return "broken";
                case NoiseType.MeleeSwing: return "blade";
                case NoiseType.BleedDrip: return "drip";
                case NoiseType.DoorSwing: return "door";
                case NoiseType.ShellClink: return "shell";
                case NoiseType.RationBite: return "bite";
                case NoiseType.Cough: return "cough";
                default: return "";
            }
        }

        public static string Caption(NoiseType type, float dx, float dz, string language)
        {
            string sound = Sound(type);
            if (sound.Length == 0) return "";
            return "[" + Say("cap." + sound, language) + ", " + Compass(dx, dz, language) + "]";
        }

        private static string Say(string key, string language)
        {
            return string.IsNullOrEmpty(language) ? Shell.Loc.T(key) : Shell.Loc.T(key, language);
        }
    }
}
