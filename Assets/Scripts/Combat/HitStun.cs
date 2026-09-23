using OutpostZero.Core;

namespace OutpostZero.Combat
{
    /// <summary>
    /// A shotgun staggers longer than a pistol. A brute keeps thirty percent of that stagger.
    /// </summary>
    public static class HitStun
    {
        public static float Seconds(WeaponType type)
        {
            if (type == WeaponType.Shotgun) return 0.6f;
            if (type == WeaponType.Melee) return 0.4f;
            return 0.25f;
        }

        /// <summary>A stun this long (a shotgun blast, a charge into a wall) reels the body back; a shorter one flinches.</summary>
        public const float StaggerFrom = 0.5f;

        public static bool Staggers(float seconds) => seconds >= StaggerFrom;

        /// <summary>The brute's resistance covers blows it takes, not the stun of its own charge into a wall.</summary>
        public static float Taken(float seconds, bool brute, bool fromBlow)
        {
            return Resist(seconds, brute && fromBlow);
        }

        public static float Resist(float seconds, bool brute)
        {
            if (seconds < 0f) seconds = 0f;
            if (brute) seconds *= 0.3f;
            if (seconds < 0.05f) return 0.05f;
            return seconds;
        }
    }
}
