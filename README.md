# Outpost Zero — 3D Top-Down Zombie Survival & Colony Simulator

A 3D top-down / isometric post-apocalyptic survival game built in Unity, combining twin-stick tactical scavenging expeditions, deep RimWorld-style survivor social sandbox management, modular grid base construction, and community succession permadeath.

---

## Quick Start Guide

### 1. Open the Project in Unity
- Open **Unity Hub** (or run `unity open C:\Users\User\.gemini\antigravity\scratch\OutpostZero`).
- Select **Add Project from Disk** and select `C:\Users\User\.gemini\antigravity\scratch\OutpostZero`.
- Use **Unity 6 LTS** or **Unity 2022.3+ LTS** with the Universal Render Pipeline (URP).

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

## Roadmap Ahead

- **Phase 2 (Colony Management)**: Survivor trait generator (Medic, Engineer, Cook), hunger/morale/fatigue needs, autonomous schedule routines, and interpersonal relationships.
- **Phase 3 (Sanctuary Construction)**: Freeform grid base building (barricades, watchtowers, hydroponic farms, water purifiers, generators).
- **Phase 4 (Factions & Vendors)**: Outpost trading bazaars, wandering merchant caravans, and dynamic faction reputation.
