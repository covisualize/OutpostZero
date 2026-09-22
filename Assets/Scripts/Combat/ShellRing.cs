using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A spent casing calls anything close once it hits.
    /// The ring sits over a crouch and under a walk.
    /// </summary>
    public static class ShellRing
    {
        public const float Radius = 3.2f;
        public const float Loud = 0.28f;

        public static bool Calls(WeaponType type)
        {
            return BrassCue.Ejects(type);
        }
    }
}
