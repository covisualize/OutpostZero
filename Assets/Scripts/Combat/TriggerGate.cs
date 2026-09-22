namespace OutpostZero.Combat
{
    /// <summary>
    /// A rifle keeps firing while the trigger is held. A pistol and a shotgun fire once per press.
    /// </summary>
    public static class TriggerGate
    {
        public static bool ShouldFire(bool automatic, bool held, bool pressed)
        {
            if (automatic) return held;
            return pressed;
        }
    }
}
