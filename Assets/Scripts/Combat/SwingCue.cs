using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A blade whooshes on the swing itself. A gun does not.
    /// An authored clip can still play beside it.
    /// </summary>
    public static class SwingCue
    {
        public const float Volume = 0.36f;

        public static string Sound(WeaponType type)
        {
            return type == WeaponType.Melee ? "whoosh" : "";
        }
    }
}
