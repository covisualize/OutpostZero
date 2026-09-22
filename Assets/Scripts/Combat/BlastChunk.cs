using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A powder blast throws barrel bits. Toxic and oil do not.
    /// The wake, the fuse, and the damage stay as they were.
    /// </summary>
    public static class BlastChunk
    {
        public const int Count = 6;
        public const float Life = 1.1f;
        public const float Speed = 4.5f;

        public static bool Throws(HazardKind kind)
        {
            return kind == HazardKind.Explosive;
        }
    }
}
