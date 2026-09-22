namespace OutpostZero.Combat
{
    /// <summary>
    /// The red health bar drops with the hit. The darker trail catches up slowly, and a heal snaps both.
    /// </summary>
    public static class HealthGhost
    {
        public const float Drain = 0.35f;

        public static float Follow(float shown, float actual, float dt)
        {
            if (actual >= shown - 0.0001f) return actual;
            if (dt <= 0f) return shown;
            float next = shown - Drain * dt;
            return next < actual ? actual : next;
        }
    }
}
