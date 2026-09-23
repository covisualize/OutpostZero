using System.IO;
using NUnit.Framework;
using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Tests.EditMode
{
    public class AimRigTests
    {
        [Test]
        public void TheSpineTurnsHardestWhenAimingAndNotAtAllWhenDead()
        {
            Assert.AreEqual(0f, AimRig.Weight(false, false, false, true));
            Assert.AreEqual(1f, AimRig.Weight(true, false, false, true));
            Assert.Greater(AimRig.Weight(true, false, false, false), AimRig.Weight(true, false, true, false));
            Assert.Greater(AimRig.Weight(true, false, true, false), AimRig.Weight(true, true, false, false));
            float w = 0f;
            for (int i = 0; i < 60; i++) w = AimRig.Blend(w, 1f, 1f / 60f);
            Assert.Greater(w, 0.99f);
            Assert.AreEqual(0.5f, AimRig.Blend(0f, 1f, 1f / AimRig.BlendRate * 0.5f), 1e-5f);
        }

        [Test]
        public void TheLookTargetStaysInsideTheTwistLimitOnTheSameSide()
        {
            var chest = new Vector3(0f, 1.4f, 0f);
            var ahead = AimRig.Target(chest, Vector3.forward, new Vector3(1f, 0f, 5f));
            Assert.AreEqual(1.4f, ahead.y, 1e-5f, "the head looks level, not at the feet");
            Assert.Greater(ahead.x, 0f);

            var right = AimRig.Target(chest, Vector3.forward, new Vector3(5f, 0f, -1f));
            Vector3 flat = right - chest;
            Assert.Greater(flat.x, 0f, "a target behind and to the right stays on the right");
            Assert.AreEqual(AimRig.MaxTwist, Vector3.Angle(Vector3.forward, flat), 0.01f);

            var left = AimRig.Target(chest, Vector3.forward, new Vector3(-5f, 0f, -1f));
            Assert.Less((left - chest).x, 0f);
            Assert.AreEqual(AimRig.MaxTwist, Vector3.Angle(Vector3.forward, left - chest), 0.01f);

            var near = AimRig.Target(chest, Vector3.forward, new Vector3(0f, 0f, 0.3f));
            Assert.AreEqual(2f, (near - chest).magnitude, 1e-4f, "a cursor at the feet is pushed out to 2 m");
            var still = AimRig.Target(chest, Vector3.zero, chest);
            Assert.AreEqual(2f, (still - chest).magnitude, 1e-4f);
        }

        [Test]
        public void TheYawTurnsTowardTheAimAndIsScaledByWeight()
        {
            var chest = new Vector3(0f, 1.4f, 0f);
            float right = AimRig.Yaw(Vector3.forward, chest, new Vector3(3f, 0f, 3f), 1f);
            Assert.AreEqual(45f, right, 0.01f);
            Assert.AreEqual(-45f, AimRig.Yaw(Vector3.forward, chest, new Vector3(-3f, 0f, 3f), 1f), 0.01f);
            Assert.AreEqual(22.5f, AimRig.Yaw(Vector3.forward, chest, new Vector3(3f, 0f, 3f), 0.5f), 0.01f);
            Assert.AreEqual(AimRig.MaxTwist, AimRig.Yaw(Vector3.forward, chest, new Vector3(5f, 0f, -5f), 1f), 0.01f);
            Assert.AreEqual(0f, AimRig.Yaw(Vector3.forward, chest, new Vector3(3f, 0f, 3f), 0f));
        }

        [Test]
        public void TheSocketFollowsTheHandSwingButNeverFar()
        {
            var rest = new Vector3(0.28f, 1.05f, 0.45f);
            var handRest = new Vector3(0.3f, 0.9f, 0f);
            Assert.AreEqual(rest, AimRig.Socket(rest, handRest, handRest, AimRig.HandFollow));
            var swung = AimRig.Socket(rest, handRest, handRest + new Vector3(0f, 0f, 0.1f), 1f);
            Assert.AreEqual(0.55f, swung.z, 1e-5f);
            var flung = AimRig.Socket(rest, handRest, handRest + new Vector3(0f, -2f, 0f), 1f);
            Assert.AreEqual(AimRig.MaxDrift, (flung - rest).magnitude, 1e-5f);
        }

        [Test]
        public void TheReloadClipIsTimedToTheReloadAndItsEventOnlyFinishesLate()
        {
            Assert.AreEqual(1f, AimRig.ReloadSpeed(1.8f, 1.8f), 1e-5f);
            Assert.AreEqual(0.5f, AimRig.ReloadSpeed(1f, 2f), 1e-5f, "a slow, wounded reload plays the clip slower");
            Assert.AreEqual(1f, AimRig.ReloadSpeed(0f, 2f));
            Assert.AreEqual(4f, AimRig.ReloadSpeed(10f, 0.5f));
            Assert.IsFalse(AimRig.AcceptReloadEvent(0.5f));
            Assert.IsTrue(AimRig.AcceptReloadEvent(AimRig.ReloadEarliest));

            Assert.AreEqual(2, AimRig.EventsFor("Walk", out string step).Length);
            Assert.AreEqual(AimRig.Footstep, step);
            var reload = AimRig.EventsFor("Reload", out string done);
            Assert.AreEqual(AimRig.ReloadDone, done);
            Assert.GreaterOrEqual(reload[0], AimRig.ReloadEarliest);
            Assert.IsEmpty(AimRig.EventsFor("Idle", out _));
            var bite = AimRig.EventsFor("AttackB", out string impact);
            Assert.AreEqual(AimRig.AttackImpact, impact);
            Assert.Less(bite[0], SwingClock.LateHit);
            Assert.AreEqual("Zombie_Walker", ClipEvents.Bare("Armature|Zombie_Walker"));
        }

        [Test]
        public void EachCharacterModelGetsItsOwnController()
        {
            Assert.AreEqual("Zombie_Brute", CharacterRig.ModelId(ModelPaths.ZombieBrute));
            Assert.AreEqual("Zombie_Runner", CharacterRig.ModelId("Assets\\Models\\Zombie_Runner.fbx"));
            Assert.AreEqual("", CharacterRig.ModelId(null));
            Assert.AreEqual("SurvivorLocomotion", CharacterRig.ResourcePath(CharacterRig.ModelId(ModelPaths.SurvivorLeader)));
            Assert.AreEqual("Animators/Zombie_Walker", CharacterRig.ResourcePath("Zombie_Walker"));
            Assert.AreEqual("Assets/Resources/Animators/Zombie_Walker.controller", CharacterRig.ControllerAsset("Zombie_Walker"));
            Assert.AreEqual("Assets/Resources/SurvivorLocomotion.controller", CharacterRig.ControllerAsset(CharacterRig.PlayerModel));
            Assert.AreEqual("", CharacterRig.ResourcePath(""));
        }

        [Test]
        public void ASwingBitesExactlyOnceWhetherTheClipOrTheClockLandsIt()
        {
            Assert.IsTrue(SwingClock.Bites(0.5f, 0.65f, false, false));
            Assert.IsFalse(SwingClock.Bites(0.5f, 0.65f, true, false));
            Assert.IsFalse(SwingClock.Bites(0.5f, 0.65f, false, true), "a clip-driven swing waits for the contact frame");
            Assert.IsTrue(SwingClock.Bites(0.9f, 1f, false, true), "the clock still lands a swing whose event never fired");
            Assert.IsFalse(SwingClock.Bites(0.9f, 1f, true, true));
            Assert.IsTrue(SwingClock.Hears(0.4f, false));
            Assert.IsFalse(SwingClock.Hears(0.4f, true));
            Assert.IsFalse(SwingClock.Hears(-1f, false), "no bite between swings");

            int bites = 0;
            bool bitten = false;
            float swing = 0f;
            while (swing < 1f)
            {
                float before = swing;
                swing = SwingClock.Advance(swing, 1f / 60f, 1.2f);
                if (before < AimRig.ImpactAt && swing >= AimRig.ImpactAt && SwingClock.Hears(swing, bitten)) { bitten = true; bites++; }
                if (SwingClock.Bites(before, swing, bitten, true)) { bitten = true; bites++; }
            }
            Assert.AreEqual(1, bites);
        }

        [Test]
        public void TheCommittedControllerTimesTheReload()
        {
            string text = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Resources", "SurvivorLocomotion.controller"));
            StringAssert.Contains("- m_Name: ReloadSpeed\n    m_Type: 1\n    m_DefaultFloat: 1", text.Replace("\r\n", "\n"));
            int reload = text.IndexOf("  m_Name: Reload\n", System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(reload, 0);
            int end = text.IndexOf("--- !u!", reload, System.StringComparison.Ordinal);
            string state = text.Substring(reload, end - reload);
            StringAssert.Contains("m_SpeedParameterActive: 1", state);
            StringAssert.Contains("m_SpeedParameter: ReloadSpeed", state);
        }
    }
}
