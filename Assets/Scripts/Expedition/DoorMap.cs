namespace OutpostZero.Expedition
{
    /// <summary>
    /// A street door and the room it opens. The room sits ninety meters east, off the scavenged block.
    /// </summary>
    public static class DoorMap
    {
        public const float Shift = 90f;

        public static void Inside(float x, float z, out float insideX, out float insideZ)
        {
            insideX = x + Shift;
            insideZ = z;
        }

        public static void Outside(float insideX, float insideZ, out float x, out float z)
        {
            x = insideX - Shift;
            z = insideZ;
        }

        public static bool IsInside(float x)
        {
            return x >= Shift * 0.5f;
        }

        public static string Prompt(bool leaving)
        {
            return Prompt(leaving, "en");
        }

        public static string Prompt(bool leaving, string language)
        {
            string key = leaving ? "ask.out" : "ask.in";
            if (string.IsNullOrEmpty(language)) return Shell.Loc.T(key);
            return Shell.Loc.T(key, language);
        }

        public static string Cross(bool leaving, string language)
        {
            string key = leaving ? "door.street" : "door.inside";
            if (string.IsNullOrEmpty(language)) return Shell.Loc.T(key);
            return Shell.Loc.T(key, language);
        }
    }
}
