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
using OutpostZero.Graphics;

namespace OutpostZero.EditorTools
{
    public static class PrototypeSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/PrototypeArena.unity";
        private const string SettingsDir = "Assets/Settings";
        private const string MaterialsDir = "Assets/Materials";
        private const string ModelsDir = "Assets/Models";
        private static int missingModels;

        [MenuItem("Tools/Outpost Zero/Build Prototype Test Arena", false, 1)]
        public static void BuildScene()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Build Outpost Zero Detailed Arena");

            // 0. Ensure URP Pipeline Asset & Settings
            EnsureURPPipelineConfigured();
            RendererFeatureSetup.EnsureDecals();
            missingModels = 0;
            DefaultDataGenerator.Generate();
            SurvivorAnimatorBuilder.Build();

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
            RendererFeatureSetup.EnsureDecals();
            missingModels = 0;
            DefaultDataGenerator.Generate();
            SurvivorAnimatorBuilder.Build();

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

            if (missingModels > 0)
            {
                string message = $"[Outpost Zero] {missingModels} model(s) missing. Refusing to save a scene full of placeholder cubes.";
                Debug.LogError(message);
                if (Application.isBatchMode)
                {
                    throw new System.InvalidOperationException(message);
                }
            }

            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            AssetDatabase.SaveAssets();
            Debug.Log($"[Outpost Zero] Successfully saved and registered scene at {ScenePath}!");
        }

        public static void CiBuildLinuxPlayer()
        {
            try
            {
                BuildAndSaveSceneBatch();
                string location = Path.Combine("Builds", "Linux", "OutpostZero.x86_64");
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath },
                    locationPathName = location,
                    target = BuildTarget.StandaloneLinux64,
                    options = BuildOptions.None
                });
                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                {
                    EditorApplication.Exit(1);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildGameExecutable()
        {
            try
            {
                BuildAndSaveSceneBatch();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                return;
            }

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

        private static GameObject InstantiateModel(string relativePath, string name, Vector3 pos, Quaternion rot, Vector3 scale, Transform parent = null, bool isStatic = true, bool addBoxCollider = false, int layer = GameLayers.Environment)
        {
            string fullPath = relativePath.StartsWith("Assets/") ? relativePath : $"{ModelsDir}/{relativePath}";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);

            GameObject instance;
            if (prefab != null)
            {
                instance = Object.Instantiate(prefab, pos, rot, parent);
                instance.name = name;
            }
            else
            {
                missingModels++;
                Debug.LogError($"[PrototypeSceneBuilder] Model not found at {fullPath}.");
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

            GameLayers.ApplyRecursively(instance, layer);
            Undo.RegisterCreatedObjectUndo(instance, $"Create {name}");
            return instance;
        }

        private static void AttachLoot(GameObject instance, LootKind kind, int amount)
        {
            if (instance == null) return;
            GameLayers.ApplyRecursively(instance, GameLayers.Loot);
            var pickup = Attach.Ensure<LootPickup>(instance);
            pickup.Configure(kind, amount);
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

            var lightData = Attach.Ensure<UniversalAdditionalLightData>(light.gameObject);

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
            GameLayers.ApplyRecursively(groundRoot, GameLayers.Environment);

            var probeHost = new GameObject("LightProbeGrid");
            probeHost.transform.SetParent(groundRoot.transform, false);
            var group = probeHost.AddComponent<LightProbeGroup>();
            group.probePositions = ProbeGrid.Lights(ProbeGrid.YardMin, ProbeGrid.YardMax, ProbeGrid.YardMin, ProbeGrid.YardMax);

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
            var surface = Attach.Ensure<NavMeshSurface>(ground);
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
            InstantiateModel(ModelPaths.Relative(ModelPaths.StreetLamp), "StreetLamp_NW", new Vector3(-3.2f, 0, 10f), Quaternion.Euler(0, 90, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_StreetBench.fbx", "StreetBench_NW", new Vector3(-3.2f, 0, 13f), Quaternion.Euler(0, 90, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Vehicle_Wrecked_Sedan.fbx", "Sedan_NW_Parked", new Vector3(-2.2f, 0, 8f), Quaternion.Euler(0, -8, 0), Vector3.one, obstaclesRoot.transform);

            // 2. North-East Industrial Warehouse Sector
            InstantiateModel("Environment/Building_Warehouse_Depot.fbx", "Building_Warehouse_NE", new Vector3(14f, 0, 14f), Quaternion.Euler(0, -90, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Crate_Wood.fbx", "WoodCrate_Stack1", new Vector3(8.5f, 0, 12f), Quaternion.Euler(0, 18, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Crate_Wood.fbx", "WoodCrate_Stack2", new Vector3(8.5f, 1.2f, 12f), Quaternion.Euler(0, 32, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Crate_Military.fbx", "MilCrate_NE", new Vector3(7.2f, 0, 13.5f), Quaternion.Euler(0, -10, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Barrel_Toxic.fbx", "Barrel_Toxic_NE", new Vector3(8.0f, 0, 15f), Quaternion.identity, Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Barrel_Oil.fbx", "Barrel_Oil_NE", new Vector3(8.8f, 0, 15.2f), Quaternion.identity, Vector3.one, obstaclesRoot.transform);
            AttachLoot(InstantiateModel(ModelPaths.Relative(ModelPaths.ScrapPile), "Salvage_Scrap_NE", new Vector3(9.2f, 0, 10.5f), Quaternion.Euler(0, 45, 0), Vector3.one, obstaclesRoot.transform, isStatic: false), LootKind.Scrap, 8);

            // 3. South-East Ruined City Sector
            InstantiateModel("Environment/Ruin_Wall_Corner.fbx", "Ruin_Wall_SE", new Vector3(12f, 0, -14f), Quaternion.identity, Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Barricade_Concrete_Jersey.fbx", "Jersey_SE_1", new Vector3(7.5f, 0, -12f), Quaternion.Euler(0, 20, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Prop_Barrel_Red_Explosive.fbx", "Barrel_Explosive_SE", new Vector3(6.2f, 0, -13f), Quaternion.identity, Vector3.one, obstaclesRoot.transform);
            AttachLoot(InstantiateModel(ModelPaths.Relative(ModelPaths.AmmoShotgun), "Loot_ShotgunAmmo_SE", new Vector3(11.5f, 0, -12.5f), Quaternion.identity, Vector3.one, obstaclesRoot.transform, isStatic: false), LootKind.AmmoShotgun, 12);

            // 4. Central Roadblock & Checkpoint
            InstantiateModel("Props/Vehicle_Apocalypse_Truck.fbx", "ArmoredTruck_Roadblock", new Vector3(1.2f, 0, 0f), Quaternion.Euler(0, 28, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Barricade_Concrete_Jersey.fbx", "Jersey_Center_1", new Vector3(-1.8f, 0, 2f), Quaternion.Euler(0, -15, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel("Props/Barricade_Wood_Wire.fbx", "WoodWire_Center", new Vector3(2.8f, 0, -2.5f), Quaternion.Euler(0, 40, 0), Vector3.one, obstaclesRoot.transform);
            InstantiateModel(ModelPaths.Relative(ModelPaths.StreetLamp), "StreetLamp_Center", new Vector3(3.2f, 0, 5f), Quaternion.Euler(0, -90, 0), Vector3.one, obstaclesRoot.transform);
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
            InstantiateModel(ModelPaths.Relative(ModelPaths.Colonist), "NPC_Colonist_Engineer", new Vector3(-14f, 0, -9.5f), Quaternion.identity, Vector3.one, sanctuaryRoot.transform, isStatic: false, addBoxCollider: true, layer: 0);

            // NPC Merchant Vendor Stall
            InstantiateModel(ModelPaths.Relative(ModelPaths.Merchant), "NPC_Merchant_Vendor", new Vector3(-9.5f, 0, -10f), Quaternion.Euler(0, 135, 0), Vector3.one, sanctuaryRoot.transform, isStatic: false, addBoxCollider: true, layer: 0);

            // Starting Survival Loot Supplies in Outpost
            AttachLoot(InstantiateModel(ModelPaths.Relative(ModelPaths.Medkit), "Supply_Medkit_Start", new Vector3(-11f, 0, -14.5f), Quaternion.identity, Vector3.one, sanctuaryRoot.transform, isStatic: false), LootKind.Medkit, 1);
            AttachLoot(InstantiateModel(ModelPaths.Relative(ModelPaths.Ammo9mm), "Supply_Ammo_9mm_Start", new Vector3(-13.5f, 0.9f, -8.5f), Quaternion.identity, Vector3.one, sanctuaryRoot.transform, isStatic: false), LootKind.Ammo9mm, 24);
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
            GameObject playerVisual = InstantiateModel(ModelPaths.Relative(ModelPaths.SurvivorLeader), "Survivor_BodyMesh", Vector3.zero, Quaternion.identity, Vector3.one, player.transform, isStatic: false, addBoxCollider: false, layer: GameLayers.Player);
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
            spot.spotAngle = LampCookie.Outer;
            spot.innerSpotAngle = LampCookie.Inner;
            spot.shadows = LightShadows.Soft;
            spot.range = 32f;
            spot.intensity = 2.8f;
            spot.color = new Color(1f, 0.96f, 0.88f);

            var health = player.AddComponent<HealthSystem>();
            health.Configure(100f);
            player.AddComponent<PlayerInventory>();
            GameLayers.ApplyRecursively(player, GameLayers.Player);

            GameObject socket = new GameObject("Weapon_Socket");
            socket.transform.SetParent(player.transform);
            socket.transform.localPosition = new Vector3(0.28f, 1.05f, 0.45f);

            var pistolDef = DefaultDataGenerator.LoadWeapon("Pistol_9mm");
            var shotgunDef = DefaultDataGenerator.LoadWeapon("Shotgun_Pump");
            var macheteDef = DefaultDataGenerator.LoadWeapon("Machete");

            GameObject pistolObj = new GameObject("Pistol_9mm");
            pistolObj.transform.SetParent(socket.transform, false);
            var pistol = pistolObj.AddComponent<FirearmWeapon>();
            pistol.Configure(pistolDef);
            GameObject pistolMesh = InstantiateModel(ModelPaths.Relative(ModelPaths.Pistol), "Pistol_Mesh", Vector3.zero, Quaternion.identity, Vector3.one, pistolObj.transform, isStatic: false, layer: GameLayers.Player);
            pistolMesh.transform.localPosition = Vector3.zero;
            pistolMesh.transform.localRotation = Quaternion.Euler(0, 90, 0);

            GameObject shotgunObj = new GameObject("Shotgun_Pump");
            shotgunObj.transform.SetParent(socket.transform, false);
            var shotgun = shotgunObj.AddComponent<FirearmWeapon>();
            shotgun.Configure(shotgunDef);
            GameObject shotgunMesh = InstantiateModel(ModelPaths.Relative(ModelPaths.Shotgun), "Shotgun_Mesh", Vector3.zero, Quaternion.identity, Vector3.one, shotgunObj.transform, isStatic: false, layer: GameLayers.Player);
            shotgunMesh.transform.localPosition = new Vector3(0, 0, 0.1f);
            shotgunMesh.transform.localRotation = Quaternion.Euler(0, 90, 0);

            GameObject macheteObj = new GameObject("Combat_Machete");
            macheteObj.transform.SetParent(socket.transform, false);
            var machete = macheteObj.AddComponent<MeleeWeapon>();
            machete.Configure(macheteDef);
            GameObject macheteMesh = InstantiateModel(ModelPaths.Relative(ModelPaths.Machete), "Machete_Mesh", Vector3.zero, Quaternion.identity, Vector3.one, macheteObj.transform, isStatic: false, layer: GameLayers.Player);
            macheteMesh.transform.localPosition = new Vector3(0, 0, 0.15f);
            macheteMesh.transform.localRotation = Quaternion.Euler(0, 90, 0);

            var rifleDef = DefaultDataGenerator.LoadWeapon("Rifle_Assault");
            GameObject rifleObj = new GameObject("Assault_Rifle");
            rifleObj.transform.SetParent(socket.transform, false);
            var rifle = rifleObj.AddComponent<FirearmWeapon>();
            rifle.Configure(rifleDef);
            GameObject rifleMesh = InstantiateModel(ModelPaths.Relative(ModelPaths.AssaultRifle), "Rifle_Mesh", Vector3.zero, Quaternion.identity, Vector3.one, rifleObj.transform, isStatic: false, layer: GameLayers.Player);
            rifleMesh.transform.localPosition = Vector3.zero;
            rifleMesh.transform.localRotation = Quaternion.Euler(0, 90, 0);

            var pc = player.AddComponent<PlayerController>();
            pc.Configure(new WeaponBase[] { pistol, shotgun, rifle, machete }, spot);

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

            var camData = Attach.Ensure<UniversalAdditionalCameraData>(cam.gameObject);

            var follow = Attach.Ensure<TopDownCameraFollow>(cam.gameObject);
            follow.SetFollowTarget(playerTarget);
        }

        private static void SetupHUD(GameObject playerObj)
        {
            if (Object.FindFirstObjectByType<SurvivalHUD>() == null)
            {
                GameObject hudObj = new GameObject("--- SURVIVAL HUD ---");
                var hud = hudObj.AddComponent<SurvivalHUD>();
                hud.Bind(playerObj.GetComponent<PlayerController>());
                Undo.RegisterCreatedObjectUndo(hudObj, "Create Survival HUD");
            }
        }

        private static void SetupZombies(Transform playerTransform)
        {
            // 1. Prototype Walker
            GameObject walker = CreateZombiePrototype("Zombie_Walker", DefaultDataGenerator.LoadZombie("Walker"));
            GameObject runner = CreateZombiePrototype("Zombie_Runner", DefaultDataGenerator.LoadZombie("Runner"));
            GameObject brute = CreateZombiePrototype("Zombie_Brute", DefaultDataGenerator.LoadZombie("Brute"));

            GameObject spawnerObj = new GameObject("--- ZOMBIE HORDE SPAWNER ---");
            spawnerObj.AddComponent<ZombiePool>();
            var spawner = spawnerObj.AddComponent<ZombieSpawner>();
            spawner.Configure(walker, new GameObject[] { walker, runner, brute }, 14, 32);
            spawnerObj.AddComponent<HordeDirector>();
            spawner.UseDirectorForSpawns();

            Undo.RegisterCreatedObjectUndo(spawnerObj, "Create Zombie Spawner");
        }

        private static GameObject CreateZombiePrototype(string name, ZombieArchetype archetype)
        {
            GameObject proto = new GameObject(name);
            proto.tag = "Enemy";
            proto.layer = GameLayers.Enemy;
            proto.transform.position = new Vector3(0, -100f, 0);

            string modelPath = archetype != null && !string.IsNullOrEmpty(archetype.modelPath)
                ? ModelPaths.Relative(archetype.modelPath)
                : ModelPaths.Relative(ModelPaths.ZombieWalker);
            GameObject visual = InstantiateModel(modelPath, "Zombie_Mesh", Vector3.zero, Quaternion.identity, Vector3.one, proto.transform, isStatic: false, layer: GameLayers.Enemy);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            foreach (var col in visual.GetComponentsInChildren<Collider>())
            {
                Object.DestroyImmediate(col);
            }

            var cc = proto.AddComponent<CapsuleCollider>();
            cc.height = 1.9f;
            cc.radius = 0.45f;
            cc.center = new Vector3(0, 0.95f, 0);

            var agent = proto.AddComponent<NavMeshAgent>();
            agent.stoppingDistance = 1.2f;
            agent.radius = 0.45f;
            agent.height = 1.9f;

            proto.AddComponent<HealthSystem>();
            var ai = proto.AddComponent<ZombieAI>();
            ai.Configure(archetype);
            GameLayers.ApplyRecursively(proto, GameLayers.Enemy);

            proto.SetActive(false);
            Undo.RegisterCreatedObjectUndo(proto, $"Create {name}");
            return proto;
        }
    }
}
#endif
