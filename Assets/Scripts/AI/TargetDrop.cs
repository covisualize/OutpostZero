namespace OutpostZero.AI
{
    /// <summary>
    /// A chase ends when the target is gone.
    /// A fallen follower is no longer a body to bite.
    /// </summary>
    public static class TargetDrop
    {
        public static bool Gone(bool hasTarget, bool active)
        {
            return hasTarget && !active;
        }
    }
}
