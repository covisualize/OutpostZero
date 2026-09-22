# Contributing

This guide takes you from a fresh clone to rebuilt assets, a built scene, and passing tests. For how the code fits together, see [ARCHITECTURE.md](ARCHITECTURE.md). For models and textures, see [ASSET_PIPELINE.md](ASSET_PIPELINE.md).

## Tools

| Tool | Version | Needed for |
|---|---|---|
| Unity | **6000.0.83f1** (Unity 6), installed through Unity Hub | Opening the project, building the scene, Test Runner |
| Blender | **4.2 LTS** or newer | Regenerating models (optional; the FBX files are committed) |
| Python | 3.10+ | Pipeline tests that don't need Blender |
| .NET SDK | 8.0 | The license-free typecheck (optional) |
| Git | any recent | Cloning; FBX and PNG files are plain Git objects, no LFS |

The project uses URP, the Input System, AI Navigation, Cinemachine and the Unity Test Framework. Unity Hub resolves them from `Packages/manifest.json` on first open.

## 1. Clone and open

```powershell
git clone https://github.com/covisualize/OutpostZero.git
cd OutpostZero
```

In Unity Hub, choose **Add > Add project from disk**, pick the clone, and open it with 6000.0.83f1. The first import takes a few minutes. `FbxPrefabPostprocessor` creates the prefabs and materials as the models import.

## 2. Regenerate assets (optional)

Windows (PowerShell):

```powershell
& "C:\Program Files\Blender Foundation\Blender 4.2\blender.exe" -b -P BlenderScripts\build_all_assets.py
& "C:\Program Files\Blender Foundation\Blender 4.2\blender.exe" -b -P BlenderScripts\build_all_assets.py -- --only characters,weapons
```

Linux / macOS (bash):

```bash
blender -b -P BlenderScripts/build_all_assets.py
blender -b -P BlenderScripts/build_all_assets.py -- --only props --models-dir /tmp/outpost-models
```

Categories are `characters`, `weapons`, `architecture`, `props`, `kit`, `base`. Output goes to `Assets/Models/<Category>`. Set `OUTPOST_MODELS_DIR` or pass `--models-dir` to write somewhere else.

## 3. Build the scene

1. In Unity, choose **Tools > Outpost Zero > Build Prototype Test Arena**. This generates the default weapon and zombie data, configures URP and decals, builds `Assets/Scenes/PrototypeArena.unity`, and sets build settings to `Boot.unity` then `PrototypeArena.unity`.
2. Open **Window > AI > Navigation** and click **Bake**.
3. Press **Play** in the arena to go straight to the main menu. To test a cold start, open `Assets/Scenes/Boot.unity` and press Play.

Headless, the same build runs through `-executeMethod`:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.0.83f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod OutpostZero.EditorTools.PrototypeSceneBuilder.BuildAndSaveSceneBatch -logFile build.log
```

`PrototypeSceneBuilder.CiBuildLinuxPlayer` and `BuildGameExecutable` build the scene and then a Linux or Windows player into `Builds/`.

## 4. Run tests

**Unity Test Runner.** Open **Window > General > Test Runner** and run **EditMode** (`Assets/Tests/EditMode`) and **PlayMode** (`Assets/Tests/PlayMode`). From the command line:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.0.83f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform EditMode -testResults editmode.xml -logFile editmode.log
& "C:\Program Files\Unity\Hub\Editor\6000.0.83f1\Editor\Unity.exe" -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults playmode.xml -logFile playmode.log
```

**Typecheck without Unity.** `Tools/Typecheck/run.sh` compiles every assembly against the Unity 6 managed DLLs and runs the EditMode tests on .NET 8. It needs bash (on Windows, use Git Bash or WSL). The first run downloads the Unity editor archive to extract its DLLs into `.typecheck/`.

```bash
Tools/Typecheck/run.sh
```

It prints `passed N, native-only N, failed N`. Native-only tests call engine code that only exists inside Unity (`JsonUtility`, `AssetDatabase`). They are listed but don't fail the run. Any compile error or real test failure exits non-zero.

**Pipeline tests without Blender:**

```powershell
cd BlenderScripts
python -m unittest test_paths.py test_manifest.py test_character_rig.py test_texture_set.py test_kit_catalog.py test_character_detail.py test_asset_audit.py
```

## 5. CI

Every push and pull request runs:

| Workflow | Jobs | Needs secrets |
|---|---|---|
| `typecheck.yml` | Compile against Unity 6 and run EditMode tests on .NET | No |
| `blender-assets.yml` | Repo-relative path and manifest tests; headless Blender 4.2 asset build | No |
| `unity-ci.yml` | EditMode tests, PlayMode smoke tests, headless scene validation and Linux player; Windows player on version tags | `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` |

A PR is ready when Typecheck and Blender assets pass and nothing new shows up in the Unity jobs. The Unity jobs fail at license activation until a maintainer adds the three secrets. See [GameCI activation](https://game.ci/docs/github/activation).

## 6. Conventions

**Branches.** Name feature branches after their Linear issue using its generated branch name, for example `covisualize/pro-63-main-menu-scene-flow-manager-boot-menu-sanctuary-expedition`. Use lower case only.

**Commits.** One logical change per commit. Write the subject as a plain imperative sentence that says what the game or code now does, for example `Give TargetDrop and FollowPull valid 32-character meta GUIDs`. Don't add a type prefix.

**Code.**

- One namespace per folder under `Assets/Scripts` (`OutpostZero.<Folder>`; `Editor/` is `OutpostZero.EditorTools`).
- Put rules in small static classes, and keep MonoBehaviours thin so EditMode tests can reach the logic.
- Every player-facing string goes through `Loc.T(key)`, with the English and Spanish entries added together in `Shell/Localization.cs`. Keys must be unique; a duplicate key stops the whole table from loading.
- Add components that might be missing with `Attach.Ensure<T>`, not `GetComponent<T>() ?? AddComponent<T>()`.
- Compare float timers and ranges at inclusive edges with `Tick.Past` or `Tick.Slack`.
- New scripts need a `.cs.meta` file with a unique 32-character hex GUID.
- Add an EditMode test for every new rule.

**Assets.** Follow the naming and pivot rules in [ASSET_PIPELINE.md](ASSET_PIPELINE.md), and commit generated textures and `.meta` files together with the FBX.

## Planning

The roadmap lives in the Linear project **Outpost Zero — Full Playable Game**, milestones M0–M5, issues PRO-10 to PRO-70. Reference the issue id in the PR description.
