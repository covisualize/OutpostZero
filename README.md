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
5. Press **Play**!

---

## Controls & Keybindings

| Key / Action | Function |
|---|---|
| **W, A, S, D** | 8-Directional Character Movement |
| **Mouse Cursor** | Raycast Ground Cursor Aiming & Torso Rotation |
| **Left Mouse Button (LMB)** | Attack (Fire current firearm or swing melee weapon) |
| **Right Mouse Button (RMB)** | Aim Down Sights (ADS zoom & camera focus) |
| **Left Shift** | Sprint (Drains stamina, emits loud running noise) |
| **Left Control / C** | Crouch / Sneak (Quiet footsteps, reduced zombie sight detection) |
| **R** | Reload weapon magazine |
| **1, 2, 3 / Mouse Wheel** | Select Weapon (1: 9mm Pistol, 2: Shotgun, 3: Combat Machete) |
| **F** | Toggle Tactical Flashlight |
| **Q** | Use Medical Kit (Restores 50 HP) |
| **Escape** | Pause Expedition / Open Options |

---

## Core Systems Implemented (Phase 1)

1. **Twin-Stick Player Controller**: Smooth movement, stamina drain/recovery, cursor aiming, weapon handling, and crouching.
2. **Tactical Noise & Acoustic Physics (`NoiseManager`)**:
   - Footsteps, melee swings, and gunshots emit expanding acoustic waves.
   - Sound muffles through solid building walls.
   - Dynamic real-time noise meter on the HUD displays current sound footprint.
3. **Weapon Arsenal**:
   - **Tactical 9mm Pistol**: Moderate damage, quick reload, 22m sound radius.
   - **Remington 870 Shotgun**: High spread pellet blast, massive knockback, 38m loud noise radius.
   - **Steel Machete**: High close-range slash damage, stamina cost, silent (2.5m noise radius).
4. **NavMesh Zombie Swarm AI (`ZombieAI` & `ZombieSpawner`)**:
   - Sensory perception: 110-degree vision cone + sound hearing (`INoiseListener`).
   - State machine: `Idle`, `Wander`, `InvestigateNoise`, `Chase`, `Attack`, `Stunned`, `Dead`.
   - Swarm alerting: Zombies emit a distress/attack call alerting nearby dormant zombies.
5. **Community Succession Permadeath**:
   - When the active expedition leader is killed, the death overlay activates.
   - You assume command of the next survivor from your sanctuary community.
6. **Survival HUD**:
   - Real-time Health, Stamina, Weapon Ammo/Reload status, Acoustic Noise Bar, and Scrap count.

---

## Continuous integration

GitHub Actions workflows:

- `.github/workflows/unity-ci.yml` — EditMode tests, PlayMode smoke test, headless scene build, Linux player. Windows player builds run on version tags.
- `.github/workflows/blender-assets.yml` — path tests plus a Blender 4.2 headless asset build.

Repository secrets required for the Unity jobs:

| Secret | Purpose |
|---|---|
| `UNITY_LICENSE` | Unity license file contents (or the license string GameCI expects) |
| `UNITY_EMAIL` | Unity account email |
| `UNITY_PASSWORD` | Unity account password |

See [GameCI](https://game.ci/) for activating a personal or serial license. Without those secrets the Unity jobs cannot start; the Blender path tests do not need them.

## Roadmap Ahead

- **Phase 2 (Colony Management)**: Survivor trait generator (Medic, Engineer, Cook), hunger/morale/fatigue needs, autonomous schedule routines, and interpersonal relationships.
- **Phase 3 (Sanctuary Construction)**: Freeform grid base building (barricades, watchtowers, hydroponic farms, water purifiers, generators).
- **Phase 4 (Factions & Vendors)**: Outpost trading bazaars, wandering merchant caravans, and dynamic faction reputation.
