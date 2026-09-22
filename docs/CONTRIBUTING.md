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
& "C:\Program Files\Blender Foundation\Blender 4.2\blender.exe" -b -P BlenderScripts\pipeline.py -- --changed
& "C:\Program Files\Blender Foundation\Blender 4.2\blender.exe" -b -P BlenderScripts\pipeline.py -- --only Prop_Dumpster
```

Linux / macOS (bash):

```bash
blender -b -P BlenderScripts/pipeline.py -- --changed
blender -b -P BlenderScripts/pipeline.py -- --category props --models-dir /tmp/outpost-models
```

`--changed` rebuilds only the entries whose generator, shared build code, or manifest entry moved since the last build. Commit the rebuilt FBX, textures, sidecars, and `Assets/Models/.pipeline-cache.json` together; the Blender CI job fails when they are stale. See [`BlenderScripts/README.md`](../BlenderScripts/README.md) for every flag.

## 3. Build the scene

1. In Unity, choose **Tools > Outpost Zero > Build Prototype Test Arena**. This generates the default weapon and zombie data, configures URP and decals, builds `Assets/Scenes/PrototypeArena.unity`, and sets build settings to `Boot.unity` then `PrototypeArena.unity`.
2. Open **Window > AI > Navigation** and click **Bake**.
3. Press **Play** in the arena to go straight to the main menu. To test a cold start, open `Assets/Scenes/Boot.unity` and press Play.

Headless, the same build runs through `-executeMethod`:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.0.83f1\Editor\Unity.exe" -batchmode -quit -projectPath . -executeMethod OutpostZero.EditorTools.PrototypeSceneBuilder.BuildAndSaveSceneBatch -logFile build.log
```

### Players

`BuildScript` builds players. In the editor, use **Tools > Outpost Zero > Build Players** and pick Windows, Linux, macOS, or all three (dev builds). Headless:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.0.83f1\Editor\Unity.exe" -batchmode -nographics -quit -projectPath . -executeMethod OutpostZero.EditorTools.BuildScript.BuildAll -targets windows,linux,mac -release -version v0.6.0 -logFile build.log
```

| Flag | Meaning |
|---|---|
| `-targets windows,linux,mac` | Which players to build. Leave it out for all three. |
| `-release` | IL2CPP, engine code stripping, and LZ4HC. Without it you get a Mono development build with LZ4. |
| `-version v0.6.0` | The version to stamp. Otherwise `GITHUB_REF_NAME`, then the latest `v*` tag, then `Assets/Resources/version.json`. Dev builds append `-dev+<short sha>`. |
| `-sha <commit>` | The commit to stamp. Defaults to `GITHUB_SHA`, then `git rev-parse HEAD`. |
| `-output Builds` | Output root. Players go to `Builds/Windows/OutpostZero.exe`, `Builds/Linux/OutpostZero.x86_64` and `Builds/macOS/OutpostZero.app` (Intel and Apple Silicon). |
| `-skipScene` | Reuse the saved arena instead of rebuilding it first. |

Each build writes `Assets/StreamingAssets/version.json` (version, commit, Unity version, channel), which the menu and credits read. It also writes `Builds/build-report.json` with the result, size and time for each player. Scripting backend, stripping and `bundleVersion` go back to their previous values afterwards, so a build leaves `ProjectSettings` unchanged. IL2CPP needs the player's own OS, so the release workflow builds each platform on its own runner.

### Screenshot tour and logs

Run a player with `-screenshotTour -screenshotDir shots -quitAfterTour`, or set `OUTPOST_TOUR=1`. It flies six fixed camera marks over 30 seconds (overview, street, sanctuary, rain, fog at dusk, night storm) and saves one PNG per mark plus `index.json`. F12 saves a screenshot at any time. The PlayMode test `ScreenTourTests` runs the same tour into `Artifacts/Screenshots`.

Every session writes `Logs/outpost.log` under `Application.persistentDataPath` (on Windows, `%USERPROFILE%\AppData\LocalLow\DefaultCompany\OutpostZero\Logs`). The last five sessions are kept, and a log rolls over at 2 MB. If the previous session logged an exception or never closed, the next start warns and points at `outpost.1.log`; attach that file to bug reports. Crash upload is opt-in (`outpost.crash_upload` in PlayerPrefs) and has no endpoint yet.

### Releases

```powershell
python Tools/Release/release.py bump --dry-run      # shows the next version from the commits since the last tag
python Tools/Release/release.py bump --pre rc       # e.g. 0.5.0 -> 0.6.0-rc.1
git commit -am "chore(release): v0.6.0-rc.1"
git tag v0.6.0-rc.1
git push origin v0.6.0-rc.1
```

`bump` picks major, minor or patch from the commits (a breaking change stays minor before 1.0), or takes `--major`, `--minor` or `--patch`. It writes the version to `Assets/Resources/version.json`, `ProjectSettings` `bundleVersion` and `SceneRoute.Version`, and regenerates `CHANGELOG.md`. It does not tag or push anything. `python Tools/Release/release.py changelog` regenerates the changelog alone.

Pushing the tag runs `release.yml`. It checks that the tag matches `version.json`, takes the notes from that version's `CHANGELOG.md` section, builds release players on Windows, Linux and macOS runners, and zips each player with `README.md`, `CHANGELOG.md`, `THIRD_PARTY_NOTICES.md` and `LICENSE`. Then it publishes a GitHub Release, marked as a prerelease when the tag has a `-` suffix. When a `BUTLER_API_KEY` secret and an `ITCH_TARGET` variable (`user/game`) are set, it also pushes each zip to itch.io.

**Hotfixes.** Branch `hotfix/v1.0.x` from the release tag, land `fix:` commits there (cherry-picked to `main` as well), then run `bump --patch` and tag from the hotfix branch.

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
python -m unittest discover -s . -p "test_*.py"
```

## 5. CI

Every push and pull request runs:

| Workflow | Jobs | Needs secrets |
|---|---|---|
| `typecheck.yml` | Compile against Unity 6 and run EditMode tests on .NET; release tool tests | No |
| `blender-assets.yml` | Pipeline and manifest tests; changed-asset plan and PR comment; headless Blender 4.2 `--changed` build; stale-model and byte-for-byte reproducibility checks | No |
| `unity-ci.yml` | EditMode tests; PlayMode smoke tests and the screenshot tour (uploaded and linked in a PR comment); headless scene validation and a Linux dev player | `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` |
| `release.yml` (on `v*` tags) | Release notes, Windows / Linux / macOS release players, zips, GitHub Release, optional itch.io | The Unity secrets; `BUTLER_API_KEY` optional |

A PR is ready when Typecheck and Blender assets pass and nothing new shows up in the Unity jobs. The Unity jobs fail at license activation until a maintainer adds the three secrets. See [GameCI activation](https://game.ci/docs/github/activation).

## 6. Conventions

**Branches.** Name feature branches after their Linear issue using its generated branch name, for example `covisualize/pro-63-main-menu-scene-flow-manager-boot-menu-sanctuary-expedition`. Use lower case only.

**Commits.** One logical change per commit, written as a [Conventional Commit](https://www.conventionalcommits.org): `type(scope): imperative sentence`, for example `fix(meta): give TargetDrop and FollowPull valid 32-character GUIDs`. The types are `feat`, `fix`, `perf`, `refactor`, `docs`, `test`, `build`, `ci` and `chore`. The scope is optional and can be a Linear id (`feat(PRO-53): ...`). Mark a breaking change with `!` or a `BREAKING CHANGE:` footer. The changelog and `bump` read these prefixes. Older subjects without one appear under "Other changes".

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
