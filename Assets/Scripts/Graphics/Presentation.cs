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
            bool es = language == "es";
            if (dx * dx + dz * dz < 0.04f) return es ? "aquí" : "here";
            float ax = dx < 0f ? -dx : dx;
            float az = dz < 0f ? -dz : dz;
            bool north = dz >= 0f;
            bool east = dx >= 0f;
            if (az >= ax * 2f) return north ? (es ? "norte" : "north") : (es ? "sur" : "south");
            if (ax >= az * 2f) return east ? (es ? "este" : "east") : (es ? "oeste" : "west");
            if (north && east) return es ? "noreste" : "northeast";
            if (north) return es ? "noroeste" : "northwest";
            if (east) return es ? "sureste" : "southeast";
            return es ? "suroeste" : "southwest";
        }

        public static string Caption(NoiseType type, float dx, float dz, string language)
        {
            string where = Compass(dx, dz, language);
            bool es = language == "es";
            if (type == NoiseType.ZombieScream) return es ? "[Grito, " + where + "]" : "[Zombie scream, " + where + "]";
            if (type == NoiseType.GunshotLoud || type == NoiseType.GunshotQuiet) return es ? "[Disparo, " + where + "]" : "[Gunshot, " + where + "]";
            if (type == NoiseType.Explosion) return es ? "[Explosión, " + where + "]" : "[Explosion, " + where + "]";
            if (type == NoiseType.Thunder) return es ? "[Trueno, " + where + "]" : "[Thunder, " + where + "]";
            if (type == NoiseType.ObjectBroken) return es ? "[Rotura, " + where + "]" : "[Something broke, " + where + "]";
            if (type == NoiseType.MeleeSwing) return es ? "[Corte, " + where + "]" : "[Blade, " + where + "]";
            if (type == NoiseType.BleedDrip) return es ? "[Goteo, " + where + "]" : "[Drip, " + where + "]";
            if (type == NoiseType.DoorSwing) return es ? "[Puerta, " + where + "]" : "[Door, " + where + "]";
            if (type == NoiseType.ShellClink) return es ? "[Casquillo, " + where + "]" : "[Shell, " + where + "]";
            if (type == NoiseType.RationBite) return es ? "[Bocado, " + where + "]" : "[Bite, " + where + "]";
            return "";
        }
    }
}
