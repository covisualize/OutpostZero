using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using OutpostZero.Core;
using OutpostZero.EditorTools;

namespace OutpostZero.Tests.EditMode
{
    public class PlayabilityFoundationTests
    {
        [Test]
        public void WeaponHitMaskHitsEnemiesAndWallsButNotThePlayer()
        {
            int mask = GameLayers.WeaponHitMask;
            Assert.AreNotEqual(0, mask & GameLayers.EnemyMask);
            Assert.AreNotEqual(0, mask & GameLayers.EnvironmentMask);
            Assert.AreEqual(0, mask & GameLayers.PlayerMask);
            Assert.AreEqual(0, mask & GameLayers.LootMask);
            Assert.AreNotEqual(0, mask);
        }

        [Test]
        public void VisionMaskIsEnvironmentOnly()
        {
            Assert.AreEqual(GameLayers.EnvironmentMask, (int)GameLayers.VisionOcclusionMask);
        }

        [Test]
        public void ZeroMaskResolvesToFallback()
        {
            LayerMask resolved = GameLayers.Resolve(default, GameLayers.WeaponHitMask);
            Assert.AreEqual((int)GameLayers.WeaponHitMask, (int)resolved);
        }

        [Test]
        public void EveryCanonicalModelExists()
        {
            foreach (string path in ModelPaths.All)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsNotNull(model, $"Missing model artifact: {path}");
            }
        }

        [Test]
        public void StreetLampUsesThePropPrefix()
        {
            StringAssert.Contains("Prop_StreetLamp.fbx", ModelPaths.StreetLamp);
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/Props/StreetLamp.fbx"));
        }

        [Test]
        public void DefaultDefinitionsMatchPrototypeTuning()
        {
            DefaultDataGenerator.Generate();

            var pistol = DefaultDataGenerator.LoadWeapon("Pistol_9mm");
            var shotgun = DefaultDataGenerator.LoadWeapon("Shotgun_Pump");
            var machete = DefaultDataGenerator.LoadWeapon("Machete");
            var walker = DefaultDataGenerator.LoadZombie("Walker");
            var runner = DefaultDataGenerator.LoadZombie("Runner");
            var brute = DefaultDataGenerator.LoadZombie("Brute");

            Assert.IsNotNull(pistol);
            Assert.AreEqual(34f, pistol.baseDamage);
            Assert.AreEqual(12, pistol.maxMagazine);
            Assert.AreEqual(60, pistol.reserveAmmo);
            Assert.AreEqual(7, shotgun.projectilesPerShot);
            Assert.AreEqual(8.5f, shotgun.spreadAngle);
            Assert.IsTrue(machete.isMelee);
            Assert.Greater(machete.baseDamage, 0f);

            Assert.AreEqual(65f, walker.maxHealth);
            Assert.AreEqual(5.6f, runner.chaseSpeed);
            Assert.AreEqual(180f, brute.maxHealth);
            Assert.AreEqual(ZombieSpecialAbility.Charge, brute.specialAbility);

            foreach (var weapon in new[] { pistol, shotgun, machete })
            {
                Assert.IsFalse(string.IsNullOrEmpty(weapon.modelPath));
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(weapon.modelPath), weapon.modelPath);
            }
        }
    }
}
