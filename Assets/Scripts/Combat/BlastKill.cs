using UnityEngine;

namespace OutpostZero.Combat
{
    /// <summary>
    /// Marks the damage an explosion deals, so a body it kills can be thrown instead of falling
    /// in place. Blasts open a scope around their damage loop; chained blasts nest.
    /// </summary>
    public static class BlastKill
    {
        public const float Lift = 0.45f;
        public const float MaxPush = 14f;

        private static int depth;
        private static Vector3 origin;
        private static float force;
        private static float radius;

        public static bool Active => depth > 0;
        public static Vector3 Origin => origin;
        public static float Force => force;
        public static float Radius => radius;

        public static void Begin(Vector3 at, float push, float reach)
        {
            depth++;
            origin = at;
            force = push;
            radius = reach;
        }

        /// <summary>The throw for a body the current blast just killed.</summary>
        public static Vector3 PushFor(Vector3 body)
        {
            return Push(body, origin, force, radius);
        }

        public static void End()
        {
            if (depth > 0) depth--;
        }

        /// <summary>Velocity change for a body at <paramref name="body"/>: away from the blast and up, weaker with distance.</summary>
        public static Vector3 Push(Vector3 body, Vector3 at, float push, float reach)
        {
            Vector3 away = body - at;
            away.y = 0f;
            float distance = away.magnitude;
            Vector3 flat = distance > 0.001f ? away / distance : Vector3.forward;
            if (reach <= 0.1f) reach = 1f;
            float falloff = Mathf.Clamp01(1f - distance / reach) * 0.7f + 0.3f;
            float speed = Mathf.Min(push * falloff, MaxPush);
            return (flat + Vector3.up * Lift).normalized * speed;
        }
    }
}
