#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using OutpostZero.Core;
using OutpostZero.Sensory;
using OutpostZero.Player;
using OutpostZero.Combat;
using OutpostZero.AI;
using OutpostZero.UI;
using OutpostZero.Utils;

namespace OutpostZero.EditorTools
{
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/PrototypeArena.unity";
        private const string SettingsDir = "Assets/Settings";
        private const string MaterialsDir = "Assets/Materials";
        private const string ModelsDir = "Assets/Models";

        [MenuItem("Tools/Outpost Zero/Build Prototype Test Arena", false, 1)]
        public static void BuildScene()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Outpost Zero Detailed Arena");

            // 0. Ensure URP Pipeline Asset & Settings
            EnsureURPPipelineConfigured();

            // 1. Core Singletons
            EnsureCoreManagers();

            // 2. Lighting & Atmosphere
            SetupAtmosphere();

            // 3. Ground, Streets & Urban Environment
            GameObject ground = CreateGround();
            CreateUrbanObstacles(ground.transform);
            CreateSanctuaryHub(ground.transform);
            BakeNavMeshOnGround(ground);

            // 4. Weapons & Player
            GameObject player = CreatePlayer();

            // 5. Camera Follow
            SetupCamera(player.transform);

            // 6. UI HUD
            SetupHUD(player);

            // 7. Zombie Prototype Variants & Spawner
            SetupZombies(player.transform);

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[Outpost Zero] Detailed 3D Arena built successfully! Press Play to test!");
            EditorUtility.DisplayDialog("Outpost Zero Arena Ready", 
                "Detailed 3D World successfully built with Blender models, URP materials, and full tactical survival systems!\n\nPress Play to test!", "Awesome!");
        }

        public static void BuildAndSaveSceneBatch()
        {
            Debug.Log("[Outpost Zero] Starting headless batch scene build...");

            EnsureURPPipelineConfigured();

            if (!Directory.Exists("Assets/Scenes"))
            {
                Directory.CreateDirectory("Assets/Scenes");
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            EnsureCoreManagers();
            SetupAtmosphere();
            GameObject ground = CreateGround();
            CreateUrbanObstacles(ground.transform);
            CreateSanctuaryHub(ground.transform);
            BakeNavMeshOnGround(ground);
            GameObject player = CreatePlayer();
            SetupCamera(player.transform);
            SetupHUD(player);
            SetupZombies(player.transform);

            // In-game auto screen capture utility
            var capGo = new GameObject("ScreenCaptureManager");
            capGo.AddComponent<AutoScreenCapture>();

            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            Debug.Log($"[Outpost Zero] Successfully saved and registered scene at {ScenePath}!");
        }

        public static void BuildGameExecutable()
        {
            BuildAndSaveSceneBatch();

            string buildDir = "Builds";
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            string exePath = Path.Combine(buildDir, "OutpostZero.exe");
            Debug.Log($"[Outpost Zero] Building Windows Standalone player to: {exePath}...");

            BuildPlayerOptions buildOptions = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);
            Debug.Log($"[Outpost Zero] Build result: {report.summary.result} (Errors: {report.summary.totalErrors})");

            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        public static UniversalRenderPipelineAsset EnsureURPPipelineConfigured()
        {
            if (!Directory.Exists(SettingsDir))
            {
                Directory.CreateDirectory(SettingsDir);
            }

            string urpAssetPath = $"{SettingsDir}/OutpostZero_URP.asset";
            string rendererPath = $"{SettingsDir}/OutpostZero_URP_Renderer.asset";

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, rendererPath);
            }

            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
            if (urpAsset == null)
            {
                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(urpAsset, urpAssetPath);
            }

            GraphicsSettings.defaultRenderPipeline = urpAsset;
            QualitySettings.renderPipeline = urpAsset;

            for (int i = 0; i < QualitySettings.count; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urpAsset;
            }

            AssetDatabase.SaveAssets();
            return urpAsset;
        }

        private static Material GetOrCreateMaterial(string matName, Color color)
        {
            if (!Directory.Exists(MaterialsDir))
            {
                Directory.CreateDirectory(MaterialsDir);
            }

            string path = $"{MaterialsDir}/{matName}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit") 
                               ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                               ?? Shader.Find("Standard");

            if (mat == null)
            {
                mat = new Material(urpLitShader);
                mat.name = matName;
                ApplyMaterialColor(mat, color);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                if (mat.shader != urpLitShader && urpLitShader != null)
                {
                    mat.shader = urpLitShader;
                }
                ApplyMaterialColor(mat, color);
                EditorUtility.SetDirty(mat);
            }

            return mat;
        }

        private static void ApplyMaterialColor(Material mat, Color color)
        {
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
        }

        private static GameObject InstantiateModel(string relativePath, string name, Vector3 pos, Quaternion rot, Vector3 scale, Transform parent = null, bool isStatic = true, bool addBoxCollider = false)
        {
            string fullPath = $"{ModelsDir}/{relativePath}";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);

            GameObject instance;
            if (prefab != null)
            {
                instance = Object.Instantiate(prefab, pos, rot, parent);
                instance.name = name;
            }
            else
            {
                Debug.LogWarning($"[PrototypeSceneBuilder] Model not found at {fullPath}, generating placeholder primitive cube.");
                instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                instance.name = name;
                instance.transform.position = pos;
                instance.transform.rotation = rot;
                if (parent != null) instance.transform.SetParent(parent);
            }

            instance.transform.localScale = scale;

            if (isStatic)
            {
                instance.isStatic = true;
                foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.isStatic = true;
                }
            }

            // Ensure colliders exist for physics and navigation
            if (addBoxCollider)
            {
                if (instance.GetComponent<Collider>() == null)
                {
                    instance.AddComponent<BoxCollider>();
                }
            }
            else
            {
                EnsureMeshOrBoxColliders(instance, isStatic);
            }

            Undo.RegisterCreatedObjectUndo(instance, $"Create {name}");
            return instance;
        }

        private static void EnsureMeshOrBoxColliders(GameObject root, bool isStatic)
        {
            var colliders = root.GetComponentsInChildren<Collider>();
            if (colliders.Length == 0)
            {
                var renderers = root.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length > 0)
                {
                    foreach (var r in renderers)
                    {
                        var mf = r.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            var mc = r.gameObject.AddComponent<MeshCollider>();
                            mc.sharedMesh = mf.sharedMesh;
                            if (!isStatic) mc.convex = true;
                        }
                    }
                }
                else
                {
                    root.AddComponent<BoxCollider>();
                }
            }
        }

        private static void EnsureCoreManagers()
        {
            if (Object.FindFirstObjectByType<GameManager>() == null)
            {
                GameObject gmObj = new GameObject("--- GAME MANAGER ---");
                gmObj.AddComponent<GameManager>();
                Undo.RegisterCreatedObjectUndo(gmObj, "Create GameManager");
            }

            if (Object.FindFirstObjectByType<NoiseManager>() == null)
            {
                GameObject nmObj = new GameObject("--- NOISE MANAGER ---");
                nmObj.AddComponent<NoiseManager>();
                Undo.RegisterCreatedObjectUndo(nmObj, "Create NoiseManager");
            }
        }

        private static void SetupAtmosphere()
        {
            var light = Object.FindFirstObjectByType<Light>();
            if (light == null || light.type != LightType.Directional)
            {
                GameObject sunObj = new GameObject("Directional Light (Sun)");
                light = sunObj.AddComponent<Light>();
                light.type = LightType.Directional;
                Undo.RegisterCreatedObjectUndo(sunObj, "Create Sun Light");
            }

            light.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
            light.color = new Color(1.0f, 0.94f, 0.86f); // Warm late-afternoon post-apocalyptic sunlight
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;

            var lightData = light.GetComponent<UniversalAdditionalLightData>() ?? light.gameObject.AddComponent<UniversalAdditionalLightData>();

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.36f, 0.38f, 0.44f);
        }

        private static GameObject CreateGround()
        {
            GameObject groundRoot = new GameObject("--- ENVIRONMENT ROOT ---");
            groundRoot.transform.position = Vector3.zero;

            // City ground plane (70m x 70m)
            GameObject baseGround = GameObject.CreatePrimitive(PrimitiveType.Plane);
            baseGround.name = "Ground_AsphaltBase";
            baseGround.transform.SetParent(groundRoot.transform);
            baseGround.transform.position = Vector3.zero;
            baseGround.transform.localScale = new Vector3(7.0f, 1.0f, 7.0f);

            var renderer = baseGround.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GetOrCreateMaterial("Mat_Ground_Asphalt", new Color(0.18f, 0.19f, 0.21f));
            baseGround.isStatic = true;

            // Central Avenue (North-South, 4 straight road tiles)
            InstantiateModel("Environment/Road_Tile_Straight.fbx", "Road_North_2", new Vector3(0, 0, 15), Quaternion.identity, Vector3.one, groundRoot.transform);
            InstantiateModel("Environment/Road_Tile_Straight.fbx", "Road_North_1", new Vector3(0, 0, 5), Quaternion.identity, Vector3.one, groundRoot.transform);
            InstantiateModel("Environment/Road_Tile_Intersection.fbx", "Road_Intersection_Center", new Vector3(0, 0, -5), Quaternion.identity, Vector3.one, groundRoot.transform);
            InstantiateModel("Environment/Road_Tile_Straight.fbx", "Road_South_1", new Vector3(0, 0, -15), Quaternion.identity, Vector3.one, groundRoot.transform);

            // Cross street (East-West)
            Quaternion rotEastWest = Quaternion.Euler(0, 90, 0);
            InstantiateModel("Environment/Road_Tile_Straight.fbx", "Road_West_1", new Vector3(-10, 0, -5), rotEastWest, Vector3.one, groundRoot.transform);
            InstantiateModel("Environment/Road_Tile_Straight.fbx", "Road_East_1", new Vector3(10, 0, -5), rotEastWest, Vector3.one, groundRoot.transform);

            Undo.RegisterCreatedObjectUndo(groundRoot, "Create Ground & Streets");
            return baseGround;
        }

        private static void BakeNavMeshOnGround(GameObject ground)
        {
            ground.isStatic = true;
            var surface = ground.GetComponent<NavMeshSurface>() ?? ground.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
            surface.BuildNavMesh();

            if (surface.navMeshData != null)
            {
                string navMeshDir = "Assets/Scenes/PrototypeArena";
                if (!Directory.Exists(navMeshDir))
                {
                    Directory.CreateDirectory(navMeshDir);
                }
                string navMeshPath = $"{navMeshDir}/NavMesh-Ground_Street.asset";

                var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(navMeshPath);
                if (existing != null)
                {
                    AssetDatabase.DeleteAsset(navMeshPath);
                }
                AssetDatabase.CreateAsset(surface.navMeshData, navMeshPath);
                AssetDatabase.SaveAssets();

                surface.navMeshData = AssetDatabase.LoadAssetAtPath<NavMeshData>(navMeshPath);
                EditorUtility.SetDirty(surface);
                EditorUtility.SetDirty(ground);
            }
        }

        private static void CreateUrbanObstacles(Transform parent)
        {
            GameObject obstaclesRoot = new GameObject("--- URBAN ARCHITECTURE & PROPS ---");
            obstaclesRoot.transform.SetParent(parent);

            // 1. North-West Commercial Sector
            InstantiateModel("Environment/Building_Storefront_2Story.fbx", "Building_Storefront_NW", new Vector3(-11f, 0, 14f), Quaternion.Euler(0, 90, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Dumpster.fbx", "Dumpster_Alley", new Vector3(-6.5f, 0, 18f), Quaternion.Euler(0, -15, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_StreetLamp.fbx", "StreetLamp_NW", new Vector3(-3.2f, 0, 10f), Quaternion.Euler(0, 90, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_StreetBench.fbx", "StreetBench_NW", new Vector3(-3.2f, 0, 13f), Quaternion.Euler(0, 90, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Vehicle_Wrecked_Sedan.fbx", "Sedan_NW_Parked", new Vector3(-2.2f, 0, 8f), Quaternion.Euler(0, -8, 0), Vector3.one, obstaclesRoot.transform);

            // 2. North-East Industrial Warehouse Sector
            InstantiateModel("Environment/Building_Warehouse_Depot.fbx", "Building_Warehouse_NE", new Vector3(14f, 0, 14f), Quaternion.Euler(0, -90, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Crate_Wood.fbx", "WoodCrate_Stack1", new Vector3(8.5f, 0, 12f), Quaternion.Euler(0, 18, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Crate_Wood.fbx", "WoodCrate_Stack2", new Vector3(8.5f, 1.2f, 12f), Quaternion.Euler(0, 32, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Crate_Military.fbx", "MilCrate_NE", new Vector3(7.2f, 0, 13.5f), Quaternion.Euler(0, -10, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Barrel_Toxic.fbx", "Barrel_Toxic_NE", new Vector3(8.0f, 0, 15f), Quaternion.identity, Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Barrel_Oil.fbx", "Barrel_Oil_NE", new Vector3(8.8f, 0, 15.2f), Quaternion.identity, Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Weapons/Loot_ScrapPile.fbx", "Salvage_Scrap_NE", new Vector3(9.2f, 0, 10.5f), Quaternion.Euler(0, 45, 0), Vector3.one, obstaclesRoot.transform);

            // 3. South-East Ruined City Sector
            InstantiateModel("Environment/Ruin_Wall_Corner.fbx", "Ruin_Wall_SE", new Vector3(12f, 0, -14f), Quaternion.identity, Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Barricade_Concrete_Jersey.fbx", "Jersey_SE_1", new Vector3(7.5f, 0, -12f), Quaternion.Euler(0, 20, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Barrel_Red_Explosive.fbx", "Barrel_Explosive_SE", new Vector3(6.2f, 0, -13f), Quaternion.identity, Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Weapons/Loot_AmmoBox_Shotgun.fbx", "Loot_ShotgunAmmo_SE", new Vector3(11.5f, 0, -12.5f), Quaternion.identity, Vector3.one, obstaclesRoot.transform);

            // 4. Central Roadblock & Checkpoint
            InstantiateModel("Props/Vehicle_Apocalypse_Truck.fbx", "ArmoredTruck_Roadblock", new Vector3(1.2f, 0, 0f), Quaternion.Euler(0, 28, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Barricade_Concrete_Jersey.fbx", "Jersey_Center_1", new Vector3(-1.8f, 0, 2f), Quaternion.Euler(0, -15, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Barricade_Wood_Wire.fbx", "WoodWire_Center", new Vector3(2.8f, 0, -2.5f), Quaternion.Euler(0, 40, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/StreetLamp.fbx", "StreetLamp_Center", new Vector3(3.2f, 0, 5f), Quaternion.Euler(0, -90, 0), Vector3.one, obstaclesRoot.transform);
        }

        private static void CreateSanctuaryHub(Transform parent)
        {
            GameObject sanctuaryRoot = new GameObject("--- SANCTUARY COLONY HUB ---");
            sanctuaryRoot.transform.SetParent(parent);

            // Perimeter fortifications
            InstantiateModel("Props/Barricade_Sandbags.fbx", "Sanctuary_Sandbag_Wall", new Vector3(-8f, 0, -8f), Quaternion.Euler(0, 45, 0), Vector3.one, sanctuaryRoot.transform);
            InstantiateModel("Props/Barricade_Wood_Wire.fbx", "Sanctuary_Wood_Gate", new Vector3(-5.5f, 0, -10f), Quaternion.Euler(0, 30, 0), Vector3.one, sanctuaryRoot.transform);

            // Lookout Watchtower
            InstantiateModel("BaseBuilding/Base_Watchtower.fbx", "Watchtower_GuardPost", new Vector3(-16f, 0, -16f), Quaternion.Euler(0, 45, 0), Vector3.one, sanctuaryRoot.transform);

            // Colony Base Modules
            InstantiateModel("BaseBuilding/Base_Campfire_Cooker.fbx", "Sanctuary_Campfire", new Vector3(-12f, 0, -12f), Quaternion.identity, Vector3.one, sanctuaryRoot.transform);
            InstantiateModel("BaseBuilding/Base_CraftingWorkbench.fbx", "Sanctuary_Workbench", new Vector3(-14f, 0, -8.5f), Quaternion.Euler(0, 180, 0), Vector3.one, sanctuaryRoot.transform);
            InstantiateModel("BaseBuilding/Base_Generator_Diesel.fbx", "Sanctuary_Generator", new Vector3(-18f, 0, -11f), Quaternion.Euler(0, 90, 0), Vector3.one, sanctuaryRoot.transform);
            InstantiateModel("BaseBuilding/Base_MedicalCot.fbx", "Sanctuary_MedicalCot", new Vector3(-10f, 0, -15f), Quaternion.Euler(0, 30, 0), Vector3.one, sanctuaryRoot.transform);
            InstantiateModel("BaseBuilding/Base_WaterCollector.fbx", "Sanctuary_WaterCollector", new Vector3(-14.5f, 0, -14f), Quaternion.identity, Vector3.one, sanctuaryRoot.transform);

            // Friendly NPC Colonist at Workbench
            InstantiateModel("Characters/Colonist_Survivor.fbx", "NPC_Colonist_Engineer", new Vector3(-14f, 0, -9.5f), Quaternion.identity, Vector3.one, sanctuaryRoot.transform, isStatic: false, addBoxCollider: true);

            // NPC Merchant Vendor Stall
            InstantiateModel("Characters/NPC_Merchant.fbx", "NPC_Merchant_Vendor", new Vector3(-9.5f, 0, -10f), Quaternion.Euler(0, 135, 0), Vector3.one, sanctuaryRoot.transform, isStatic: false, addBoxCollider: true);

            // Starting Survival Loot Supplies in Outpost
            InstantiateModel("Weapons/Loot_Medkit.fbx", "Supply_Medkit_Start", new Vector3(-11f, 0, -14.5f), Quaternion.identity, Vector3.one, sanctuaryRoot.transform, isStatic: false);
            InstantiateModel("Weapons/Loot_AmmoBox_9mm.fbx", "Supply_Ammo_9mm_Start", new Vector3(-13.5f, 0.9f, -8.5f), Quaternion.identity, Vector3.one, sanctuaryRoot.transform, isStatic: false);
        }

        private static GameObject CreatePlayer()
        {
            GameObject player = new GameObject("Player_SurvivorLeader");
            player.tag = "Player";
            player.transform.position = new Vector3(0, 0.05f, 0);

            // Character Controller
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.9f;
            cc.radius = 0.40f;
            cc.center = new Vector3(0, 0.95f, 0);

            // Visual 3D Mesh (Survivor Leader model)
            GameObject playerVisual = InstantiateModel("Characters/Survivor_Leader.fbx", "Survivor_BodyMesh", Vector3.zero, Quaternion.identity, Vector3.one, player.transform, isStatic: false, addBoxCollider: false);
            playerVisual.transform.localPosition = Vector3.zero;
            playerVisual.transform.localRotation = Quaternion.identity;

            // Remove any colliders on visual child so CharacterController handles collisions
            foreach (var col in playerVisual.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(col);
            }

            // Tactical Flashlight attached to shoulder
            GameObject lightObj = new GameObject("Tactical_Flashlight");
            lightObj.transform.SetParent(player.transform);
            lightObj.transform.localPosition = new Vector3(0.25f, 1.4f, 0.2f);
            var spot = lightObj.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 65f;
            spot.range = 32f;
            spot.intensity = 2.8f;
            spot.color = new Color(1f, 0.96f, 0.88f);

            // Health & Inventory
            var health = player.AddComponent<HealthSystem>();
            SetPrivateField(health, "maxHealth", 100f);
            SetPrivateField(health, "currentHealth", 100f);
            var inv = player.AddComponent<PlayerInventory>();

            // Weapon Socket & 3D Weapon Models
            GameObject socket = new GameObject("Weapon_Socket");
            socket.transform.SetParent(player.transform);
            socket.transform.localPosition = new Vector3(0.28f, 1.05f, 0.45f);

            // Weapon 1: Tactical 9mm Pistol
            GameObject pistolObj = new GameObject("Pistol_9mm");
            pistolObj.transform.SetParent(socket.transform, false);
            var pistol = pistolObj.AddComponent<FirearmWeapon>();
            SetPrivateField(pistol, "weaponName", "Tactical 9mm Pistol");
            SetPrivateField(pistol, "weaponType", WeaponType.Pistol);
            SetPrivateField(pistol, "baseDamage", 34f);
            SetPrivateField(pistol, "attackRate", 3.2f);
            SetPrivateField(pistol, "noiseRadius", 20f);
            SetPrivateField(pistol, "maxMagazine", 12);
            SetPrivateField(pistol, "currentAmmo", 12);
            SetPrivateField(pistol, "reserveAmmo", 60);

            GameObject pistolMesh = InstantiateModel("Weapons/Weapon_Pistol_9mm.fbx", "Pistol_Mesh", Vector3.zero, Quaternion.identity, Vector3.one, pistolObj.transform, isStatic: false);
            pistolMesh.transform.localPosition = Vector3.zero;
            pistolMesh.transform.localRotation = Quaternion.Euler(0, 90, 0);

            // Weapon 2: Remington Pump Shotgun
            GameObject shotgunObj = new GameObject("Shotgun_Pump");
            shotgunObj.transform.SetParent(socket.transform, false);
            var shotgun = shotgunObj.AddComponent<FirearmWeapon>();
            SetPrivateField(shotgun, "weaponName", "Remington 870 Shotgun");
            SetPrivateField(shotgun, "weaponType", WeaponType.Shotgun);
            SetPrivateField(shotgun, "baseDamage", 19f); // x 7 pellets
            SetPrivateField(shotgun, "projectilesPerShot", 7);
            SetPrivateField(shotgun, "spreadAngle", 8.5f);
            SetPrivateField(shotgun, "attackRate", 1.1f);
            SetPrivateField(shotgun, "noiseRadius", 38f);
            SetPrivateField(shotgun, "maxMagazine", 6);
            SetPrivateField(shotgun, "currentAmmo", 6);
            SetPrivateField(shotgun, "reserveAmmo", 24);

            GameObject shotgunMesh = InstantiateModel("Weapons/Weapon_Shotgun_Pump.fbx", "Shotgun_Mesh", Vector3.zero, Quaternion.identity, Vector3.one, shotgunObj.transform, isStatic: false);
            shotgunMesh.transform.localPosition = new Vector3(0, 0, 0.1f);
            shotgunMesh.transform.localRotation = Quaternion.Euler(0, 90, 0);

            // Weapon 3: Combat Machete
            GameObject macheteObj = new GameObject("Combat_Machete");
            macheteObj.transform.SetParent(socket.transform, false);
            var machete = macheteObj.AddComponent<MeleeWeapon>();
            SetPrivateField(machete, "weaponName", "Steel Machete");
            SetPrivateField(machete, "baseDamage", 48f);
            SetPrivateField(machete, "range", 1.9f);
            SetPrivateField(machete, "attackRate", 1.8f);
            SetPrivateField(machete, "noiseRadius", 2.0f); // Stealth silent

            GameObject macheteMesh = InstantiateModel("Weapons/Weapon_Machete.fbx", "Machete_Mesh", Vector3.zero, Quaternion.identity, Vector3.one, macheteObj.transform, isStatic: false);
            macheteMesh.transform.localPosition = new Vector3(0, 0, 0.15f);
            macheteMesh.transform.localRotation = Quaternion.Euler(0, 90, 0);

            // Player Controller
            var pc = player.AddComponent<PlayerController>();
            SetPrivateField(pc, "equippedWeapons", new WeaponBase[] { pistol, shotgun, machete });
            SetPrivateField(pc, "flashlight", spot);

            Undo.RegisterCreatedObjectUndo(player, "Create Player Survivor");
            return player;
        }

        private static void SetupCamera(Transform playerTarget)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
                camObj.AddComponent<AudioListener>();
            }

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.12f, 0.15f);

            var camData = cam.GetComponent<UniversalAdditionalCameraData>() ?? cam.gameObject.AddComponent<UniversalAdditionalCameraData>();

            var follow = cam.GetComponent<TopDownCameraFollow>() ?? cam.gameObject.AddComponent<TopDownCameraFollow>();
            SetPrivateField(follow, "target", playerTarget);
        }

        private static void SetupHUD(GameObject playerObj)
        {
            if (Object.FindFirstObjectByType<SurvivalHUD>() == null)
            {
                GameObject hudObj = new GameObject("--- SURVIVAL HUD ---");
                var hud = hudObj.AddComponent<SurvivalHUD>();
                SetPrivateField(hud, "player", playerObj.GetComponent<PlayerController>());
                Undo.RegisterCreatedObjectUndo(hudObj, "Create Survival HUD");
            }
        }

        private static void SetupZombies(Transform playerTransform)
        {
            // 1. Prototype Walker
            GameObject walker = CreateZombiePrototype("Zombie_Walker", "Characters/Zombie_Walker.fbx", 3.8f, 65f, 18f, 1.4f);
            // 2. Prototype Runner (fast, agile)
            GameObject runner = CreateZombiePrototype("Zombie_Runner", "Characters/Zombie_Runner.fbx", 5.6f, 45f, 22f, 1.0f);
            // 3. Prototype Brute (heavy, tank)
            GameObject brute = CreateZombiePrototype("Zombie_Brute", "Characters/Zombie_Brute.fbx", 2.6f, 180f, 38f, 2.0f);

            // Spawner
            GameObject spawnerObj = new GameObject("--- ZOMBIE HORDE SPAWNER ---");
            var spawner = spawnerObj.AddComponent<ZombieSpawner>();
            SetPrivateField(spawner, "zombiePrefab", walker);
            SetPrivateField(spawner, "zombiePrefabVariants", new GameObject[] { walker, runner, brute });
            SetPrivateField(spawner, "initialCount", 14);
            SetPrivateField(spawner, "maxAliveZombies", 32);

            Undo.RegisterCreatedObjectUndo(spawnerObj, "Create Zombie Spawner");
        }

        private static GameObject CreateZombiePrototype(string name, string modelPath, float speed, float maxHp, float attackDmg, float attackCd)
        {
            GameObject proto = new GameObject(name);
            proto.tag = "Enemy";
            proto.transform.position = new Vector3(0, -100f, 0); // Park far off-screen

            // Visual mesh
            GameObject visual = InstantiateModel(modelPath, "Zombie_Mesh", Vector3.zero, Quaternion.identity, Vector3.one, proto.transform, isStatic: false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            foreach (var col in visual.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(col);
            }

            // Character Collider
            var cc = proto.AddComponent<CapsuleCollider>();
            cc.height = 1.9f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0, 0.95f, 0);

            // NavMeshAgent
            var agent = proto.AddComponent<NavMeshAgent>();
            agent.speed = speed;
            agent.stoppingDistance = 1.2f;
            agent.radius = 0.45f;
            agent.height = 1.9f;

            // Health & AI
            var health = proto.AddComponent<HealthSystem>();
            SetPrivateField(health, "maxHealth", maxHp);
            SetPrivateField(health, "currentHealth", maxHp);

            var ai = proto.AddComponent<ZombieAI>();
            SetPrivateField(ai, "chaseSpeed", speed);
            SetPrivateField(ai, "attackDamage", attackDmg);
            SetPrivateField(ai, "attackCooldown", attackCd);

            proto.SetActive(false); // Only active as an instantiated clone
            Undo.RegisterCreatedObjectUndo(proto, $"Create {name}");
            return proto;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance | 
                System.Reflection.BindingFlags.Public);

            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
#endif
