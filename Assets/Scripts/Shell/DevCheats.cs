namespace OutpostZero.Shell
{
    /// <summary>
    /// The hidden dev menu: scene jump, god mode, spawn zombies, give items, and skip time.
    /// It only opens in the editor or a development build, so a release player never sees it.
    /// </summary>
    public static class DevCheats
    {
        public const float SkipHours = 6f;
        public const int SpawnCount = 3;

        public static readonly string[] Kit = { "medkit", "ammo_rifle", "canned_food", "molotov" };

        public static bool God { get; private set; }

        public static bool Allowed(bool editor, bool developmentBuild)
        {
            return editor || developmentBuild;
        }

        public static bool Toggle(bool allowed, bool open)
        {
            return allowed && !open;
        }

        public static void SetGod(bool on)
        {
            God = on;
        }

        public static bool Shielded(bool dodging)
        {
            return God || dodging;
        }
    }
}
