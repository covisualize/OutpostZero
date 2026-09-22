namespace OutpostZero.Graphics
{
    /// <summary>
    /// A bullet hole and a scorch stay on the street until the run ends.
    /// Blood and oil still fade. A held mark drops off past the gore cull.
    /// </summary>
    public static class MarkStay
    {
        public const float Blood = 8f;
        public const float Oil = 8f;

        public static bool Holds(string kind)
        {
            return kind == "hole" || kind == "scorch";
        }

        public static float Life(string kind)
        {
            if (kind == "oil") return Oil;
            if (Holds(kind)) return 0f;
            return Blood;
        }

        public static bool OnStreet(Core.GameState state)
        {
            return state == Core.GameState.ExpeditionActive || state == Core.GameState.RaidActive;
        }

        public static bool Visible(string kind, bool street, bool near)
        {
            if (!Holds(kind)) return true;
            return street && near;
        }
    }
}
