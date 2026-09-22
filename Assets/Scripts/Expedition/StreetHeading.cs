using System;

namespace OutpostZero.Expedition
{
    /// <summary>
    /// Compass bearings on the street. Zero degrees faces north (+Z). Positive is clockwise, toward east.
    /// </summary>
    public static class StreetHeading
    {
        public const float Half = 90f;

        public static float Degrees(float x, float z)
        {
            if (x * x + z * z < 0.0000001f) return 0f;
            return (float)(Math.Atan2(x, z) * (180.0 / Math.PI));
        }

        public static float Wrap(float degrees)
        {
            while (degrees > 180f) degrees -= 360f;
            while (degrees < -180f) degrees += 360f;
            return degrees;
        }

        public static float Delta(float faceX, float faceZ, float fromX, float fromZ, float toX, float toZ)
        {
            return Wrap(Degrees(toX - fromX, toZ - fromZ) - Degrees(faceX, faceZ));
        }

        public static float Incoming(float faceX, float faceZ, float hitX, float hitZ)
        {
            return Wrap(Degrees(-hitX, -hitZ) - Degrees(faceX, faceZ));
        }

        public static bool OnStrip(float delta, float halfWidth, out float x)
        {
            if (delta > Half || delta < -Half)
            {
                x = 0f;
                return false;
            }
            x = (delta / Half) * halfWidth;
            return true;
        }

        public static string Sector(float delta)
        {
            float wrapped = Wrap(delta);
            float span = Math.Abs(wrapped);
            if (span <= 45f) return "front";
            if (span >= 135f) return "back";
            return wrapped > 0f ? "right" : "left";
        }

        public static string Cardinal(float worldYaw)
        {
            float turn = Wrap(worldYaw);
            if (turn < 0f) turn += 360f;
            if (turn >= 315f || turn < 45f) return "N";
            if (turn < 135f) return "E";
            if (turn < 225f) return "S";
            return "W";
        }

        public static string Mark(string label, float delta)
        {
            if (!OnStrip(delta, 1f, out _)) return label + " behind";
            return label + " " + Sector(delta);
        }

        public static string Readout(float faceX, float faceZ, float fromX, float fromZ, bool showPoi, float poiX, float poiZ, bool showGate, float gateX, float gateZ)
        {
            string text = Cardinal(Degrees(faceX, faceZ));
            if (showPoi) text += "   " + Mark("POI", Delta(faceX, faceZ, fromX, fromZ, poiX, poiZ));
            if (showGate) text += "   " + Mark("Gate", Delta(faceX, faceZ, fromX, fromZ, gateX, gateZ));
            return text;
        }
    }
}
