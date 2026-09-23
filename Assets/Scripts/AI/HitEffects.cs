using OutpostZero.Core;

namespace OutpostZero.AI
{
    /// <summary>
    /// What a zombie's landed hit leaves when its archetype names nothing: every bite can infect,
    /// a runner's claw can open a bleed, and a brute's blow knocks the target down.
    /// </summary>
    public static class HitEffects
    {
        public const float InfectSeconds = 8f;
        public const float BleedSeconds = 5f;
        public const float BlowKnockdown = 0.7f;

        public static HitEffect[] Default(ZombieSpecialAbility ability)
        {
            var infect = new HitEffect(StatusKind.Infected, ClawCut.BiteInfect, InfectSeconds);
            if (ability == ZombieSpecialAbility.Lunge)
                return new[] { new HitEffect(StatusKind.Bleeding, ClawCut.RunnerBleed, BleedSeconds), infect };
            if (ability == ZombieSpecialAbility.Charge)
                return new[] { infect, new HitEffect(StatusKind.KnockedDown, 1f, BlowKnockdown) };
            return new[] { infect };
        }

        public static HitEffect[] For(HitEffect[] authored, ZombieSpecialAbility ability)
        {
            return authored != null && authored.Length > 0 ? authored : Default(ability);
        }
    }
}
