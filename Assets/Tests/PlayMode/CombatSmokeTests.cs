using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using OutpostZero.Combat;
using OutpostZero.Core;

namespace OutpostZero.Tests.PlayMode
{
    public class CombatSmokeTests
    {
        [UnityTest]
        public IEnumerator PistolRaycastDamagesAnEnemy()
        {
            var zombieObject = new GameObject("Zombie_Test");
            zombieObject.layer = GameLayers.Enemy;
            zombieObject.transform.position = new Vector3(0f, 0f, 4f);
            var capsule = zombieObject.AddComponent<CapsuleCollider>();
            capsule.height = 1.8f;
            capsule.center = new Vector3(0f, 0.9f, 0f);
            var health = zombieObject.AddComponent<HealthSystem>();
            health.Configure(65f);

            var gunObject = new GameObject("Pistol_Test");
            gunObject.transform.position = new Vector3(0f, 1.2f, 0f);
            var gun = gunObject.AddComponent<FirearmWeapon>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.displayName = "Test Pistol";
            definition.weaponType = WeaponType.Pistol;
            definition.baseDamage = 34f;
            definition.attackRate = 10f;
            definition.range = 25f;
            definition.maxMagazine = 12;
            definition.reserveAmmo = 12;
            definition.projectilesPerShot = 1;
            definition.spreadAngle = 0f;
            definition.noiseRadius = 0f;
            gun.Configure(definition);

            yield return new WaitForFixedUpdate();
            bool fired = gun.TryAttack(Vector3.forward);
            yield return null;

            Assert.IsTrue(fired);
            Assert.Less(health.CurrentHealth, 65f);

            Object.Destroy(zombieObject);
            Object.Destroy(gunObject);
            Object.Destroy(definition);
        }
    }
}
