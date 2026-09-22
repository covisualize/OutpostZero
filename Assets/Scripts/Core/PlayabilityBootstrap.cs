using UnityEngine;
using UnityEngine.SceneManagement;
using OutpostZero.AI;
using OutpostZero.Combat;
using OutpostZero.Player;

namespace OutpostZero.Core
{
    /// <summary>
    /// Makes a scene that was saved before the layer/loot pass playable:
    /// assigns physics layers and attaches pickups to the known prototype loot props.
    /// </summary>
    public static class PlayabilityBootstrap
    {
        public static void Apply(Scene scene)
        {
            if (!scene.IsValid()) return;

            foreach (var root in scene.GetRootGameObjects())
            {
                AssignLayers(root, -1);
                AttachKnownLoot(root);
                AttachBoards(root);
            }

            var spawners = Object.FindObjectsByType<ZombieSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var spawner in spawners)
            {
                if (spawner.GetComponent<ZombiePool>() == null)
                {
                    spawner.gameObject.AddComponent<ZombiePool>();
                }
            }
        }

        private static void AssignLayers(GameObject go, int inherited)
        {
            int classified = Classify(go.name);
            int applied = classified >= 0 ? classified : inherited;
            if (applied >= 0)
            {
                go.layer = applied;
            }

            int next = inherited;
            if (classified == GameLayers.Player || classified == GameLayers.Enemy)
            {
                next = classified;
            }
            else if (classified >= 0)
            {
                next = classified;
            }

            foreach (Transform child in go.transform)
            {
                AssignLayers(child.gameObject, next);
            }
        }

        private static int Classify(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;

            if (name.Contains("Player") || name.Contains("Survivor_Leader") || name.Contains("Survivor_Body"))
                return GameLayers.Player;
            if (name.StartsWith("Zombie"))
                return GameLayers.Enemy;
            if (name.StartsWith("Supply_") || name.StartsWith("Salvage_Scrap") || name.StartsWith("Loot_"))
                return GameLayers.Loot;
            if (name.Contains("Ground") || name.Contains("ENVIRONMENT") || name.StartsWith("Road_")
                || name.StartsWith("Building") || name.StartsWith("Ruin") || name.StartsWith("Barricade")
                || name.StartsWith("Prop_") || name.StartsWith("Vehicle") || name.StartsWith("Base_")
                || name.StartsWith("Sanctuary") || name.StartsWith("Jersey") || name.StartsWith("Wood")
                || name.Contains("Dumpster") || name.Contains("Barrel") || name.Contains("Crate")
                || name.Contains("Sedan") || name.Contains("Truck") || name.Contains("Street")
                || name.Contains("Watchtower") || name.Contains("Campfire") || name.Contains("Workbench")
                || name.Contains("Generator") || name.Contains("WaterCollector") || name.Contains("MedicalCot"))
                return GameLayers.Environment;

            return -1;
        }

        private static void AttachBoards(GameObject go)
        {
            if (IsWoodBoard(go.name) && go.GetComponent<StreetBoard>() == null)
            {
                go.AddComponent<StreetBoard>();
            }

            foreach (Transform child in go.transform)
            {
                AttachBoards(child.gameObject);
            }
        }

        private static bool IsWoodBoard(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Contains("Jersey")) return false;
            return name.Contains("WoodWire") || name.Contains("Barricade_Wood") || name.Contains("Sandbag") || name.Contains("Wood_Gate");
        }

        private static void AttachKnownLoot(GameObject go)
        {
            if (go.GetComponent<LootPickup>() == null && TryMatchLoot(go.name, out var kind, out var amount))
            {
                var pickup = go.AddComponent<LootPickup>();
                pickup.Configure(kind, amount);
            }

            foreach (Transform child in go.transform)
            {
                AttachKnownLoot(child.gameObject);
            }
        }

        private static bool TryMatchLoot(string name, out LootKind kind, out int amount)
        {
            kind = LootKind.Scrap;
            amount = 1;
            if (name.Contains("Medkit"))
            {
                kind = LootKind.Medkit;
                amount = 1;
                return true;
            }
            if (name.Contains("9mm") && (name.StartsWith("Supply_") || name.StartsWith("Loot_")))
            {
                kind = LootKind.Ammo9mm;
                amount = 24;
                return true;
            }
            if (name.Contains("ShotgunAmmo") || name.Contains("AmmoBox_Shotgun"))
            {
                kind = LootKind.AmmoShotgun;
                amount = 12;
                return true;
            }
            if (name.Contains("Scrap") && (name.StartsWith("Salvage_") || name.StartsWith("Loot_")))
            {
                kind = LootKind.Scrap;
                amount = 8;
                return true;
            }
            return false;
        }
    }
}
