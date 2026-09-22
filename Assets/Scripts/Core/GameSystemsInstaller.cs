using UnityEngine;
using UnityEngine.SceneManagement;
using OutpostZero.AI;
using OutpostZero.Colony;
using OutpostZero.Combat;
using OutpostZero.Expedition;
using OutpostZero.Graphics;
using OutpostZero.Items;
using OutpostZero.Player;
using OutpostZero.Shell;
using OutpostZero.UI;

namespace OutpostZero.Core
{
    /// <summary>
    /// Attaches the expedition, colony, and shell systems to whatever scene is loaded.
    /// The prototype arena was saved before these components existed, so Play still grows them.
    /// </summary>
    public static class GameSystemsInstaller
    {
        public static void Install(Scene scene)
        {
            if (!scene.IsValid()) return;
            var host = Object.FindFirstObjectByType<GameManager>();
            if (host == null) return;

            Add<SettingsService>(host.gameObject);
            Add<AudioManager>(host.gameObject);
            Add<SaveSystem>(host.gameObject);
            Add<RunArchive>(host.gameObject);
            Add<SurvivorRoster>(host.gameObject);
            Add<WorldClock>(host.gameObject);
            Add<ColonyStorage>(host.gameObject);
            Add<GridBuilder>(host.gameObject);
            Add<CraftingBench>(host.gameObject);
            Add<NightRaidController>(host.gameObject);
            Add<FactionTrade>(host.gameObject);
            Add<ObjectiveTracker>(host.gameObject);
            Add<DayNightCycle>(host.gameObject);
            Add<WeatherController>(host.gameObject);
            Add<PostFxRig>(host.gameObject);
            Add<UrpMaterialPass>(host.gameObject);
            Add<PerfBudget>(host.gameObject);
            Add<LodGovernor>(host.gameObject);
            Add<WorldMapService>(host.gameObject);
            Add<TutorialDirector>(host.gameObject);
            Add<CodexDirector>(host.gameObject);
            Add<OutpostZero.UI.SceneFlow>(host.gameObject);
            Add<DevPanel>(host.gameObject);
            Add<CampServices>(host.gameObject);
            Add<CampPopulation>(host.gameObject);
            Add<DistrictDressing>(host.gameObject);
            StreetDetail.RaiseHome();
            MapRim.Raise();

            foreach (var root in scene.GetRootGameObjects())
            {
                Walk(root);
            }

            var player = Object.FindFirstObjectByType<PlayerController>();
            if (player != null) OutfitPlayer(player);

            var camera = Camera.main;
            if (camera != null)
            {
                Add<HitFeedback>(camera.gameObject);
                Add<ImpactDecalPool>(camera.gameObject);
                Add<GameShellUI>(camera.gameObject);
                Add<OutpostInterface>(camera.gameObject);
                Add<ExpeditionCameraRig>(camera.gameObject);
            }

            ExtractionZone.Create(new Vector3(-5.5f, 0.5f, -10f));

            if (host.GetComponent<GameManager>().CurrentState == GameState.ExpeditionActive)
            {
                WorldMapService.Instance?.ApplyOpening();
            }
        }

        private static void OutfitPlayer(PlayerController player)
        {
            Add<PlayerInteractor>(player.gameObject);
            Add<SurvivalNeeds>(player.gameObject);
            Add<StatusEffectController>(player.gameObject);
            Add<PlayerVisibility>(player.gameObject);
            Add<ProceduralSurvivorMotion>(player.gameObject);
            Add<SurvivorLocomotion>(player.gameObject);
            CharacterVariety.Ensure(player.gameObject).Bind("survivor", true);
            EnsureRifle(player);
            ApplyFireModes(player);
        }

        private static void EnsureRifle(PlayerController player)
        {
            var guns = player.GetComponentsInChildren<FirearmWeapon>(true);
            foreach (var gun in guns)
            {
                if (gun.Type == WeaponType.Rifle) return;
            }

            Transform socket = player.transform.Find("Weapon_Socket");
            if (socket == null) socket = player.transform;
            var rifleObject = new GameObject("Assault_Rifle");
            rifleObject.transform.SetParent(socket, false);
            var rifle = rifleObject.AddComponent<FirearmWeapon>();
            var definition = ScriptableObject.CreateInstance<WeaponDefinition>();
            definition.id = "rifle_assault";
            definition.displayName = "Assault Rifle";
            definition.weaponType = WeaponType.Rifle;
            definition.baseDamage = 26f;
            definition.attackRate = 9f;
            definition.range = 32f;
            definition.spreadAngle = 3f;
            definition.projectilesPerShot = 1;
            definition.maxMagazine = 30;
            definition.reserveAmmo = 90;
            definition.reloadDuration = 2.1f;
            definition.noiseRadius = 34f;
            definition.noiseIntensity = 1f;
            definition.noiseType = NoiseType.GunshotLoud;
            definition.automatic = true;
            definition.useProjectile = true;
            definition.modelPath = ModelPaths.AssaultRifle;
            rifle.Configure(definition);
            player.AddWeapon(rifle);
        }

        private static void ApplyFireModes(PlayerController player)
        {
            var guns = player.GetComponentsInChildren<FirearmWeapon>(true);
            for (int i = 0; i < guns.Length; i++)
            {
                guns[i].SetFireMode(WeaponCard.FiresAutomatic(guns[i].Type, false), WeaponCard.FiresProjectile(guns[i].Type, false));
            }
        }

        private static void Walk(GameObject go)
        {
            string name = go.name;
            if (name.Contains("Barrel") && go.GetComponent<DestructibleHazard>() == null)
            {
                var hazard = go.AddComponent<DestructibleHazard>();
                if (name.Contains("Toxic")) hazard.Configure(HazardKind.Toxic);
                else if (name.Contains("Oil")) hazard.Configure(HazardKind.Oil);
                else hazard.Configure(HazardKind.Explosive);
                hazard.Stamp(StreetLedger.Mark(name, go.transform.position.x, go.transform.position.z));
            }

            if (name.Contains("Dumpster") && go.GetComponent<FlyMark>() == null)
                go.AddComponent<FlyMark>();

            if (name.StartsWith("Building_")) WallSeal.Seal(go);
            if (name.Contains("StreetLamp")) SodiumLamp.Raise(go);
            if (name.Contains("Generator")) YardFlood.Raise(go);
            if (name.Contains("Sedan") || name.Contains("Truck") || name.Contains("Vehicle")) HazardBlink.Raise(go);

            if ((name.Contains("Crate") || name.Contains("Dumpster")) && go.GetComponent<LootContainer>() == null)
            {
                var container = go.AddComponent<LootContainer>();
                container.Configure(name.Contains("Mil") ? "military" : "crate");
                container.Stamp(StreetLedger.Mark(name, go.transform.position.x, go.transform.position.z));
                if (go.GetComponent<Collider>() == null)
                {
                    var box = go.AddComponent<BoxCollider>();
                    box.size = new Vector3(1.2f, 1.2f, 1.2f);
                }
            }

            CampStation station = go.GetComponent<CampStation>();
            if (station == null)
            {
                if (name.Contains("Workbench")) station = AddStation(go, StationKind.Workbench);
                else if (name.Contains("Campfire")) station = AddStation(go, StationKind.Campfire);
                else if (name.Contains("MedicalCot")) station = AddStation(go, StationKind.MedicalCot);
                else if (name.Contains("WaterCollector")) station = AddStation(go, StationKind.Water);
                else if (name.Contains("Merchant")) station = AddStation(go, StationKind.Merchant);
                else if (name.Contains("Generator")) station = AddStation(go, StationKind.Generator);
            }
            if (station != null && station.Kind == StationKind.Merchant)
            {
                go.layer = GameLayers.Interactable;
            }

            var zombie = go.GetComponent<ZombieAI>();
            if (zombie != null)
            {
                if (name.Contains("Runner")) zombie.SetAbility(ZombieSpecialAbility.Lunge);
                else if (name.Contains("Brute")) zombie.SetAbility(ZombieSpecialAbility.Charge);
                if (go.GetComponent<ZombieMotion>() == null) go.AddComponent<ZombieMotion>();
            }
            else if (name.Contains("Merchant"))
            {
                CharacterVariety.Ensure(go).Bind("merchant", false);
            }
            else if (name.Contains("Colonist"))
            {
                CharacterVariety.Ensure(go).Bind("colonist", false);
            }

            var spawner = go.GetComponent<ZombieSpawner>();
            if (spawner != null)
            {
                Add<HordeDirector>(go);
                spawner.UseDirectorForSpawns();
            }

            var light = go.GetComponent<Light>();
            if (light != null && light.type != LightType.Directional && !name.Contains("Flashlight") && go.GetComponent<LightSource>() == null)
            {
                go.AddComponent<LightSource>().Configure(light.range > 0f ? light.range : 8f);
            }

            foreach (Transform child in go.transform)
            {
                Walk(child.gameObject);
            }
        }

        private static CampStation AddStation(GameObject go, StationKind kind)
        {
            var station = go.AddComponent<CampStation>();
            station.Configure(kind);
            if (go.GetComponent<Collider>() == null)
            {
                var box = go.AddComponent<BoxCollider>();
                box.size = new Vector3(1.4f, 1.6f, 1.4f);
                box.center = new Vector3(0f, 0.8f, 0f);
            }
            return station;
        }

        private static void Add<T>(GameObject host) where T : Component
        {
            if (host.GetComponent<T>() == null) host.gameObject.AddComponent<T>();
        }
    }
}
