# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Outpost Zero is a Unity 6 (**6000.0.83f1**, URP) top-down zombie survival game with a colony sim: street expeditions (combat, stealth, noise, horde pacing, extraction) and a camp (survivor roster, needs and morale, grid building, crafting, night raids, factions, leader succession with permadeath). Models are generated procedurally by Python scripts run in Blender 4.2 (`BlenderScripts/`). The roadmap is the Linear project "Outpost Zero — Full Playable Game" (milestones M0–M5, issues PRO-10 to PRO-70).

Every system is written and unit-tested, but the README says none of it has been checked by playing in the Unity editor. Don't assume that code which passes its tests works in play.

## Commands

A cloud or Linux session has no Unity editor, so the checks you can run are the typecheck and the Python tests.

```bash
# Compile every assembly against the Unity 6 managed DLLs and run the EditMode tests on .NET 8 (no license needed).
# The first run downloads the Unity editor archive into .typecheck/ (gitignored).
# It prints "passed N, native-only N, failed N". Native-only tests call JsonUtility or AssetDatabase and don't fail the run.
Tools/Typecheck/run.sh

# Run one EditMode test on .NET after run.sh has built everything (same props that run.sh uses):
REPO_ROOT=$PWD dotnet test Tools/Typecheck/Runner/Runner.csproj --no-build \
  -p:UnityData=$PWD/.typecheck/unity/Editor/Data -p:PkgRoot=$PWD/.typecheck/pkgs \
  --filter "FullyQualifiedName~OutpostZero.Tests.EditMode.HudTests"

# Python tests (the same ones CI runs)
python3 -m unittest discover -s BlenderScripts -p "test_*.py"   # pipeline, manifest, texture and audit tests (needs numpy)
python3 -m unittest discover -s Tools/Release
python3 -m unittest discover -s Tools/Balance
python3 BlenderScripts/material_library.py --check   # the committed material library must match its generator
python3 Tools/Audio/make_mixer.py --check            # the committed OutpostMixer must match its generator

# Asset pipeline (needs Blender 4.2)
blender -b -P BlenderScripts/pipeline.py -- --changed          # rebuild only the stale entries
blender -b -P BlenderScripts/pipeline.py -- --only <AssetName>
python3 BlenderScripts/pipeline.py --changed --dry-run         # list the stale assets without Blender

# Releases (bump never tags or pushes)
python3 Tools/Release/release.py bump --dry-run
python3 Tools/Release/release.py changelog
```

With Unity installed:

```bash
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testResults editmode.xml -testFilter <Namespace.Class[.Method]>
Unity -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults playmode.xml
Unity -batchmode -quit -projectPath . -executeMethod OutpostZero.EditorTools.PrototypeSceneBuilder.BuildAndSaveSceneBatch
Unity -batchmode -nographics -quit -projectPath . -executeMethod OutpostZero.EditorTools.BuildScript.BuildAll -targets linux
```

In the editor, **Tools > Outpost Zero > Build Prototype Test Arena** does three things: it generates the default weapon and zombie data, builds `PrototypeArena.unity`, and sets the build order to Boot, then Arena. The NavMesh must then be baked by hand.

CI: `typecheck.yml` and `blender-assets.yml` run on every push and PR and need no secrets. `unity-ci.yml` fails at license activation until the `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` secrets are set. The Blender job fails when committed FBX or PNG files are stale or don't match a fresh rebuild byte for byte.

## Architecture

Assemblies: the runtime assembly `OutpostZero` (`Assets/Scripts/OutpostZero.asmdef`), the editor assembly `OutpostZero.Editor`, and the test assemblies `OutpostZero.Tests.EditMode` and `OutpostZero.Tests.PlayMode`. Each folder under `Assets/Scripts` is one namespace, `OutpostZero.<Folder>`, except `Editor/`, whose namespace is `OutpostZero.EditorTools`. `docs/ARCHITECTURE.md` has the namespace table, the state diagram and the list of events.

- **Rules live in static classes that don't use Unity, and MonoBehaviours stay thin.** Most gameplay logic is in small `static` classes such as `DodgeClock`, `CampaignBoard` and `NewGamePlan`, which EditMode tests call directly. That is why the .NET typecheck can run the tests. Put new logic in the same kind of class and test it there.
- **Services** are MonoBehaviour singletons with a static `Instance`. `GameSystemsInstaller.Install(scene)` attaches about 30 of them to the `DontDestroyOnLoad` `GameManager`. It also outfits the player and adds the HUD, UI and camera rig to the main camera. The saved arena scene predates most of these components, so they are added at runtime rather than stored in the scene. Always call a service null-safe (`SaveSystem.Instance?.Save()`), because the tests and the Boot scene run without services.
- **Game state**: `GameManager.SetState` is the only place state changes. It sets `Time.timeScale = 0` for the menu, pause, results, succession, game-over and victory states, and it raises `OnGameStateChanged`.
- **Scenes and flow**: `Boot.unity` (build index 0) streams in `PrototypeArena.unity`. The menu, camp, expedition and results screens are all game states inside that one arena scene. Every change between screens goes through `SceneFlow.Travel`, which runs the loading card and then calls `OnExit`/`OnEnter(FlowContext)` on each registered `ISceneEntry`, `GameManager` among them.
- **Data**: weapons (`WeaponDefinition`) and zombies (`ZombieArchetype`) are assets under `Assets/Data`, generated by `DefaultDataGenerator`. Items and loot tables are listed by `Assets/Resources/ItemDatabase.asset`. `ItemDatabase` copies them into `ItemCatalog` and `LootTables`, whose code records are the fallback that keeps saves and pure tests working without imported assets. Districts, recipes and strings are defined in code.
- **Saves**: `SaveSystem` writes JSON with `JsonUtility`, one file per slot (`slot_auto.json`, `slot_N.json`). `SaveCodec` packs every saved system into that schema, so a new persisted field must go through it.
- **Asset pipeline**: `BlenderScripts/assets.manifest.json` lists each asset and its generator. `pipeline.py` resets the scene, calls the generator, and handles the pivot, rig, LODs, export, texture baking and sidecars. A generator (`build_<thing>(ctx)` in a category module) only builds geometry and returns the mesh; it never resets the scene or exports. In Unity, `FbxPrefabPostprocessor` turns imported FBX files into prefabs and materials. `docs/ASSET_PIPELINE.md` covers naming, units, pivots and the steps for adding a new asset.

## Conventions

- **Player-facing strings** go through `Loc.T(key)`. Add the English and Spanish entries together in `Shell/Localization.cs`. A duplicate key stops the whole table from loading.
- **Components that may be missing** are added with `Attach.Ensure<T>(go)`. Never write `GetComponent<T>() ?? AddComponent<T>()`: Unity's fake null makes `??` skip the add.
- **Float timers and ranges** compare at inclusive edges with `Tick.Past` / `Tick.Slack`.
- **New files under `Assets/`** need a `.meta` file with a unique 32-character hex GUID, because nothing generates one without Unity. When you change generated assets, commit the FBX, textures, sidecar, `.meta` files and `Assets/Models/.pipeline-cache.json` together.
- **Commits** follow Conventional Commits: `type(scope): imperative sentence`. The types are `feat`, `fix`, `perf`, `refactor`, `docs`, `test`, `build`, `ci` and `chore`, and the scope is often a Linear id (`feat(PRO-57): ...`). `release.py` builds the changelog and version bump from these prefixes.
- **Branches** use the lowercase branch name Linear generates for the issue.
