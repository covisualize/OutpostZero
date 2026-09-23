using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Player;

namespace OutpostZero.Tests.EditMode
{
    public class CharacterMotionTests
    {
        private static string Root => Directory.GetCurrentDirectory();

        private static string Read(params string[] parts)
        {
            var all = new List<string> { Root };
            all.AddRange(parts);
            return File.ReadAllText(Path.Combine(all.ToArray())).Replace("\r\n", "\n");
        }

        [Test]
        public void GaitsPlayAtTheBodysSpeedSoThePlantedFootHolds()
        {
            float walk = StrideSheet.GroundSpeed(CharacterRig.PlayerModel, "Walk");
            float rate = StrideSheet.Rate(4.5f, walk);
            Assert.Greater(rate, 1f, "the leader walks faster than the clip's own stride");
            Assert.AreEqual(0f, StrideSheet.Slide(4.5f, walk, rate), 1e-4f);
            float sprint = StrideSheet.GroundSpeed(CharacterRig.PlayerModel, "Sprint");
            Assert.AreEqual(0f, StrideSheet.Slide(7.5f, sprint, StrideSheet.Rate(7.5f, sprint)), 1e-4f);
            float crouch = StrideSheet.GroundSpeed(CharacterRig.PlayerModel, "CrouchWalk");
            Assert.AreEqual(0f, StrideSheet.Slide(2.2f, crouch, StrideSheet.Rate(2.2f, crouch)), 1e-4f);

            Assert.AreEqual(1f, StrideSheet.Rate(0f, walk), "standing still leaves the idle alone");
            Assert.AreEqual(StrideSheet.Fastest, StrideSheet.Rate(50f, walk));
            Assert.AreEqual(StrideSheet.Rate(3f, walk) * 0.5f, StrideSheet.Rate(3f, walk, 2f), 1e-4f, "a bigger body takes longer strides");
            Assert.AreEqual(1f, StrideSheet.Rate(3f, StrideSheet.GroundSpeed("Zombie_Runner", "Charge")), "a clip with no gait plays as authored");

            float zombieWalk = StrideSheet.GroundSpeed("Zombie_Walker", "Walk");
            float zombieRun = StrideSheet.GroundSpeed("Zombie_Walker", "Sprint");
            Assert.IsTrue(StrideSheet.Sprints(3.8f, zombieWalk, zombieRun), "a walker chasing at 3.8 m/s runs");
            Assert.IsFalse(StrideSheet.Sprints(1.2f, zombieWalk, zombieRun), "a wander stays a walk");
            Assert.Greater(StrideSheet.GroundSpeed("Zombie_Brute", "Charge"), zombieRun);
            Assert.AreEqual(1f, StrideSheet.SpeedParam(2.6f, 2.6f), 1e-5f);
            Assert.AreEqual(0f, StrideSheet.SpeedParam(1f, 0f));
        }

        [Test]
        public void ACrowdSplitsIntoThreeLooksAndNeverBlendsTwoGaits()
        {
            var counts = new int[CrowdVariant.Count];
            for (int seed = 0; seed < 600; seed++) counts[CrowdVariant.Pick(seed * 7919 + 13)]++;
            for (int i = 0; i < counts.Length; i++) Assert.Greater(counts[i], 140, "variant " + i);
            Assert.AreEqual(CrowdVariant.Pick(42), CrowdVariant.Pick(42));
            for (int v = 0; v < CrowdVariant.Count; v++)
            {
                float walk = CrowdVariant.Walk(v);
                Assert.IsTrue(walk == 0f || walk == 1f, "gait blend " + walk);
                Assert.AreEqual(walk == 1f ? "Shamble" : "Walk", CrowdVariant.WalkClip(v));
            }
            Assert.AreNotEqual(CrowdVariant.Idle(0), CrowdVariant.Idle(1));
            Assert.AreEqual(0f, CrowdVariant.Threshold(0, 3));
            Assert.AreEqual(0.5f, CrowdVariant.Threshold(1, 3));
            Assert.AreEqual(1f, CrowdVariant.Threshold(2, 3));
            var offsets = new HashSet<float>();
            for (int seed = 0; seed < 20; seed++)
            {
                float offset = CrowdVariant.CycleOffset(seed);
                Assert.That(offset, Is.InRange(0f, 1f));
                offsets.Add(offset);
            }
            Assert.Greater(offsets.Count, 15, "neighbours start their loops apart");
            var deaths = new HashSet<float>();
            for (int seed = 0; seed < 60; seed++) deaths.Add(CrowdVariant.Death(seed));
            Assert.AreEqual(3, deaths.Count);
        }

        [Test]
        public void TheCommittedControllerPlaysGaitsAtTheStrideRate()
        {
            string text = Read("Assets", "Resources", "SurvivorLocomotion.controller");
            StringAssert.Contains("- m_Name: " + StrideSheet.MoveRate + "\n    m_Type: 1\n    m_DefaultFloat: 1", text);
            foreach (string state in new[] { "Walk", "Sprint", "CrouchWalk" })
            {
                string block = State(text, state);
                StringAssert.Contains("m_SpeedParameterActive: 1", block, state);
                StringAssert.Contains("m_SpeedParameter: " + StrideSheet.MoveRate, block, state);
            }
            StringAssert.Contains("m_SpeedParameterActive: 0", State(text, "Idle"));
        }

        private static string State(string text, string name)
        {
            foreach (string doc in text.Split(new[] { "--- !u!" }, System.StringSplitOptions.None))
                if (doc.StartsWith("1102 ") && doc.Contains("\n  m_Name: " + name + "\n")) return doc;
            Assert.Fail("no state " + name);
            return "";
        }

        [Test]
        public void ABlastThrowsItsDeadAwayAndUpAndOnlyInsideItsScope()
        {
            Assert.IsFalse(BlastKill.Active);
            BlastKill.Begin(Vector3.zero, 9f, 4f);
            BlastKill.Begin(new Vector3(1f, 0f, 0f), 10f, 4.5f);
            Assert.IsTrue(BlastKill.Active);
            Assert.AreEqual(10f, BlastKill.Force);
            BlastKill.End();
            Assert.IsTrue(BlastKill.Active, "the outer blast is still going off");
            BlastKill.End();
            Assert.IsFalse(BlastKill.Active);
            BlastKill.End();
            Assert.IsFalse(BlastKill.Active, "an extra End does not underflow");

            Vector3 near = BlastKill.Push(new Vector3(1f, 0f, 0f), Vector3.zero, 9f, 4f);
            Vector3 far = BlastKill.Push(new Vector3(3.5f, 0f, 0f), Vector3.zero, 9f, 4f);
            Assert.Greater(near.x, 0f);
            Assert.Greater(near.y, 0f);
            Assert.Greater(near.magnitude, far.magnitude);
            Assert.Greater(far.magnitude, 0f);
            Assert.LessOrEqual(BlastKill.Push(Vector3.forward, Vector3.zero, 500f, 4f).magnitude, BlastKill.MaxPush + 1e-4f);
            Assert.Greater(BlastKill.Push(Vector3.zero, Vector3.zero, 9f, 4f).magnitude, 0f, "a body on the charge still flies");
        }

        [Test]
        public void TheRagdollCoversTheRigAndWeighsOneBody()
        {
            string rig = Read("BlenderScripts", "character_rig.py");
            var bones = new HashSet<string>();
            foreach (Match m in Regex.Matches(rig, "\\(\"(\\w+)\", (?:None|\"\\w+\"), \\(")) bones.Add(m.Groups[1].Value);
            Assert.GreaterOrEqual(bones.Count, 15);

            var seen = new HashSet<string>();
            float mass = 0f;
            foreach (var limb in RagdollSheet.Limbs)
            {
                Assert.IsTrue(bones.Contains(limb.Bone), limb.Bone + " is not a rig bone");
                if (limb.Toward != null) Assert.IsTrue(bones.Contains(limb.Toward), limb.Toward);
                if (limb.Parent != null) Assert.IsTrue(seen.Contains(limb.Parent), limb.Bone + " comes before its parent");
                Assert.Greater(limb.Radius, 0f);
                seen.Add(limb.Bone);
                mass += limb.Mass;
            }
            Assert.AreEqual(1f, mass, 1e-4f);
            Assert.IsNull(RagdollSheet.Limbs[0].Parent, "the hips are the root");
            Assert.Greater(RagdollSheet.MassOf(true, 1f), RagdollSheet.MassOf(false, 1f));
            Assert.AreEqual(RagdollSheet.BodyMass * 8f, RagdollSheet.MassOf(false, 2f), 1e-3f);
            Assert.AreEqual(1, RagdollSheet.Axis(0.01f, 0.4f, -0.02f));
            Assert.AreEqual(0, RagdollSheet.Axis(-0.3f, 0.1f, 0f));
            Assert.AreEqual(2, RagdollSheet.Axis(0f, 0.1f, 0.3f));
        }

        [Test]
        public void CorpsesHitTheWorldButNotTheLivingLootOrBullets()
        {
            string tags = Read("ProjectSettings", "TagManager.asset");
            var layers = Regex.Match(tags, "  layers:\n((?:  - [^\n]*\n)+)").Groups[1].Value.Split('\n');
            Assert.AreEqual("  - " + GameLayers.CorpseName, layers[GameLayers.Corpse]);
            Assert.AreEqual("  - " + GameLayers.EnemyName, layers[GameLayers.Enemy]);

            string dynamics = Read("ProjectSettings", "DynamicsManager.asset");
            string hex = Regex.Match(dynamics, "m_LayerCollisionMatrix: (\\w+)").Groups[1].Value;
            Assert.AreEqual(256, hex.Length);
            bool Collides(int a, int b)
            {
                string word = hex.Substring(a * 8, 8);
                uint row = 0;
                for (int i = 3; i >= 0; i--) row = (row << 8) | System.Convert.ToUInt32(word.Substring(i * 2, 2), 16);
                return (row & (1u << b)) != 0;
            }
            int corpse = GameLayers.Corpse;
            Assert.IsTrue(Collides(corpse, GameLayers.Environment));
            Assert.IsTrue(Collides(corpse, 0));
            Assert.IsTrue(Collides(corpse, corpse));
            foreach (int other in new[] { GameLayers.Player, GameLayers.Enemy, GameLayers.Loot, GameLayers.Projectile, GameLayers.Interactable })
            {
                Assert.IsFalse(Collides(corpse, other), "corpse row, layer " + other);
                Assert.IsFalse(Collides(other, corpse), "layer " + other + " row");
            }
            Assert.IsFalse(Collides(GameLayers.Player, GameLayers.Projectile), "the old rules stay");
            Assert.IsTrue(Collides(GameLayers.Player, GameLayers.Enemy));
        }

        [Test]
        public void FootfallNoiseKeepsItsPaceWhenTheClipDrivesIt()
        {
            Assert.IsTrue(StepGate.EventSpeaks(1.0f, 1.0f));
            Assert.IsTrue(StepGate.EventSpeaks(0.95f, 1.0f), "a footfall just before the beat lands it");
            Assert.IsFalse(StepGate.EventSpeaks(0.5f, 1.0f), "a fast cadence doesn't multiply the noise");
            Assert.IsTrue(StepGate.TimerOwnsNoise(5f, -1f, 0.45f), "no clip has spoken");
            Assert.IsFalse(StepGate.TimerOwnsNoise(5f, 4.6f, 0.45f), "the clip is stepping");
            Assert.IsTrue(StepGate.TimerOwnsNoise(5f, 4.2f, 0.45f), "the clip went quiet");

            int noises = 0;
            float next = 0f;
            const float interval = 0.45f;
            for (float t = 0f; t < 3f; t += 0.158f)
            {
                if (!StepGate.EventSpeaks(t, next)) continue;
                noises++;
                next = t + interval;
            }
            Assert.That(noises, Is.InRange(6, 8), "about one noise per designed interval over 3 s");
        }

        [Test]
        public void EveryWeaponAssetHoldsItsPrefabAndTheSetListsThemAll()
        {
            string weapons = Path.Combine(Root, "Assets", "Data", "Weapons");
            string set = Read("Assets", "Resources", "WeaponSet.asset");
            StringAssert.Contains(Guid(Path.Combine(Root, "Assets", "Scripts", "Core", "WeaponSet.cs.meta")), set);
            int count = 0;
            foreach (string asset in Directory.GetFiles(weapons, "*.asset"))
            {
                count++;
                string text = File.ReadAllText(asset).Replace("\r\n", "\n");
                StringAssert.Contains(Guid(asset + ".meta"), set, Path.GetFileName(asset) + " is missing from WeaponSet");
                string model = Regex.Match(text, "modelPath: (\\S+)").Groups[1].Value;
                string prefab = Path.Combine(Root, "Assets", "Prefabs", "Weapons", Path.GetFileNameWithoutExtension(model) + ".prefab");
                Assert.IsTrue(File.Exists(prefab), prefab);
                var held = Regex.Match(text, "heldPrefab: \\{fileID: (\\d+), guid: (\\w+), type: 3\\}");
                Assert.IsTrue(held.Success, Path.GetFileName(asset) + " has no held prefab");
                Assert.AreEqual(Guid(prefab + ".meta"), held.Groups[2].Value, asset);
                StringAssert.Contains("--- !u!1 &" + held.Groups[1].Value + " stripped", File.ReadAllText(prefab), "held prefab root");
                StringAssert.Contains("holdEuler: {x: 0, y: 90, z: 0}", text);
            }
            Assert.AreEqual(4, count);
            Assert.AreEqual(count, Regex.Matches(set, "fileID: 11400000").Count);
        }

        private static string Guid(string meta)
        {
            return Regex.Match(File.ReadAllText(meta), "guid: (\\w+)").Groups[1].Value;
        }
    }
}
