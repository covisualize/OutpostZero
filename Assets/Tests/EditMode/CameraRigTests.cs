using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using OutpostZero.Core;
using OutpostZero.Graphics;

namespace OutpostZero.Tests.EditMode
{
    public class CameraRigTests
    {
        private static string Read(params string[] parts)
        {
            var all = new List<string> { Directory.GetCurrentDirectory() };
            all.AddRange(parts);
            return File.ReadAllText(Path.Combine(all.ToArray())).Replace("\r\n", "\n");
        }

        [Test]
        public void TheFollowShotKeepsTheShippedOffset()
        {
            var tuning = new CameraTuning();
            CameraMath.Offset(tuning.follow.pitch, tuning.follow.distance, out float up, out float back);
            Assert.AreEqual(16f, up, 0.15f);
            Assert.AreEqual(11f, back, 0.15f);
            CameraMath.Shot(16f, 11f, out float pitch, out float distance);
            Assert.AreEqual(tuning.follow.pitch, pitch, 0.1f);
            Assert.AreEqual(tuning.follow.distance, distance, 0.1f);
            Assert.AreEqual(0.15f, tuning.leadInfluence);
            Assert.AreEqual(4.5f, tuning.leadMax);
        }

        [Test]
        public void AimAndCampShotsFollowTheIssue()
        {
            var tuning = new CameraTuning();
            Assert.AreEqual(42f, tuning.aim.Lens(55f));
            Assert.Greater(tuning.aim.pitch, tuning.follow.pitch, "a slight pitch change into ADS");
            Assert.Less(tuning.aim.pitch - tuning.follow.pitch, 8f);
            Assert.AreEqual(65f, tuning.camp.pitch);
            CameraMath.Offset(tuning.camp.pitch, tuning.camp.distance, out float campUp, out _);
            Assert.Greater(campUp, 16f, "the overview sits higher than the follow shot");
            Assert.AreEqual(55f, tuning.follow.Lens(55f), "the follow lens tracks the Field of View setting");
            Assert.AreEqual(70f, tuning.follow.Lens(70f));
        }

        [Test]
        public void PointerAndStickLeadAreCapped()
        {
            CameraMath.Lead(10f, 0f, 0.15f, 4.5f, out float x, out float z);
            Assert.AreEqual(1.5f, x, 1e-4f);
            Assert.AreEqual(0f, z, 1e-4f);
            CameraMath.Lead(100f, 100f, 0.15f, 4.5f, out x, out z);
            Assert.AreEqual(4.5f, Math.Sqrt(x * x + z * z), 1e-3);
            CameraMath.StickLead(1f, 1f, 4f, out x, out z);
            Assert.AreEqual(4f, Math.Sqrt(x * x + z * z), 1e-3);
            CameraMath.StickLead(0f, 0.5f, 4f, out x, out z);
            Assert.AreEqual(2f, z, 1e-4f);
        }

        [Test]
        public void ShakeFadesWithDistanceAndTheSetting()
        {
            Assert.AreEqual(0.4f, CameraMath.Attenuate(0.4f, 50f, 0f), "own recoil never fades");
            Assert.AreEqual(0f, CameraMath.Attenuate(1f, 24f, 24f));
            Assert.AreEqual(0.25f, CameraMath.Attenuate(1f, 12f, 24f), 1e-4f);
            Assert.AreEqual(1f, CameraMath.Attenuate(1f, 0f, 24f), 1e-4f);

            var tuning = new CameraTuning();
            float near = tuning.Force(CameraTuning.Explosion, 2f, 1f);
            float far = tuning.Force(CameraTuning.Explosion, 18f, 1f);
            Assert.Greater(near, far * 4f);
            Assert.AreEqual(0f, tuning.Force(CameraTuning.Explosion, 30f, 1f));
            Assert.AreEqual(0f, tuning.Force(CameraTuning.Shotgun, 0f, 0f), "Screen Shake at 0 silences it");
            Assert.AreEqual(tuning.Force(CameraTuning.Shotgun, 0f, 1f) * 0.5f, tuning.Force(CameraTuning.Shotgun, 0f, 0.5f), 1e-5f);
            Assert.AreEqual(0f, tuning.Force("nothing", 0f, 1f));
        }

        [Test]
        public void EveryWeaponAndEventHasAShakeRow()
        {
            var tuning = new CameraTuning();
            foreach (WeaponType type in Enum.GetValues(typeof(WeaponType)))
                Assert.IsNotNull(tuning.Shake(CameraTuning.ShotShake(type)), type.ToString());
            Assert.IsNotNull(tuning.Shake(CameraTuning.Explosion));
            Assert.IsNotNull(tuning.Shake(CameraTuning.BruteStomp));
            Assert.Greater(tuning.Shake(CameraTuning.Shotgun).force, tuning.Shake(CameraTuning.Pistol).force);
            Assert.Greater(tuning.Shake(CameraTuning.BruteStomp).radius, 0f, "brute stomps fade with distance");
            Assert.Greater(tuning.Shake(CameraTuning.Explosion).radius, tuning.Shake(CameraTuning.BruteStomp).radius);
        }

        [Test]
        public void EdgeScrollRampsInAtTheBorderOnly()
        {
            CameraMath.EdgeScroll(960f, 540f, 1920f, 1080f, 0.03f, out float x, out float z);
            Assert.AreEqual(0f, x);
            Assert.AreEqual(0f, z);
            CameraMath.EdgeScroll(0f, 540f, 1920f, 1080f, 0.03f, out x, out z);
            Assert.AreEqual(-1f, x, 1e-4f);
            CameraMath.EdgeScroll(1920f, 1080f, 1920f, 1080f, 0.03f, out x, out z);
            Assert.AreEqual(1f, x, 1e-4f);
            Assert.AreEqual(1f, z, 1e-4f, "the top of the screen scrolls north");
            CameraMath.EdgeScroll(-5f, 540f, 1920f, 1080f, 0.03f, out x, out z);
            Assert.AreEqual(0f, x, "a pointer outside the window does not scroll");
            CameraMath.EdgeScroll(1920f * 0.015f, 540f, 1920f, 1080f, 0.03f, out x, out _);
            Assert.AreEqual(-0.5f, x, 1e-3f);
        }

        [Test]
        public void WheelZoomStepsAndClamps()
        {
            var tuning = new CameraTuning();
            Assert.AreEqual(27f, CameraMath.Zoom(30f, 1f, tuning.campZoomStep, tuning.campMinDistance, tuning.campMaxDistance));
            Assert.AreEqual(33f, CameraMath.Zoom(30f, -1f, tuning.campZoomStep, tuning.campMinDistance, tuning.campMaxDistance));
            Assert.AreEqual(30f, CameraMath.Zoom(30f, 0f, tuning.campZoomStep, tuning.campMinDistance, tuning.campMaxDistance));
            Assert.AreEqual(tuning.campMinDistance, CameraMath.Zoom(19f, 1f, tuning.campZoomStep, tuning.campMinDistance, tuning.campMaxDistance));
            Assert.AreEqual(tuning.campMaxDistance, CameraMath.Zoom(43f, -1f, tuning.campZoomStep, tuning.campMinDistance, tuning.campMaxDistance));
        }

        [Test]
        public void TheConfinerStopsTheViewAtTheSkylineButKeepsTheLeaderInFrame()
        {
            var tuning = new CameraTuning();
            float open = MapRim.Open;
            float fov = 55f, aspect = 16f / 9f;
            CameraMath.Confine(open, tuning.follow.pitch, tuning.follow.distance, fov, aspect, tuning.edgeSlack, out float halfX, out float minZ, out float maxZ);
            CameraMath.Offset(tuning.follow.pitch, tuning.follow.distance, out float up, out float back);
            CameraMath.Reach(tuning.follow.pitch, up, fov, out float near, out float far);
            float halfWidth = tuning.follow.distance * (float)Math.Tan(CameraMath.HorizontalFov(fov, aspect) * 0.5f * Math.PI / 180.0);

            Assert.Less(halfX, open, "the camera stops short of the side fences");
            Assert.Less(open - halfX, halfWidth * 0.9f, "a leader at the side fence stays on screen");
            Assert.LessOrEqual(maxZ + far, open + tuning.edgeSlack + 0.01f, "the far edge stops at the skyline band");
            Assert.GreaterOrEqual(minZ + near, -open - tuning.edgeSlack - 0.01f);
            Assert.Less(open - (maxZ + back), far, "a leader at the north fence stays on screen");
            Assert.Less(minZ, maxZ);

            CameraMath.Confine(4f, tuning.follow.pitch, tuning.follow.distance, fov, aspect, 0f, out halfX, out minZ, out maxZ);
            Assert.GreaterOrEqual(halfX, 0f);
            Assert.LessOrEqual(minZ, maxZ, "a tiny yard collapses to a point instead of inverting");
        }

        [Test]
        public void BlendsEaseAndDepthOfFieldFollowsTheAdsBlend()
        {
            Assert.AreEqual(0f, CameraMath.Ease(0f, 2.5f));
            Assert.AreEqual(0.5f, CameraMath.Ease(1.25f, 2.5f), 1e-4f);
            Assert.AreEqual(1f, CameraMath.Ease(9f, 2.5f));
            Assert.AreEqual(1f, CameraMath.Ease(0f, 0f), "zero seconds is a cut");
            Assert.AreEqual(0.4f, CameraMath.Toward(0f, 1f, 0.25f, 0.1f), 1e-4f);
            Assert.AreEqual(1f, CameraMath.Toward(0.9f, 1f, 0.25f, 0.1f));
            Assert.AreEqual(0.6f, CameraMath.Toward(1f, 0f, 0.25f, 0.1f), 1e-4f);

            Assert.AreEqual(0f, PostFxRig.AimDepth(false, true, true, 1f), "the quality tier can turn it off");
            Assert.AreEqual(0.4f, PostFxRig.AimDepth(true, true, true, 0.4f), 1e-5f);
            Assert.AreEqual(1f, PostFxRig.AimDepth(true, true, false, 0f), "no rig: snaps with aiming");
            Assert.AreEqual(0f, PostFxRig.AimDepth(true, false, false, 1f));
        }

        [Test]
        public void TheCommittedProfileMatchesTheDefaults()
        {
            string asset = Read("Assets", "Resources", "CameraProfile.asset");
            string meta = Read("Assets", "Scripts", "Graphics", "CameraProfile.cs.meta");
            string guid = System.Text.RegularExpressions.Regex.Match(meta, "guid: (\\w+)").Groups[1].Value;
            StringAssert.Contains("guid: " + guid, asset);
            var tuning = new CameraTuning();
            foreach (var row in tuning.shakes)
            {
                StringAssert.Contains("- id: " + row.id + "\n      force: " + row.force.ToString(System.Globalization.CultureInfo.InvariantCulture), asset, row.id);
            }
            StringAssert.Contains("    aim:\n      pitch: 59\n      distance: 17.8\n      fov: 42", asset);
            StringAssert.Contains("    camp:\n      pitch: 65", asset);
            StringAssert.Contains("pullBackHold: 2", asset);
        }

        [Test]
        public void TheRigIsCinemachineThroughout()
        {
            string rig = Read("Assets", "Scripts", "Graphics", "ExpeditionCameraRig.cs");
            foreach (var part in new[] { "CinemachinePositionComposer", "CinemachineImpulseSource", "CinemachineImpulseListener", "CinemachineConfiner3D", "CinemachineSequencerCamera", "GameState.SuccessionScreen", "GameState.ExpeditionResults", "CombatEvents.OnBlast" })
                StringAssert.Contains(part, rig, part);
            StringAssert.Contains("CameraTuning.BruteStomp", Read("Assets", "Scripts", "AI", "ZombieAI.cs"));
            StringAssert.Contains("ExpeditionCameraRig.At(", Read("Assets", "Scripts", "Combat", "HitFeedback.cs"));
            StringAssert.Contains("guid: de94532651327ea468bba404c24039d3", Read("Assets", "Scripts", "Player", "CameraTargetDriver.cs.meta"), "the renamed driver keeps its scene references");
            Assert.IsFalse(File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts", "Player", "TopDownCameraFollow.cs")));
            foreach (var file in Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts"), "*.cs", SearchOption.AllDirectories))
                StringAssert.DoesNotContain("AddTrauma", File.ReadAllText(file), file);
        }
    }
}
