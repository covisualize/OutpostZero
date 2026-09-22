namespace OutpostZero.Player
{
    /// <summary>
    /// How the leader reads without an animator clip.
    /// A walk still hops 0.04. A crouch still scales to 0.72.
    /// A shot leans in, a reload dips, a hit folds back, and a death falls.
    /// </summary>
    public static class GaitSheet
    {
        public const float WalkHop = 0.04f;
        public const float SprintHop = 0.07f;
        public const float CrouchHop = 0.02f;
        public const float CrouchScale = 0.72f;
        public const float SprintLean = 12f;
        public const float CrouchLean = 6f;
        public const float AttackLean = 26f;
        public const float HitLean = -18f;
        public const float ReloadLean = 10f;
        public const float DeathLean = 76f;
        public const float ReloadDip = 0.06f;
        public const float Drop = 0.45f;
        public const float FallTime = 0.7f;
        public const float SwingTime = 0.34f;
        public const float HitTime = 0.28f;

        public enum Beat
        {
            Idle,
            Walk,
            Crouch,
            CrouchStill,
            Sprint,
            Attack,
            Hit,
            Reload,
            Dead
        }

        public static Beat Pick(bool dead, bool hit, bool attack, bool reload, bool crouch, bool sprint, float speed)
        {
            if (dead) return Beat.Dead;
            if (hit) return Beat.Hit;
            if (attack) return Beat.Attack;
            if (reload) return Beat.Reload;
            if (crouch && speed > 0.2f) return Beat.Crouch;
            if (crouch) return Beat.CrouchStill;
            if (sprint) return Beat.Sprint;
            if (speed > 0.2f) return Beat.Walk;
            return Beat.Idle;
        }

        public static float Scale(bool crouching, bool dead)
        {
            if (dead) return 1f;
            if (crouching) return CrouchScale;
            return 1f;
        }

        public static float Swing(float age)
        {
            if (age < 0f || age >= SwingTime) return 0f;
            if (age < 0.12f) return age / 0.12f;
            return 1f - (age - 0.12f) / 0.22f;
        }

        public static float Flail(float age)
        {
            if (age < 0f || age >= HitTime) return 0f;
            if (age < 0.08f) return age / 0.08f;
            return 1f - (age - 0.08f) / 0.2f;
        }

        public static float Dip(float age)
        {
            if (age < 0f) age = 0f;
            float t = age % 0.8f;
            if (t < 0.4f) return t / 0.4f;
            return 1f - (t - 0.4f) / 0.4f;
        }

        public static float Fall(float age)
        {
            if (age < 0f) age = 0f;
            if (age >= FallTime) return 1f;
            return age / FallTime;
        }

        public static float Lean(Beat beat, float age)
        {
            if (beat == Beat.Dead) return DeathLean * Fall(age);
            if (beat == Beat.Hit) return HitLean * Flail(age);
            if (beat == Beat.Attack) return AttackLean * Swing(age);
            if (beat == Beat.Reload) return ReloadLean * Dip(age);
            if (beat == Beat.Sprint) return SprintLean;
            if (beat == Beat.Crouch) return CrouchLean;
            return 0f;
        }

        public static float Hop(Beat beat, float phase)
        {
            float wave = UnityEngine.Mathf.Sin(phase);
            if (beat == Beat.Sprint) return wave * SprintHop;
            if (beat == Beat.Walk) return wave * WalkHop;
            if (beat == Beat.Crouch) return wave * CrouchHop;
            return 0f;
        }

        public static float Sink(Beat beat, float age)
        {
            if (beat == Beat.Dead) return -Drop * Fall(age);
            if (beat == Beat.Reload) return -ReloadDip * Dip(age);
            return 0f;
        }
    }
}
