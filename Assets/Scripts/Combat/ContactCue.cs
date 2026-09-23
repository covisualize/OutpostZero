using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A swing that meets flesh chops. A swing that only meets a wall clangs.
    /// A wound on the player grunts while they are still up.
    /// </summary>
    public static class ContactCue
    {
        public static string Impact(bool flesh, bool wall)
        {
            if (flesh) return "chop";
            if (wall) return "clang";
            return "";
        }

        public static bool Wall(int layer)
        {
            return layer == GameLayers.Environment;
        }

        public static bool Pain(bool player, float healthAfter)
        {
            return player && healthAfter > 0.001f;
        }
    }
}
