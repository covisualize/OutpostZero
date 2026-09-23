using UnityEngine;

namespace OutpostZero.AI
{
    public static class SwingClock
    {
        public const float HitAt = 0.6f;

        /// <summary>When the clip drives the bite, the clock only lands it if the impact event never came.</summary>
        public const float LateHit = 0.95f;

        public static float Advance(float t, float dt, float duration)
        {
            if (t < 0f) t = 0f;
            if (duration <= 0.01f) return 1f;
            if (dt < 0f) dt = 0f;
            float next = t + dt / duration;
            return next > 1f ? 1f : next;
        }

        public static bool Connects(float before, float after)
        {
            return before < HitAt && after >= HitAt;
        }

        /// <summary>One bite per swing: the clip's impact event or, failing that, the clock.</summary>
        public static bool Bites(float before, float after, bool bitten, bool clipDriven)
        {
            if (bitten) return false;
            float at = clipDriven ? LateHit : HitAt;
            return before < at && after >= at;
        }

        /// <summary>An impact event counts only mid-swing and only once.</summary>
        public static bool Hears(float swing, bool bitten)
        {
            return swing >= 0f && swing < 1f && !bitten;
        }
    }
}
