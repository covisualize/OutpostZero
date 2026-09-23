namespace OutpostZero.Sensory
{
    /// <summary>
    /// The ground under a step changes how far the noise carries.
    /// Metal and hard pavement reach farther. Wood and gravel stay closer.
    /// A named water surface is its own lift. Rain puddles stay on PuddleStep.
    /// </summary>
    public static class StepReach
    {
        public const float Metal = 1.35f;
        public const float Hard = 1.12f;
        public const float Wood = 0.9f;
        public const float Gravel = 0.75f;
        public const float Water = 1.18f;
        public const float GlassCrunchReach = 1.28f;

        public static float Scale(string stepId)
        {
            if (stepId == "step_metal") return Metal;
            if (stepId == "step_hard") return Hard;
            if (stepId == "step_wood") return Wood;
            if (stepId == "step_gravel") return Gravel;
            if (stepId == "step_water") return Water;
            if (stepId == "step_glass") return GlassCrunchReach;
            return 1f;
        }

        public static float Radius(float radius, string stepId)
        {
            if (radius < 0f) radius = 0f;
            return radius * Scale(stepId);
        }
    }
}
