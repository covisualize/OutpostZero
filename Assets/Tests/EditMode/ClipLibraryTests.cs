using System.IO;
using NUnit.Framework;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Tests.EditMode
{
    public class ClipLibraryTests
    {
        private static string Read(string relative)
        {
            return File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), relative)).Replace("\r\n", "\n");
        }

        [Test]
        public void EachGunFamilyPlaysItsOwnReload()
        {
            Assert.AreEqual("Reload", CharacterRig.ReloadClip(WeaponType.Pistol));
            Assert.AreEqual("Reload", CharacterRig.ReloadClip(WeaponType.SMG));
            Assert.AreEqual("ReloadShotgun", CharacterRig.ReloadClip(WeaponType.Shotgun));
            Assert.AreEqual("ReloadRifle", CharacterRig.ReloadClip(WeaponType.Rifle));
            Assert.AreEqual(0f, CharacterRig.ReloadBlend(WeaponType.Pistol));
            Assert.AreEqual(0.5f, CharacterRig.ReloadBlend(WeaponType.Shotgun));
            Assert.AreEqual(1f, CharacterRig.ReloadBlend(WeaponType.Rifle));
            foreach (string take in CharacterRig.ReloadTakes)
            {
                AimRig.EventsFor(take, out string done);
                Assert.AreEqual(AimRig.ReloadDone, done, take + " finishes the reload");
            }
        }

        [Test]
        public void TheWanderHasTwoTakesOnOneStride()
        {
            CollectionAssert.AreEqual(new[] { "Walk", "WalkB", "Shamble" }, CrowdVariant.WalkTakes);
            Assert.AreEqual(StrideSheet.GroundSpeed("Zombie_Walker", "Walk"), StrideSheet.GroundSpeed("Zombie_Walker", "WalkB"));
            Assert.AreEqual(2, AimRig.EventsFor("WalkB", out string step).Length);
            Assert.AreEqual(AimRig.Footstep, step);
        }

        [Test]
        public void TheGraphBlendsReloadsAndMeltsZombieCorpses()
        {
            string builder = Read("Assets/Scripts/Editor/SurvivorAnimatorBuilder.cs");
            StringAssert.Contains("Variants(controller, upper, \"Reload\", CharacterRig.ReloadVariant, byName, CharacterRig.ReloadTakes)", builder);
            StringAssert.Contains("Variants(controller, machine, \"Walk\", CharacterRig.GaitVariant, byName, CrowdVariant.WalkTakes)", builder);
            StringAssert.Contains("EnsureParameter(controller, CharacterRig.ReloadVariant, AnimatorControllerParameterType.Float)", builder);
            StringAssert.Contains("if (byName.TryGetValue(CharacterRig.Dissolve, out var heap)) Melt(machine, heap);", builder);
            StringAssert.Contains("ExitTo(death, melt);", builder);
            StringAssert.Contains("case \"WalkB\":", builder);

            string body = Read("Assets/Scripts/Player/SurvivorLocomotion.cs");
            StringAssert.Contains("animator.SetFloat(CharacterRig.ReloadVariant, CharacterRig.ReloadBlend(gun.Type));", body);
            StringAssert.Contains("AimRig.ReloadSpeed(ReloadClipLength(gun.Type), gun.ReloadSeconds)", body);
        }

        [Test]
        public void ALongStunReelsAndAShortOneFlinches()
        {
            Assert.IsTrue(HitStun.Staggers(HitStun.Seconds(WeaponType.Shotgun)));
            Assert.IsFalse(HitStun.Staggers(HitStun.Seconds(WeaponType.Pistol)));
            Assert.IsFalse(HitStun.Staggers(HitStun.Seconds(WeaponType.Melee)));
            Assert.IsFalse(HitStun.Staggers(HitStun.Resist(HitStun.Seconds(WeaponType.Shotgun), true)), "a brute shrugs off the blast");
            Assert.AreEqual(OutpostZero.AI.SpecialBeat.WallStun, HitStun.Taken(OutpostZero.AI.SpecialBeat.WallStun, true, false), 1e-5f, "a brute's own wall crash is not resisted");
            Assert.AreEqual(0.18f, HitStun.Taken(HitStun.Seconds(WeaponType.Shotgun), true, true), 1e-5f);
            Assert.IsTrue(HitStun.Staggers(HitStun.Taken(OutpostZero.AI.SpecialBeat.WallStun, true, false)), "a brute that charges a wall reels");
            StringAssert.Contains("ApplyImpulse(-dash, 0.4f, SpecialBeat.WallStun, false);", Read("Assets/Scripts/AI/ZombieAI.cs"));
            CollectionAssert.Contains(CharacterRig.UpperClears, CharacterRig.Stagger);

            string builder = Read("Assets/Scripts/Editor/SurvivorAnimatorBuilder.cs");
            StringAssert.Contains("if (byName.TryGetValue(CharacterRig.Stagger, out var reel)) Reel(machine, reel);", builder);
            StringAssert.Contains("EnsureParameter(controller, CharacterRig.Stagger, AnimatorControllerParameterType.Trigger)", builder);
            string motion = Read("Assets/Scripts/AI/ZombieMotion.cs");
            StringAssert.Contains("animator.SetTrigger(Reel());", motion);
            StringAssert.Contains("HitStun.Staggers(brain.StunSeconds)", motion);
        }

        [Test]
        public void TheCharacterFilesCarryTheNewTakes()
        {
            string rig = Read("BlenderScripts/character_rig.py");
            foreach (string take in new[] { "\"ReloadShotgun\": reload_shotgun", "\"ReloadRifle\": reload_rifle", "\"WalkB\": _plus(", "\"Dissolve\": _dissolve()" })
                StringAssert.Contains(take, rig);
        }
    }
}
