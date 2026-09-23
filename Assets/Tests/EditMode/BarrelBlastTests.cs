using System.IO;
using NUnit.Framework;
using OutpostZero.Combat;

namespace OutpostZero.Tests.EditMode
{
    /// <summary>PRO-26: the red barrel's falloff, noise and staggered chain.</summary>
    public class BarrelBlastTests
    {
        private static string Read(params string[] parts)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), Path.Combine(parts))).Replace("\r\n", "\n");
        }

        [Test]
        public void DamageFallsFromOneTwentyToTwentyAcrossSixMetres()
        {
            Assert.AreEqual(120f, BarrelBlast.Damage(0f), 0.001f);
            Assert.AreEqual(70f, BarrelBlast.Damage(3f), 0.001f);
            Assert.AreEqual(20f, BarrelBlast.Damage(6f), 0.001f);
            Assert.AreEqual(0f, BarrelBlast.Damage(6.01f), 0.001f);
            Assert.AreEqual(120f, BarrelBlast.Damage(-1f), 0.001f);
            Assert.AreEqual(45f, BarrelBlast.Noise, 0.001f);
            Assert.AreEqual(6f, BarrelBlast.Radius, 0.001f);
        }

        [Test]
        public void ThreeWalkersBesideTheBarrelDie()
        {
            string walker = Read("Assets", "Data", "Zombies", "Walker.asset");
            var m = System.Text.RegularExpressions.Regex.Match(walker, "maxHealth: ([0-9.]+)");
            float health = float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
            foreach (float distance in new[] { 1f, 2f, 3f })
                Assert.GreaterOrEqual(BarrelBlast.Damage(distance), health, "a walker " + distance + " m off survives");
        }

        [Test]
        public void ChainDelaysStayInsideTheWindowAndVary()
        {
            float low = 1f, high = 0f;
            for (int id = -500; id < 500; id++)
            {
                float delay = BarrelBlast.ChainDelay(id);
                Assert.GreaterOrEqual(delay, BarrelBlast.ChainMin);
                Assert.LessOrEqual(delay, BarrelBlast.ChainMax);
                if (delay < low) low = delay;
                if (delay > high) high = delay;
            }
            Assert.Less(low, 0.18f);
            Assert.Greater(high, 0.37f);
            Assert.AreEqual(BarrelBlast.ChainDelay(42), BarrelBlast.ChainDelay(42));
        }

        [Test]
        public void TheBarrelUsesTheBlastNumbersAndPrimesItsNeighbours()
        {
            string hazard = Read("Assets", "Scripts", "Combat", "DestructibleHazard.cs");
            StringAssert.Contains("BarrelBlast.Damage(", hazard);
            StringAssert.Contains("NoiseTable.Radius(kind == HazardKind.Explosive ? NoiseTable.BarrelBlast", hazard);
            Assert.AreEqual(BarrelBlast.Noise, OutpostZero.Sensory.NoiseTable.Radius(OutpostZero.Sensory.NoiseTable.BarrelBlast));
            StringAssert.Contains("hazard.Prime(BarrelBlast.ChainDelay(", hazard);
            StringAssert.Contains("NoiseType.Explosion", hazard);
            StringAssert.DoesNotContain("hazard.TakeHit(damage)", hazard, "a neighbour waits for its own delay");
            StringAssert.Contains("type == NoiseType.GunshotLoud || type == NoiseType.Explosion", Read("Assets", "Scripts", "AI", "ZombieSpawner.cs"), "the blast calls reinforcements");
        }
    }
}
