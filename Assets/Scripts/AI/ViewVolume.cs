using System;

namespace OutpostZero.AI
{
    /// <summary>
    /// Camera frustum test for a small body. A spawn inside the volume would appear on screen.
    /// </summary>
    public static class ViewVolume
    {
        public static bool Seen(
            float eyeX, float eyeY, float eyeZ,
            float fwdX, float fwdY, float fwdZ,
            float rightX, float rightY, float rightZ,
            float upX, float upY, float upZ,
            float fovDeg, float aspect, float near, float far,
            float boxX, float boxY, float boxZ,
            float halfX, float halfY, float halfZ)
        {
            float flen = (float)Math.Sqrt(fwdX * fwdX + fwdY * fwdY + fwdZ * fwdZ);
            if (flen < 0.2f) return false;
            fwdX /= flen;
            fwdY /= flen;
            fwdZ /= flen;
            if (aspect < 0.2f) aspect = 1f;
            if (near < 0.01f) near = 0.01f;
            if (far < near + 0.5f) far = near + 0.5f;
            float vertical = (float)Math.Tan(fovDeg * 0.5 * Math.PI / 180.0);
            if (vertical < 0.05f) vertical = 0.05f;
            float horizontal = vertical * aspect;

            for (int i = 0; i < 8; i++)
            {
                float x = boxX + ((i & 1) == 0 ? -halfX : halfX);
                float y = boxY + ((i & 2) == 0 ? -halfY : halfY);
                float z = boxZ + ((i & 4) == 0 ? -halfZ : halfZ);
                if (Inside(eyeX, eyeY, eyeZ, fwdX, fwdY, fwdZ, rightX, rightY, rightZ, upX, upY, upZ, horizontal, vertical, near, far, x, y, z))
                    return true;
            }
            return Inside(eyeX, eyeY, eyeZ, fwdX, fwdY, fwdZ, rightX, rightY, rightZ, upX, upY, upZ, horizontal, vertical, near, far, boxX, boxY, boxZ);
        }

        private static bool Inside(
            float eyeX, float eyeY, float eyeZ,
            float fwdX, float fwdY, float fwdZ,
            float rightX, float rightY, float rightZ,
            float upX, float upY, float upZ,
            float horizontal, float vertical, float near, float far,
            float x, float y, float z)
        {
            float dx = x - eyeX;
            float dy = y - eyeY;
            float dz = z - eyeZ;
            float depth = dx * fwdX + dy * fwdY + dz * fwdZ;
            if (depth < near || depth > far) return false;
            float side = dx * rightX + dy * rightY + dz * rightZ;
            float lift = dx * upX + dy * upY + dz * upZ;
            if (side < 0f) side = -side;
            if (lift < 0f) lift = -lift;
            return side <= depth * horizontal && lift <= depth * vertical;
        }
    }
}
