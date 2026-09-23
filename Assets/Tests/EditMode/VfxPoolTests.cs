using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using OutpostZero.Combat;
using OutpostZero.Core;

namespace OutpostZero.Tests.EditMode
{
    public class VfxPoolTests
    {
        private static string Root => Directory.GetCurrentDirectory();

        private static string Read(params string[] parts)
        {
            var all = new List<string> { Root };
            all.AddRange(parts);
            return File.ReadAllText(Path.Combine(all.ToArray())).Replace("\r\n", "\n");
        }

        [Test]
        public void EachWeaponTypeFiresItsOwnMuzzleEvent()
        {
            Assert.AreEqual(VfxEvent.MuzzlePistol, VfxBook.MuzzleFor(WeaponType.Pistol));
            Assert.AreEqual(VfxEvent.MuzzleShotgun, VfxBook.MuzzleFor(WeaponType.Shotgun));
            Assert.AreEqual(VfxEvent.MuzzleRifle, VfxBook.MuzzleFor(WeaponType.Rifle));
            Assert.AreEqual(VfxEvent.MuzzleRifle, VfxBook.MuzzleFor(WeaponType.SMG));
            Assert.AreEqual(VfxEvent.None, VfxBook.MuzzleFor(WeaponType.Melee));
        }

        [Test]
        public void EverySurfaceFamilyThrowsADistinctImpact()
        {
            var seen = new HashSet<VfxEvent>();
            foreach (var face in new[] { "flesh", "metal", "wood", "concrete" }) Assert.IsTrue(seen.Add(VfxBook.ImpactFor(face)), face);
            Assert.AreEqual(VfxEvent.BloodSpray, VfxBook.ImpactFor("flesh"));
            Assert.AreEqual(VfxEvent.ImpactSpark, VfxBook.ImpactFor("metal"));
            Assert.AreEqual(VfxEvent.ImpactSplinter, VfxBook.ImpactFor("wood"));
            Assert.AreEqual(VfxEvent.ImpactDust, VfxBook.ImpactFor("glass"));
        }

        [Test]
        public void EachHazardKindBurstsDifferently()
        {
            Assert.AreEqual(VfxEvent.ExplosionFlash, CombatVfx.BlastFor(HazardKind.Explosive));
            Assert.AreEqual(VfxEvent.ToxicCloud, CombatVfx.BlastFor(HazardKind.Toxic));
            Assert.AreEqual(VfxEvent.OilFire, CombatVfx.BlastFor(HazardKind.Oil));
        }

        [Test]
        public void EveryEventHasAPositiveLifeAndKeep()
        {
            foreach (VfxEvent id in Enum.GetValues(typeof(VfxEvent)))
            {
                if (id == VfxEvent.None) continue;
                Assert.Greater(VfxBook.Life(id), 0f, id.ToString());
                Assert.Greater(VfxBook.Keep(id), 0, id.ToString());
            }
            Assert.AreEqual(0.05f, VfxBook.Life(VfxEvent.Tracer), 0.0001f, "a tracer is a one-frame streak");
            Assert.Greater(VfxBook.Keep(VfxEvent.Shell), VfxBook.Keep(VfxEvent.Fireball), "busy effects keep more idle");
        }

        [Test]
        public void ThePoolKeepsUpToItsCapThenDrops()
        {
            Assert.IsTrue(VfxBook.Keeps(0, 4));
            Assert.IsTrue(VfxBook.Keeps(3, 4));
            Assert.IsFalse(VfxBook.Keeps(4, 4));
            Assert.IsTrue(VfxBook.Keeps(0, 0), "a zero cap still keeps one");
            Assert.IsFalse(VfxBook.Keeps(1, 0));
        }

        [Test]
        public void StatsBalanceSoALeakShowsAsLiveClimbing()
        {
            VfxStats.Reset();
            VfxStats.Make();
            VfxStats.Make();
            Assert.AreEqual(2, VfxStats.Live);
            VfxStats.Park();
            VfxStats.Park();
            Assert.AreEqual(0, VfxStats.Live);
            Assert.AreEqual(2, VfxStats.Idle);
            VfxStats.Reuse();
            Assert.AreEqual(1, VfxStats.Live);
            Assert.AreEqual(1, VfxStats.Idle);
            Assert.AreEqual(33, VfxStats.ReusePercent());
            VfxStats.Drop();
            Assert.AreEqual(0, VfxStats.Live);
            Assert.AreEqual(1, VfxStats.Dropped);
            VfxStats.Park();
            VfxStats.Park();
            Assert.AreEqual(0, VfxStats.Live, "parking more than rented never goes negative");
            VfxStats.Forget();
            VfxStats.Forget();
            VfxStats.Forget();
            VfxStats.Forget();
            Assert.AreEqual(0, VfxStats.Idle);
            VfxStats.Reset();
            Assert.AreEqual(0, VfxStats.ReusePercent());
        }

        [Test]
        public void LibraryFindReturnsTheMatchingEntry()
        {
            var list = new[]
            {
                new VfxLibrary.Entry { id = VfxEvent.Tracer, keep = 3 },
                null,
                new VfxLibrary.Entry { id = VfxEvent.Flies, life = 2f },
            };
            Assert.AreEqual(3, VfxLibrary.FindIn(list, VfxEvent.Tracer).keep);
            Assert.AreEqual(2f, VfxLibrary.FindIn(list, VfxEvent.Flies).life);
            Assert.IsNull(VfxLibrary.FindIn(list, VfxEvent.Shell));
            Assert.IsNull(VfxLibrary.FindIn(null, VfxEvent.Shell));
        }

        [Test]
        public void TheCommittedLibraryListsEveryEventOnceAndBindsTheScript()
        {
            string asset = Read("Assets", "Resources", "VfxLibrary.asset");
            string script = Read("Assets", "Scripts", "Core", "VfxLibrary.cs.meta");
            string guid = Regex.Match(script, @"guid: (\w+)").Groups[1].Value;
            StringAssert.Contains("guid: " + guid + ", type: 3", asset);
            var ids = new List<int>();
            foreach (Match m in Regex.Matches(asset, @"- id: (\d+)")) ids.Add(int.Parse(m.Groups[1].Value));
            foreach (VfxEvent id in Enum.GetValues(typeof(VfxEvent)))
            {
                if (id == VfxEvent.None) continue;
                Assert.AreEqual(1, ids.FindAll(x => x == (int)id).Count, id.ToString());
            }
            Assert.AreEqual(Enum.GetValues(typeof(VfxEvent)).Length - 1, ids.Count);
        }

        [Test]
        public void WeaponAndZombieAssetsNameTheirEffects()
        {
            var weapons = new Dictionary<string, VfxEvent>
            {
                { "Pistol_9mm", VfxEvent.MuzzlePistol },
                { "Shotgun_Pump", VfxEvent.MuzzleShotgun },
                { "Rifle_Assault", VfxEvent.MuzzleRifle },
                { "Machete", VfxEvent.None },
            };
            foreach (var pair in weapons)
            {
                string text = Read("Assets", "Data", "Weapons", pair.Key + ".asset");
                StringAssert.Contains("muzzleVfx: " + (int)pair.Value + "\n", text, pair.Key);
            }
            foreach (var zombie in new[] { "Walker", "Runner", "Brute" })
            {
                string text = Read("Assets", "Data", "Zombies", zombie + ".asset");
                StringAssert.Contains("hitVfx: " + (int)VfxEvent.BloodSpray + "\n", text, zombie);
                StringAssert.Contains("deathVfx: " + (int)VfxEvent.DeathBurst + "\n", text, zombie);
            }
        }

        [Test]
        public void CombatVfxRentsFromThePoolInsteadOfDestroying()
        {
            string code = Read("Assets", "Scripts", "Combat", "CombatVfx.cs");
            Assert.IsFalse(Regex.IsMatch(code, @"Object\.Destroy\([a-z]+\s*,"), "no timed Destroy: effects return to the pool");
            Assert.IsFalse(code.Contains(".material.color"), "tints go through a property block, not a cloned material");
            StringAssert.Contains("VfxPool.Rent(", code);
            StringAssert.Contains("emission.enabled = false", code);
        }

        [Test]
        public void TheWatchLineShowsPoolStats()
        {
            VfxLedger.Reset();
            VfxStats.Reset();
            VfxStats.Make();
            VfxStats.Park();
            VfxStats.Reuse();
            StringAssert.EndsWith("pooled 0  reused 50%", VfxLedger.Line());
            StringAssert.EndsWith("en reserva 0  reusados 50%", VfxLedger.Line("es"));
            VfxStats.Reset();
        }
    }
}
