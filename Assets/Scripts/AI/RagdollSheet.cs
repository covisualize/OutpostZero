namespace OutpostZero.AI
{
    /// <summary>
    /// The limbs a blast-thrown body breaks into, over the shared humanoid bones. Each limb is a
    /// capsule toward <see cref="Limb.Toward"/> (a sphere when there is none) jointed to its parent.
    /// </summary>
    public static class RagdollSheet
    {
        public struct Limb
        {
            public string Bone;
            public string Parent;
            public string Toward;
            public float Radius;
            public float Mass;
            public float Swing;

            public Limb(string bone, string parent, string toward, float radius, float mass, float swing)
            {
                Bone = bone;
                Parent = parent;
                Toward = toward;
                Radius = radius;
                Mass = mass;
                Swing = swing;
            }
        }

        public const float BodyMass = 70f;
        public const float HeavyMass = 120f;
        public const float Twist = 25f;

        public static readonly Limb[] Limbs =
        {
            new Limb("Hips", null, "Spine", 0.16f, 0.20f, 0f),
            new Limb("Spine", "Hips", "Head", 0.15f, 0.22f, 30f),
            new Limb("Head", "Spine", null, 0.12f, 0.06f, 40f),
            new Limb("LeftUpperArm", "Spine", "LeftLowerArm", 0.06f, 0.05f, 80f),
            new Limb("LeftLowerArm", "LeftUpperArm", "LeftHand", 0.05f, 0.04f, 90f),
            new Limb("RightUpperArm", "Spine", "RightLowerArm", 0.06f, 0.05f, 80f),
            new Limb("RightLowerArm", "RightUpperArm", "RightHand", 0.05f, 0.04f, 90f),
            new Limb("LeftUpperLeg", "Hips", "LeftLowerLeg", 0.08f, 0.10f, 60f),
            new Limb("LeftLowerLeg", "LeftUpperLeg", "LeftFoot", 0.065f, 0.07f, 80f),
            new Limb("RightUpperLeg", "Hips", "RightLowerLeg", 0.08f, 0.10f, 60f),
            new Limb("RightLowerLeg", "RightUpperLeg", "RightFoot", 0.065f, 0.07f, 80f),
        };

        public static float MassOf(bool heavy, float scale)
        {
            if (scale <= 0.01f) scale = 1f;
            return (heavy ? HeavyMass : BodyMass) * scale * scale * scale;
        }

        /// <summary>Capsule axis (0 x, 1 y, 2 z) for a limb whose child sits at the given local offset.</summary>
        public static int Axis(float x, float y, float z)
        {
            float ax = x < 0f ? -x : x;
            float ay = y < 0f ? -y : y;
            float az = z < 0f ? -z : z;
            if (ax >= ay && ax >= az) return 0;
            return ay >= az ? 1 : 2;
        }
    }
}
