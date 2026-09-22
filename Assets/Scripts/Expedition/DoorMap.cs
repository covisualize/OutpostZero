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
            return leaving ? "Step outside" : "Step inside";
        }
    }
}
