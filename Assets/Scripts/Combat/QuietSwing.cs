using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A crouch keeps a blade swing close. A standing swing and every gun keep their reach.
    /// </summary>
    public static class QuietSwing
    {
        public const float Crouch = 0.45f;

        public static float Scale(bool crouch, WeaponType type)
        {
            if (type != WeaponType.Melee || !crouch) return 1f;
            return Crouch;
        }

        public static float Radius(float radius, bool crouch, WeaponType type)
        {
            if (radius < 0f) radius = 0f;
            return radius * Scale(crouch, type);
        }
    }
}
