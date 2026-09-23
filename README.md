# Outpost Zero — 3D Top-Down Zombie Survival & Colony Simulator

A 3D top-down / isometric post-apocalyptic survival game built in Unity, combining twin-stick tactical scavenging expeditions, deep RimWorld-style survivor social sandbox management, modular grid base construction, and community succession permadeath.

---

## Quick Start Guide

### 1. Open the Project in Unity
- Open **Unity Hub** and add this repository from disk.
- Use **Unity 6000.0.83f1** with the Universal Render Pipeline (URP).
- Regenerate models with Blender 4.2 LTS. Commands are in `BlenderScripts/README.md`.

### 2. Generate the Prototype Sandbox (One-Click)
1. Open a new scene (or default scene).
2. From the Unity top menu bar, click:
   **`Tools > Outpost Zero > Build Prototype Test Arena`**
3. Open the Navigation window (**Window > AI > Navigation**).
4. Click **Bake** to generate the NavMesh on the ground and around urban obstacles.
5. Press **Play**! Playing the arena directly opens the main menu. To test a cold start, open `Assets/Scenes/Boot.unity` (build index 0) and press Play: it streams the arena behind the loading card and logs how long the menu took to appear against the 5 s budget.

---

## Controls & Keybindings

Keyboard actions can be rebound in **Settings**. Gamepad buttons can be rebound there too.

| Keyboard / mouse | Action |
|---|---|
| **W A S D** | Move |
| **Mouse** | Aim; the torso turns to the cursor |
| **Left mouse** | Fire or swing the current weapon |
| **Right mouse** | Aim down sights |
| **Left Shift** | Sprint (drains stamina, loud footsteps) |
| **C / Left Ctrl** | Crouch (quiet footsteps, harder to see) |
| **Space** | Dodge roll (brief invulnerability, costs stamina) |
| **R** | Reload |
| **E** | Interact: loot, doors, stalls, pick up bodies |
| **Q** | Use a medkit |
| **F** | Flashlight |
| **G** | Throw the selected throwable |
| **V** | Stealth takedown |
| **B** | Build mode in camp |
| **Tab / I** | Inventory |
| **1 – 4** | Weapon slots (pistol, shotgun, machete, assault rifle) |
| **5 – 8** | Belt quick-use slots |
| **Mouse wheel** | Cycle weapons |
| **Middle mouse / hold Z** | Weapon wheel |
| **Escape** | Pause menu |
| **F3** | AI watch overlay (zombie state and targets) |
| **F4** | Roster sheet (editor and development builds only): every survivor's traits, skills, needs, wounds, task and opinion |
| **F9** | Dev menu (editor and development builds only): scene jump, god mode, spawn zombies, supply kit, skip 6 hours, reboot through Boot |

| Gamepad | Action |
|---|---|
| **Left stick / click** | Move / sprint |
| **Right stick / click** | Aim / crouch |
| **Right trigger** | Fire |
| **Left trigger** | Aim down sights |
| **Right shoulder** | Dodge |
| **Left shoulder (hold)** | Weapon wheel |
| **South (A / Cross)** | Interact |
| **West (X / Square)** | Reload |
| **North (Y / Triangle)** | Medkit |
| **East (B / Circle)** | Throw |
| **D-pad up** | Flashlight |
| **D-pad down** | Build |
| **D-pad left / right** | Previous / next weapon |
| **Select** | Inventory |
| **Start** | Pause |

---

## Feature status by milestone

The plan lives in the Linear project [Outpost Zero — Full Playable Game](https://linear.app/co-projects/project/outpost-zero-full-playable-game-4362cca50e59) (milestones M0–M5, issues PRO-10 to PRO-70). "In code" below means the system exists, is wired into the game, and its rules are covered by EditMode tests. None of it has been checked by playing in the Unity editor yet.

| Milestone | Systems | Status |
|---|---|---|
| **M0 — Playability & foundations** | Physics layers and hit masks, loot pickup, medkit binding, `WeaponDefinition` / `ZombieArchetype` data, repo-relative Blender paths, CI workflows | In code; Unity CI waits on license secrets |
| **M1 — Gameplay** | Loot containers and item database, pistol / shotgun / SMG / rifle / machete with mods and suppressors, hit feedback, explosive barrels, runner lunge and brute charge, horde director, stealth and light detection, hunger / thirst / fatigue / injuries / infection, objectives and extraction, Input System with gamepad, inventory with belt slots | In code |
| **M2 — Graphics** | URP post-processing, day-night and weather, decals, muzzle / tracer / explosion VFX, triplanar rim material, street dressing and skyline, character detail, Cinemachine follow rig, UI Toolkit HUD, LOD governor and perf budget | In code |
| **M3 — Asset pipeline** | Per-asset manifest with `pipeline.py` runner (`--only`, `--category`, `--changed`), reproducible FBX bytes, sidecars, LODs, UVs and baked texture sets, character armatures and clips, 2 m snap kit, FBX → prefab and material postprocessor, asset audit tests, docs, `BuildScript` for Windows / Linux / macOS players with semantic versions, tag-triggered releases, generated changelog, session logs, screenshot tour | In code; players and releases need the Unity secrets |
| **M4 — Sanctuary & colony** | Survivor roster with traits and skills, camp management and world clock, needs / morale / relationships, grid building, crafting, succession with corpse recovery and memorials, night raids, merchants, caravans and factions | In code |
| **M5 — Shell & release** | Boot scene, main menu with New Game (difficulty, permadeath, seed), save slots and autosave, settings and rebinding, adaptive audio, tutorial and codex, 10-district campaign with a radio-tower win and endless mode, English / Spanish, dev menu | In code; menu, camp and expedition still share the arena scene |

## Documentation

- [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) — namespaces, services, data flow, state machine, events
- [docs/ASSET_PIPELINE.md](docs/ASSET_PIPELINE.md) — manifest, generators, naming, units, adding an asset
- [docs/CONTRIBUTING.md](docs/CONTRIBUTING.md) — setup, building, tests, CI, conventions
- [docs/QA_CHECKLIST.md](docs/QA_CHECKLIST.md) — playtest checklist
- [docs/TUNING.md](docs/TUNING.md) — horde pacing target and difficulty rows
- [docs/AUDIO_ATTRIBUTION.md](docs/AUDIO_ATTRIBUTION.md) — sound credits
- [docs/ACCESSIBILITY.md](docs/ACCESSIBILITY.md) — accessibility review

---

## Continuous integration

GitHub Actions workflows:

- `.github/workflows/unity-ci.yml` — EditMode tests, PlayMode smoke test, headless scene build, Linux player. Windows player builds run on version tags.
- `.github/workflows/blender-assets.yml` — path tests plus a Blender 4.2 headless asset build.
- `.github/workflows/typecheck.yml` — no license needed. Compiles the runtime, editor, and test assemblies against the Unity 6 managed DLLs and the real Input System and AI Navigation sources, then runs the EditMode tests on .NET. Tests that reach native engine calls (`JsonUtility`, `AssetDatabase`) are listed but do not fail the job. Run it locally with `Tools/Typecheck/run.sh` (needs the .NET 8 SDK; the first run downloads the Unity editor archive to extract its managed DLLs).

Repository secrets required for the Unity jobs:

| Secret | Purpose |
|---|---|
| `UNITY_LICENSE` | Unity license file contents (or the license string GameCI expects) |
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |

See [GameCI](https://game.ci/) for activating a personal or serial license. Without those secrets the Unity jobs cannot start; the Blender path tests do not need them.

## License

Copyright (c) 2026 covisualize. All rights reserved. The source is published for reference only; see [LICENSE](LICENSE). Third-party components are listed in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
