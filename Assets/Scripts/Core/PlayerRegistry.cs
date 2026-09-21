using OutpostZero.Player;

namespace OutpostZero.Core
{
    /// <summary>
    /// Cached expedition leader. Zombies and HUD read this instead of searching the scene every frame.
    /// </summary>
    public static class PlayerRegistry
    {
        public static PlayerController Current { get; private set; }

        public static void Register(PlayerController player)
        {
            if (player != null)
            {
                Current = player;
            }
        }

        public static void Unregister(PlayerController player)
        {
            if (Current == player)
            {
                Current = null;
            }
        }
    }
}
