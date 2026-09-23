using UnityEngine;

namespace OutpostZero.Player
{
    /// <summary>
    /// Rules for the humanoid rig on top of the locomotion controller: how hard the spine and head turn
    /// toward the aim point, how far the weapon socket may follow the right hand, how fast the reload clip
    /// plays so it ends with the reload timer, and which clip events the body listens for.
    /// </summary>
    public static class AimRig
    {
        /// <summary>The widest the chest turns from the hips before the whole body has to follow.</summary>
        public const float MaxTwist = 70f;
        public const float BodyWeight = 0.4f;
        public const float HeadWeight = 0.9f;
        public const float Clamp = 0.5f;
        /// <summary>The socket keeps within this many metres of its rest point, so a death pose cannot fling the gun.</summary>
        public const float MaxDrift = 0.3f;
        /// <summary>A reload clip event before this share of the timer is ignored.</summary>
        public const float ReloadEarliest = 0.85f;
        public const float BlendRate = 8f;

        public const string ReloadDone = "OnReloadAnimComplete";
        public const string Footstep = "OnFootstep";

        public static float Weight(bool alive, bool sprinting, bool reloading, bool aimingDownSights)
        {
            if (!alive) return 0f;
            if (aimingDownSights) return 1f;
            if (sprinting) return 0.2f;
            if (reloading) return 0.5f;
            return 0.75f;
        }

        public static float Blend(float current, float target, float deltaTime)
        {
            float step = Mathf.Clamp01(deltaTime * BlendRate);
            return current + (target - current) * step;
        }

        /// <summary>
        /// Where the head looks: the aim point at chest height, pulled back inside <see cref="MaxTwist"/>
        /// of the body's facing and never nearer than 2 m, so a cursor on the feet does not fold the neck.
        /// </summary>
        public static Vector3 Target(Vector3 chest, Vector3 forward, Vector3 aim)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 1e-6f) forward = Vector3.forward;
            forward.Normalize();
            Vector3 to = aim - chest;
            to.y = 0f;
            float distance = to.magnitude;
            if (distance < 1e-4f) return chest + forward * 2f;
            Vector3 dir = to / distance;
            float yaw = Mathf.Atan2(forward.x * dir.z - forward.z * dir.x, Vector3.Dot(forward, dir)) * Mathf.Rad2Deg;
            if (Mathf.Abs(yaw) > MaxTwist)
            {
                float limit = Mathf.Sign(yaw) * MaxTwist * Mathf.Deg2Rad;
                float cos = Mathf.Cos(limit);
                float sin = Mathf.Sin(limit);
                dir = new Vector3(forward.x * cos - forward.z * sin, 0f, forward.x * sin + forward.z * cos);
            }
            return chest + dir * Mathf.Max(2f, distance);
        }

        public const float HandFollow = 0.6f;

        /// <summary>
        /// The socket's local position: its rest point moved by how far the right hand has swung from its own
        /// rest pose (both in the body's local space), scaled by <paramref name="follow"/> and capped at <see cref="MaxDrift"/>.
        /// </summary>
        public static Vector3 Socket(Vector3 rest, Vector3 handRest, Vector3 hand, float follow)
        {
            Vector3 drift = (hand - handRest) * Mathf.Clamp01(follow);
            if (drift.magnitude > MaxDrift) drift = drift.normalized * MaxDrift;
            return rest + drift;
        }

        /// <summary>Playback speed that stretches or squeezes the reload clip to the reload timer.</summary>
        public static float ReloadSpeed(float clipLength, float reloadSeconds)
        {
            if (clipLength <= 0.01f || reloadSeconds <= 0.01f) return 1f;
            return Mathf.Clamp(clipLength / reloadSeconds, 0.25f, 4f);
        }

        public static bool AcceptReloadEvent(float fill) => fill >= ReloadEarliest;

        /// <summary>
        /// Event times (as a share of the clip) the body adds to a clip by its bare name. Walks get two
        /// footfalls, the reload gets its completion at the end.
        /// </summary>
        public static float[] EventsFor(string clip, out string function)
        {
            switch (clip)
            {
                case "Walk":
                case "Sprint":
                case "CrouchWalk":
                case "Shamble":
                    function = Footstep;
                    return new[] { 0.25f, 0.75f };
                case "Reload":
                    function = ReloadDone;
                    return new[] { 0.95f };
                default:
                    function = "";
                    return new float[0];
            }
        }
    }
}
