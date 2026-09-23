using OutpostZero.Core;

namespace OutpostZero.AI
{
    /// <summary>
    /// One archetype's special: when it fires, how long it braces and dashes, how fast, and what a hit does.
    /// The zombie drives the shared brace, dash and cooldown clock and asks its ability for the numbers.
    /// </summary>
    public abstract class SpecialAbility
    {
        public abstract ZombieSpecialAbility Kind { get; }
        public abstract float Near { get; }
        public abstract float Far { get; }
        public abstract float Windup { get; }
        public abstract float DashSeconds { get; }
        public abstract float Speed { get; }
        /// <summary>A charge carries on into boards, bars and glass, and stuns itself on a solid wall.</summary>
        public virtual bool BreaksThrough => false;
        /// <summary>Seconds the target lies on the ground after a dash connects, or 0.</summary>
        public virtual float Knockdown => 0f;
        /// <summary>A dash that connects shakes the camera with a stomp.</summary>
        public virtual bool Stomps => false;

        public abstract float Damage(float attackDamage);

        public bool InReach(float distance) => distance >= Near && distance <= Far;

        public SpecialBeat.Clock Advance(SpecialBeat.Clock clock, float distance, float now, float dt, float cooldown)
        {
            return SpecialBeat.Advance(clock, InReach(distance), now, dt, Windup, DashSeconds, cooldown);
        }

        private static readonly LungeAbility Lunge = new LungeAbility();
        private static readonly ChargeAbility Charge = new ChargeAbility();

        /// <summary>The shared strategy for an archetype's special, or null for a plain walker.</summary>
        public static SpecialAbility For(ZombieSpecialAbility kind)
        {
            switch (kind)
            {
                case ZombieSpecialAbility.Lunge: return Lunge;
                case ZombieSpecialAbility.Charge: return Charge;
                default: return null;
            }
        }
    }

    /// <summary>Runner: from 3 to 5 m, a 0.4 s brace, then a short 8 m/s dash for 30 damage. A side-step makes it miss.</summary>
    public sealed class LungeAbility : SpecialAbility
    {
        public override ZombieSpecialAbility Kind => ZombieSpecialAbility.Lunge;
        public override float Near => SpecialBeat.LungeNear;
        public override float Far => SpecialBeat.LungeFar;
        public override float Windup => SpecialBeat.Windup;
        public override float DashSeconds => SpecialBeat.Dash;
        public override float Speed => SpecialBeat.LungeSpeed;
        public override float Damage(float attackDamage) => SpecialBeat.LungeDamage;
    }

    /// <summary>Brute: lines up from 6 to 12 m, roars for 0.8 s, charges through barricades and knocks the leader down.</summary>
    public sealed class ChargeAbility : SpecialAbility
    {
        public const float ExtraDamage = 8f;

        public override ZombieSpecialAbility Kind => ZombieSpecialAbility.Charge;
        public override float Near => SpecialBeat.ChargeNear;
        public override float Far => SpecialBeat.ChargeFar;
        public override float Windup => SpecialBeat.ChargeWindup;
        public override float DashSeconds => SpecialBeat.ChargeDash;
        public override float Speed => SpecialBeat.ChargeSpeed;
        public override bool BreaksThrough => true;
        public override float Knockdown => SpecialBeat.ChargeKnockdown;
        public override bool Stomps => true;
        public override float Damage(float attackDamage) => attackDamage + ExtraDamage;
    }
}
