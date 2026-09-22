using System;

namespace OutpostZero.Colony
{
    /// <summary>
    /// Three districts still have someone alive. The same street always offers the same person,
    /// and the roster will not take them twice or past eight names.
    /// </summary>
    public static class RescueBook
    {
        public const int RosterCap = 8;
        public const float FollowGap = 1.6f;
        public const float CatchUp = 14f;

        public struct Offer
        {
            public string Id;
            public string Name;
            public string Trait;
        }

        public static Offer For(string districtId)
        {
            if (districtId == "old_hospital") return new Offer { Id = "rescue_hospital", Name = "Imani Cole", Trait = "Field Medic" };
            if (districtId == "police_station") return new Offer { Id = "rescue_police", Name = "Dell Orth", Trait = "Watchful" };
            if (districtId == "mall") return new Offer { Id = "rescue_mall", Name = "Nia Pell", Trait = "Scrounger" };
            return new Offer();
        }

        public static bool CanJoin(string id, string[] rosterIds, int rosterCount)
        {
            if (string.IsNullOrEmpty(id) || rosterCount >= RosterCap) return false;
            if (id != "rescue_hospital" && id != "rescue_police" && id != "rescue_mall") return false;
            if (rosterIds == null) return true;
            for (int i = 0; i < rosterIds.Length; i++)
            {
                if (rosterIds[i] == id) return false;
            }
            return true;
        }

        public static void Step(float personX, float personZ, float leadX, float leadZ, float speed, float dt, out float nextX, out float nextZ)
        {
            float dx = leadX - personX;
            float dz = leadZ - personZ;
            float dist2 = dx * dx + dz * dz;
            if (dist2 <= FollowGap * FollowGap || dt <= 0f || speed <= 0f)
            {
                nextX = personX;
                nextZ = personZ;
                return;
            }
            float dist = (float)Math.Sqrt(dist2);
            if (dist > CatchUp)
            {
                nextX = leadX - dx / dist * 3f;
                nextZ = leadZ - dz / dist * 3f;
                return;
            }
            float step = speed * dt;
            float room = dist - 1.4f;
            if (step > room) step = room;
            if (step < 0f) step = 0f;
            nextX = personX + dx / dist * step;
            nextZ = personZ + dz / dist * step;
        }

        public static bool AtGate(float x, float z, float gateX, float gateZ, float radius)
        {
            float dx = x - gateX;
            float dz = z - gateZ;
            return dx * dx + dz * dz <= radius * radius;
        }
    }
}
