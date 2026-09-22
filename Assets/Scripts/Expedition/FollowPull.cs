namespace OutpostZero.Expedition
{
    /// <summary>
    /// A swing or a cry from someone following you pulls an idle zombie onto them.
    /// A zombie already chasing the leader stays on the leader.
    /// </summary>
    public static class FollowPull
    {
        public static bool Chases(bool follower, bool following, bool alreadyChasing, OutpostZero.Core.NoiseType noise)
        {
            if (alreadyChasing || !follower || !following) return false;
            return noise == OutpostZero.Core.NoiseType.MeleeSwing
                || noise == OutpostZero.Core.NoiseType.ZombieScream;
        }
    }
}
