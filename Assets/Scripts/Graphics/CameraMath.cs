using System;

namespace OutpostZero.Graphics
{
    /// <summary>
    /// Pure camera geometry shared by the Cinemachine rig, the target driver and the tests: shot
    /// offsets from pitch and distance, aim lead, shake falloff, edge scrolling, zoom, and the
    /// confiner box that keeps the fence line (plus a band of skyline) as far as the view reaches.
    /// </summary>
    public static class CameraMath
    {
        private const float Rad = (float)(Math.PI / 180.0);

        /// <summary>Height above and distance behind the target for a camera at this pitch.</summary>
        public static void Offset(float pitch, float distance, out float up, out float back)
        {
            up = distance * (float)Math.Sin(pitch * Rad);
            back = distance * (float)Math.Cos(pitch * Rad);
        }

        /// <summary>Pitch and distance for an offset, the inverse of <see cref="Offset"/>.</summary>
        public static void Shot(float up, float back, out float pitch, out float distance)
        {
            distance = (float)Math.Sqrt(up * up + back * back);
            pitch = (float)(Math.Atan2(up, back) / Rad);
        }

        /// <summary>Pointer lead: a share of the way to the cursor on the ground, capped.</summary>
        public static void Lead(float dx, float dz, float influence, float max, out float x, out float z)
        {
            x = dx * influence;
            z = dz * influence;
            Cap(ref x, ref z, max);
        }

        /// <summary>Right-stick lead: the stick's tilt scaled to the reach, capped at the reach.</summary>
        public static void StickLead(float sx, float sy, float reach, out float x, out float z)
        {
            x = sx * reach;
            z = sy * reach;
            Cap(ref x, ref z, reach);
        }

        /// <summary>
        /// Shake reaching a listener <paramref name="distance"/> away. A radius of 0 means the shake
        /// is the listener's own (recoil) and does not fade; otherwise it falls off with the square.
        /// </summary>
        public static float Attenuate(float force, float distance, float radius)
        {
            if (force <= 0f) return 0f;
            if (radius <= 0f) return force;
            if (distance >= radius) return 0f;
            float t = 1f - Math.Max(0f, distance) / radius;
            return force * t * t;
        }

        /// <summary>
        /// Edge scrolling: -1..1 per axis, ramping in over the outer <paramref name="margin"/>
        /// share of the screen. Outside the window (pointer lost) it does not scroll.
        /// </summary>
        public static void EdgeScroll(float px, float py, float width, float height, float margin, out float x, out float z)
        {
            x = 0f;
            z = 0f;
            if (width <= 0f || height <= 0f || margin <= 0f) return;
            if (px < 0f || py < 0f || px > width || py > height) return;
            x = Ramp(px / width, margin);
            z = Ramp(py / height, margin);
        }

        /// <summary>Wheel zoom for the overview: scrolling up moves in by one step.</summary>
        public static float Zoom(float distance, float scroll, float step, float min, float max)
        {
            if (scroll > 0.05f) distance -= step;
            else if (scroll < -0.05f) distance += step;
            return Clamp(distance, min, max);
        }

        /// <summary>Horizontal field of view for a vertical one on a screen of this aspect.</summary>
        public static float HorizontalFov(float verticalFov, float aspect)
        {
            float half = (float)Math.Atan(Math.Tan(verticalFov * 0.5f * Rad) * aspect);
            return half * 2f / Rad;
        }

        /// <summary>
        /// Where the ground view reaches, relative to the camera's ground point, for a pitched camera
        /// at <paramref name="height"/>: the far edge ahead and the near edge (negative is behind).
        /// </summary>
        public static void Reach(float pitch, float height, float verticalFov, out float near, out float far)
        {
            float half = verticalFov * 0.5f;
            float top = Math.Max(pitch - half, 8f);
            far = height / (float)Math.Tan(top * Rad);
            near = height / (float)Math.Tan((pitch + half) * Rad);
        }

        /// <summary>
        /// Box for the camera position inside a square yard of half-size <paramref name="open"/>.
        /// The view may run <paramref name="slack"/> metres past the fence (the skyline band), and the
        /// box never gets so tight that a target standing at the fence leaves the frame.
        /// </summary>
        public static void Confine(float open, float pitch, float distance, float verticalFov, float aspect, float slack,
            out float halfX, out float minZ, out float maxZ)
        {
            Offset(pitch, distance, out float up, out float back);
            Reach(pitch, up, verticalFov, out float near, out float far);
            float halfWidth = distance * (float)Math.Tan(HorizontalFov(verticalFov, aspect) * 0.5f * Rad);
            halfX = Math.Max(open + slack - halfWidth, open - halfWidth * 0.6f);
            halfX = Math.Max(halfX, 0f);
            minZ = -open - slack - near;
            maxZ = open + slack - far;
            float keep = open - back - far * 0.6f;
            if (maxZ < keep) maxZ = keep;
            if (minZ > maxZ)
            {
                float mid = (minZ + maxZ) * 0.5f;
                minZ = mid;
                maxZ = mid;
            }
        }

        /// <summary>Eased 0..1 progress over <paramref name="seconds"/>; 0 seconds is a cut.</summary>
        public static float Ease(float elapsed, float seconds)
        {
            if (seconds <= 0f) return 1f;
            float t = Clamp(elapsed / seconds, 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Moves <paramref name="value"/> toward <paramref name="target"/> at 1/<paramref name="seconds"/> per second.</summary>
        public static float Toward(float value, float target, float seconds, float deltaTime)
        {
            if (seconds <= 0f) return target;
            float step = deltaTime / seconds;
            if (value < target) return Math.Min(target, value + step);
            return Math.Max(target, value - step);
        }

        public static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static float Ramp(float at, float margin)
        {
            if (at < margin) return -(1f - at / margin);
            if (at > 1f - margin) return (at - (1f - margin)) / margin;
            return 0f;
        }

        private static void Cap(ref float x, ref float z, float max)
        {
            float length = (float)Math.Sqrt(x * x + z * z);
            if (max <= 0f)
            {
                x = 0f;
                z = 0f;
                return;
            }
            if (length <= max) return;
            float scale = max / length;
            x *= scale;
            z *= scale;
        }
    }
}
