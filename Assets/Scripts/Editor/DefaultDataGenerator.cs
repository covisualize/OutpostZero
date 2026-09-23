#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Core;
using OutpostZero.Items;

namespace OutpostZero.EditorTools
{
    public static class DefaultDataGenerator
    {
        public const string WeaponsDir = "Assets/Data/Weapons";
        public const string ZombiesDir = "Assets/Data/Zombies";

        [MenuItem("Tools/Outpost Zero/Generate Default Data", false, 2)]
        public static void Generate()
        {
            Directory.CreateDirectory(WeaponsDir);
            Directory.CreateDirectory(ZombiesDir);

            SaveWeapon("Pistol_9mm", weapon =>
            {
                weapon.id = "pistol_9mm";
                weapon.displayName = "Tactical 9mm Pistol";
                weapon.weaponType = WeaponType.Pistol;
                weapon.baseDamage = 34f;
                weapon.attackRate = 3.2f;
                weapon.range = 25f;
                weapon.spreadAngle = 2.5f;
                weapon.projectilesPerShot = 1;
                weapon.maxMagazine = 12;
                weapon.reserveAmmo = 60;
                weapon.reloadDuration = 1.8f;
                weapon.noiseRadius = 20f;
                weapon.noiseType = NoiseType.GunshotQuiet;
                weapon.modelPath = ModelPaths.Pistol;
            });

            SaveWeapon("Shotgun_Pump", weapon =>
            {
                weapon.id = "shotgun_pump";
                weapon.displayName = "Remington 870 Shotgun";
                weapon.weaponType = WeaponType.Shotgun;
                weapon.baseDamage = 19f;
                weapon.attackRate = 1.1f;
                weapon.range = 16f;
                weapon.spreadAngle = 8.5f;
                weapon.projectilesPerShot = 7;
                weapon.maxMagazine = 6;
                weapon.reserveAmmo = 24;
                weapon.reloadDuration = 2.4f;
                weapon.noiseRadius = 38f;
                weapon.noiseType = NoiseType.GunshotLoud;
                weapon.useProjectile = true;
                weapon.modelPath = ModelPaths.Shotgun;
                weapon.holdOffset = new Vector3(0f, 0f, 0.1f);
            });

            SaveWeapon("Machete", weapon =>
            {
                weapon.id = "machete";
                weapon.displayName = "Steel Machete";
                weapon.weaponType = WeaponType.Melee;
                weapon.baseDamage = 48f;
                weapon.attackRate = 1.8f;
                weapon.range = 1.9f;
                weapon.projectilesPerShot = 1;
                weapon.noiseRadius = 2f;
                weapon.noiseType = NoiseType.MeleeSwing;
                weapon.isMelee = true;
                weapon.modelPath = ModelPaths.Machete;
                weapon.holdOffset = new Vector3(0f, 0f, 0.15f);
            });

            SaveWeapon("Rifle_Assault", weapon =>
            {
                weapon.id = "rifle_assault";
                weapon.displayName = "Assault Rifle";
                weapon.weaponType = WeaponType.Rifle;
                weapon.baseDamage = 26f;
                weapon.attackRate = 9f;
                weapon.range = 32f;
                weapon.spreadAngle = 3f;
                weapon.projectilesPerShot = 1;
                weapon.maxMagazine = 30;
                weapon.reserveAmmo = 90;
                weapon.reloadDuration = 2.1f;
                weapon.noiseRadius = 34f;
                weapon.noiseType = NoiseType.GunshotLoud;
                weapon.automatic = true;
                weapon.useProjectile = true;
                weapon.modelPath = ModelPaths.AssaultRifle;
            });

            SaveZombie("Walker", zombie =>
            {
                zombie.id = "walker";
                zombie.displayName = "Walker";
                zombie.modelPath = ModelPaths.ZombieWalker;
                zombie.chaseSpeed = 3.8f;
                zombie.maxHealth = 65f;
                zombie.attackDamage = 18f;
                zombie.attackCooldown = 1.4f;
                zombie.specialAbility = ZombieSpecialAbility.None;
                zombie.lootTable = LootTables.Walker;
            });

            SaveZombie("Runner", zombie =>
            {
                zombie.id = "runner";
                zombie.displayName = "Runner";
                zombie.modelPath = ModelPaths.ZombieRunner;
                zombie.wanderSpeed = 2.4f;
                zombie.chaseSpeed = 5.6f;
                zombie.maxHealth = 45f;
                zombie.attackDamage = 22f;
                zombie.attackCooldown = 1.0f;
                zombie.sightRange = 16f;
                zombie.specialAbility = ZombieSpecialAbility.Lunge;
                zombie.lootTable = LootTables.Runner;
            });

            SaveZombie("Brute", zombie =>
            {
                zombie.id = "brute";
                zombie.displayName = "Brute";
                zombie.modelPath = ModelPaths.ZombieBrute;
                zombie.wanderSpeed = 1.2f;
                zombie.chaseSpeed = 2.6f;
                zombie.maxHealth = 180f;
                zombie.attackDamage = 38f;
                zombie.attackCooldown = 2.0f;
                zombie.attackRange = 2.1f;
                zombie.sightRange = 12f;
                zombie.specialAbility = ZombieSpecialAbility.Charge;
                zombie.lootTable = LootTables.Brute;
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            foreach (var problem in ItemDatabaseSync.Sync()) Debug.LogWarning("[DefaultDataGenerator] " + problem);
            RecipeBookSync.Sync();
            ModuleBookSync.Sync();
            FactionBookSync.Sync();
            TraitBookSync.Sync();
            TutorialBookSync.Sync();
            ExpeditionBookSync.Sync();
            SyncWeaponSet();
        }

        public static WeaponDefinition LoadWeapon(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"{WeaponsDir}/{assetName}.asset");
        }

        public static ZombieArchetype LoadZombie(string assetName)
        {
            return AssetDatabase.LoadAssetAtPath<ZombieArchetype>($"{ZombiesDir}/{assetName}.asset");
        }

        private static void SaveWeapon(string assetName, System.Action<WeaponDefinition> fill)
        {
            string path = $"{WeaponsDir}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<WeaponDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }
            fill(asset);
            asset.name = assetName;
            if (asset.muzzleVfx == VfxEvent.None) asset.muzzleVfx = VfxBook.MuzzleFor(asset.weaponType);
            if (string.IsNullOrEmpty(asset.fireSfx) && asset.weaponType != WeaponType.Melee) asset.fireSfx = OutpostZero.Shell.ClipBook.Fire(asset.weaponType);
            if (asset.heldPrefab == null && !string.IsNullOrEmpty(asset.modelPath))
            {
                asset.heldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Weapons/" + PrefabCatalog.Id(asset.modelPath) + ".prefab");
            }
            EditorUtility.SetDirty(asset);
        }

        /// <summary>Lists every weapon definition in Resources/WeaponSet so runtime weapons find their model.</summary>
        public static void SyncWeaponSet()
        {
            const string path = "Assets/Resources/" + WeaponSet.ResourcePath + ".asset";
            var set = AssetDatabase.LoadAssetAtPath<WeaponSet>(path);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<WeaponSet>();
                AssetDatabase.CreateAsset(set, path);
            }
            var found = new System.Collections.Generic.List<WeaponDefinition>();
            foreach (string guid in AssetDatabase.FindAssets("t:WeaponDefinition", new[] { WeaponsDir }))
            {
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (weapon != null) found.Add(weapon);
            }
            set.weapons = found.ToArray();
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
        }

        private static void SaveZombie(string assetName, System.Action<ZombieArchetype> fill)
        {
            string path = $"{ZombiesDir}/{assetName}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ZombieArchetype>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ZombieArchetype>();
                AssetDatabase.CreateAsset(asset, path);
            }
            fill(asset);
            asset.name = assetName;
            EditorUtility.SetDirty(asset);
        }
    }
}
#endif
