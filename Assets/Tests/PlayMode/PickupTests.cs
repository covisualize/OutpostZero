using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using OutpostZero.Combat;
using OutpostZero.Core;
using OutpostZero.Items;
using OutpostZero.Player;

namespace OutpostZero.Tests.PlayMode
{
    public class PickupTests
    {
        static FirearmWeapon Pistol(Transform parent, out WeaponDefinition definition)
        {
            var gunObject = new GameObject("Pistol_Test");
            gunObject.transform.SetParent(parent, false);
            var gun = gunObject.AddComponent<FirearmWeapon>();
            definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.displayName = "Test Pistol";
            definition.weaponType = WeaponType.Pistol;
            definition.baseDamage = 34f;
            definition.attackRate = 3f;
            definition.range = 25f;
            definition.maxMagazine = 12;
            definition.reserveAmmo = 12;
            definition.projectilesPerShot = 1;
            gun.Configure(definition);
            return gun;
        }

        [UnityTest]
        public IEnumerator PickingUpRoundsFeedsTheMatchingGun()
        {
            var leader = new GameObject("Leader_Test");
            var inventory = leader.AddComponent<PlayerInventory>();
            var gun = Pistol(leader.transform, out var definition);
            yield return null;

            int before = gun.ReserveAmmo;
            var drop = ItemDatabase.SpawnWorld("ammo_9mm", 1, new Vector3(0f, 0.2f, 1f), Quaternion.identity);
            var item = drop.GetComponent<WorldItem>();
            Assert.IsTrue(item.CanInteract(inventory));
            item.Interact(inventory);
            yield return null;

            Assert.AreEqual(before + ItemCatalog.Find("ammo_9mm").AmmoAmount, gun.ReserveAmmo);
            Assert.IsTrue(drop == null);

            Object.Destroy(leader);
            Object.Destroy(definition);
        }

        [UnityTest]
        public IEnumerator AFullPackRefusesAHeavyPickup()
        {
            var leader = new GameObject("Leader_Test");
            var inventory = leader.AddComponent<PlayerInventory>();
            yield return null;

            Assert.IsTrue(inventory.TryAddItem("cloth", "Cloth", ItemCategory.ScrapMaterial, 1, inventory.MaxWeightCapacity - 0.1f));
            float carried = inventory.CurrentWeight;
            var drop = ItemDatabase.SpawnWorld("water", 5, new Vector3(0f, 0.2f, 1f), Quaternion.identity);
            drop.GetComponent<WorldItem>().Interact(inventory);
            yield return null;

            Assert.IsTrue(drop != null);
            Assert.AreEqual(carried, inventory.CurrentWeight, 0.001f);
            Assert.GreaterOrEqual(inventory.WeightRatio, 0.99f);

            Object.Destroy(drop);
            Object.Destroy(leader);
        }
    }
}
