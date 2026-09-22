namespace OutpostZero.Shell
{
    /// <summary>
    /// Boot sits at build index 0 and streams the outpost scene behind the loading card.
    /// Menu, camp, the street, and results all run inside the outpost scene.
    /// Unity parks async progress at 0.9 until activation, so the bar reads that as full.
    /// </summary>
    public static class BootPlan
    {
        public const string BootScene = "Boot";
        public const string OutpostScene = "PrototypeArena";
        public const float Parked = 0.9f;
        public const float Budget = 5f;

        public static string SceneFor(FlowStep step)
        {
            return step == FlowStep.Boot ? BootScene : OutpostScene;
        }

        public static float Bar(float asyncProgress)
        {
            if (asyncProgress <= 0f) return 0f;
            if (asyncProgress >= Parked) return 1f;
            return asyncProgress / Parked;
        }

        public static bool Loaded(float asyncProgress)
        {
            return asyncProgress >= Parked;
        }

        public static bool InBudget(float seconds)
        {
            return seconds >= 0f && seconds < Budget;
        }
    }
}
