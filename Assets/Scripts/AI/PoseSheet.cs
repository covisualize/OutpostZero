namespace OutpostZero.AI
{
    /// <summary>
    /// How a body reads without an animator clip.
    /// A chase leans. An attack swings. A hit folds back. A death falls.
    /// </summary>
    public static class PoseSheet
    {
        public const float ChaseLean = 14f;
        public const float AttackLean = 38f;
        public const float HitLean = -22f;
        public const float DeathLean = 88f;
        public const float WalkHop = 0.05f;
        public const float AttackHop = 0.12f;
        public const float Drop = 0.55f;
        public const float FallTime = 0.6f;
        public const float ChaseSpeed = 4.2f;
        public const float WalkSpeed = 1.1f;

        public static bool Moves(ZombieAI.ZombieState state)
        {
            return state == ZombieAI.ZombieState.Chase
                || state == ZombieAI.ZombieState.Wander
                || state == ZombieAI.ZombieState.Searching
                || state == ZombieAI.ZombieState.InvestigateNoise;
        }

        public static float Speed(ZombieAI.ZombieState state)
        {
            if (state == ZombieAI.ZombieState.Chase) return ChaseSpeed;
            if (Moves(state)) return WalkSpeed;
            return 0f;
        }

        public static bool Sprint(ZombieAI.ZombieState state)
        {
            return state == ZombieAI.ZombieState.Chase;
        }

        public static float Swing(float age)
        {
            if (age < 0f) age = 0f;
            float t = age % 0.7f;
            if (t < 0.2f) return t / 0.2f;
            if (t < 0.45f) return 1f - (t - 0.2f) / 0.25f;
            return 0f;
        }

        public static float Fall(float age)
        {
            if (age < 0f) age = 0f;
            if (age >= FallTime) return 1f;
            return age / FallTime;
        }

        public static float Lean(ZombieAI.ZombieState state, float age)
        {
            if (state == ZombieAI.ZombieState.Attack) return AttackLean * Swing(age);
            if (state == ZombieAI.ZombieState.Stunned) return HitLean;
            if (state == ZombieAI.ZombieState.Dead) return DeathLean * Fall(age);
            if (state == ZombieAI.ZombieState.Chase) return ChaseLean;
            return 0f;
        }

        public static float Hop(ZombieAI.ZombieState state, float phase)
        {
            float wave = UnityEngine.Mathf.Sin(phase);
            if (state == ZombieAI.ZombieState.Attack) return wave * AttackHop;
            if (Moves(state)) return wave * WalkHop;
            return 0f;
        }

        public static float Sink(ZombieAI.ZombieState state, float age)
        {
            if (state != ZombieAI.ZombieState.Dead) return 0f;
            return -Drop * Fall(age);
        }
    }
}
