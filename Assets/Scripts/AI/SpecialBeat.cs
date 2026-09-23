namespace OutpostZero.AI
{
    /// <summary>
    /// A runner and a brute both pause, then close the gap. The clock is pure so the
    /// wind-up, the dash, and the cooldown can be checked without a scene.
    /// </summary>
    public static class SpecialBeat
    {
        public const float LungeNear = 3f;
        public const float LungeFar = 5f;
        public const float ChargeNear = 6f;
        public const float ChargeFar = 12f;
        public const float Windup = 0.4f;
        public const float ChargeWindup = 0.8f;
        public const float Dash = 0.45f;
        public const float WallStun = 1.5f;
        public const float LungeSpeed = 8f;
        public const float ChargeSpeed = 6.5f;
        public const float Cooldown = 4f;
        public const float LungeDamage = 30f;
        public const float ChargeKnockdown = 1.2f;
        /// <summary>The charge runs long enough to cover its whole line-up, so a brute that starts at 12 m arrives.</summary>
        public const float ChargeDash = ChargeFar / ChargeSpeed + 0.15f;

        public struct Clock
        {
            public int Phase;
            public float Left;
            public float Ready;
            public bool Struck;
        }

        public static bool InReach(float dist, bool charge)
        {
            float near = charge ? ChargeNear : LungeNear;
            float far = charge ? ChargeFar : LungeFar;
            return dist >= near && dist <= far;
        }

        public static float Speed(bool charge) => charge ? ChargeSpeed : LungeSpeed;

        public static bool Hits(Clock clock, float dist, float reach)
        {
            return clock.Phase == 2 && !clock.Struck && dist <= reach;
        }

        public static void Commit(float aimX, float aimZ, out float x, out float z)
        {
            float len = (float)System.Math.Sqrt(aimX * aimX + aimZ * aimZ);
            if (len < 0.2f)
            {
                x = 0f;
                z = 1f;
                return;
            }
            x = aimX / len;
            z = aimZ / len;
        }

        public static Clock Advance(Clock clock, bool inReach, float now, float dt, bool charge = false)
        {
            if (dt < 0f) dt = 0f;
            if (clock.Phase == 2)
            {
                clock.Left -= dt;
                if (clock.Left <= 0f)
                {
                    clock.Phase = 0;
                    clock.Left = 0f;
                    clock.Ready = now + Cooldown;
                }
                return clock;
            }

            if (clock.Phase == 1)
            {
                clock.Left -= dt;
                if (clock.Left <= 0f)
                {
                    clock.Phase = 2;
                    clock.Left = charge ? ChargeDash : Dash;
                    clock.Struck = false;
                }
                return clock;
            }

            if (!inReach || now < clock.Ready) return clock;
            clock.Phase = 1;
            clock.Left = charge ? ChargeWindup : Windup;
            return clock;
        }
    }
}
