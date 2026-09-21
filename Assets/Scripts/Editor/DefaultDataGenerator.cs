#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using OutpostZero.Core;

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
                weapon.modelPath = ModelPaths.Shotgun;
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
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
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
            EditorUtility.SetDirty(asset);
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
