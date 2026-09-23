using System.IO;
using NUnit.Framework;
using OutpostZero.AI;
using OutpostZero.Core;

namespace OutpostZero.Tests.EditMode
{
    public class SpecialAbilityTests
    {
        [Test]
        public void EachArchetypeGetsItsOwnStrategy()
        {
            Assert.IsNull(SpecialAbility.For(ZombieSpecialAbility.None));
            Assert.IsInstanceOf<LungeAbility>(SpecialAbility.For(ZombieSpecialAbility.Lunge));
            Assert.IsInstanceOf<ChargeAbility>(SpecialAbility.For(ZombieSpecialAbility.Charge));
            Assert.AreSame(SpecialAbility.For(ZombieSpecialAbility.Charge), SpecialAbility.For(ZombieSpecialAbility.Charge), "the strategies hold no state and are shared");
        }

        [Test]
        public void TheLungeAndChargeCarryTheIssueNumbers()
        {
            var lunge = SpecialAbility.For(ZombieSpecialAbility.Lunge);
            Assert.IsTrue(lunge.InReach(3f));
            Assert.IsTrue(lunge.InReach(5f));
            Assert.IsFalse(lunge.InReach(5.1f));
            Assert.AreEqual(0.4f, lunge.Windup);
            Assert.AreEqual(8f, lunge.Speed);
            Assert.AreEqual(30f, lunge.Damage(12f));
            Assert.AreEqual(0f, lunge.Knockdown);
            Assert.IsFalse(lunge.BreaksThrough);

            var charge = SpecialAbility.For(ZombieSpecialAbility.Charge);
            Assert.IsFalse(charge.InReach(5.9f));
            Assert.IsTrue(charge.InReach(12f));
            Assert.AreEqual(0.8f, charge.Windup);
            Assert.AreEqual(6.5f, charge.Speed);
            Assert.AreEqual(1.2f, charge.Knockdown);
            Assert.IsTrue(charge.BreaksThrough);
            Assert.GreaterOrEqual(charge.Speed * charge.DashSeconds, charge.Far, "the charge covers its whole line-up");
            Assert.AreEqual(20f + ChargeAbility.ExtraDamage, charge.Damage(20f));
        }

        [Test]
        public void TheStrategyClockMatchesTheBeatClock()
        {
            foreach (var kind in new[] { ZombieSpecialAbility.Lunge, ZombieSpecialAbility.Charge })
            {
                var special = SpecialAbility.For(kind);
                bool charge = kind == ZombieSpecialAbility.Charge;
                var a = new SpecialBeat.Clock();
                var b = new SpecialBeat.Clock();
                float now = 0f;
                float distance = charge ? 9f : 4f;
                for (int i = 0; i < 600; i++)
                {
                    a = special.Advance(a, distance, now, 1f / 60f, 4f);
                    b = SpecialBeat.Advance(b, SpecialBeat.InReach(distance, charge), now, 1f / 60f, charge, 4f);
                    Assert.AreEqual(b.Phase, a.Phase, kind + " frame " + i);
                    Assert.AreEqual(b.Left, a.Left, 1e-5f);
                    Assert.AreEqual(b.Ready, a.Ready, 1e-5f);
                    now += 1f / 60f;
                }
            }
        }

        [Test]
        public void TheZombieDrivesItsSpecialThroughTheStrategy()
        {
            string brain = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets/Scripts/AI/ZombieAI.cs")).Replace("\r\n", "\n");
            StringAssert.Contains("var special = SpecialAbility.For(specialAbility);", brain);
            StringAssert.Contains("special.Advance(abilityClock, distToTarget, Time.time, Time.deltaTime, AbilityCooldown)", brain);
            StringAssert.Contains("if (special.BreaksThrough)", brain);
            StringAssert.Contains("damageable.TakeDamage(special.Damage(attackDamage)", brain);
            StringAssert.DoesNotContain("SpecialBeat.Speed(charging)", brain);
        }
    }
}
