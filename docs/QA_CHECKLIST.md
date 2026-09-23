# Outpost Zero release QA checklist

This is the gate for a release candidate. The cloud agents have no Unity editor or licence, so every box below needs a person with the editor or a built player. EditMode tests cover the logic and are listed under **Automated** so you don't redo them by hand. Check a box only after you have watched it happen.

Record the build under test first:

| Field | Value |
| --- | --- |
| Version (`version.json` or the menu corner) | |
| Commit | |
| Tester | |
| Date | |

## 1. Automated gates

Run these before playing. A red gate blocks the release.

```powershell
# Windows (PowerShell). The typecheck needs bash, so run it from Git Bash or WSL.
bash Tools/Typecheck/run.sh
py -m unittest discover -s Tools\Release
py -m unittest discover -s Tools\Balance
```

```bash
# Linux / macOS
Tools/Typecheck/run.sh
python3 -m unittest discover -s Tools/Release
python3 -m unittest discover -s Tools/Balance
```

- [ ] The typecheck harness builds every assembly and EditMode reports `failed 0`.
- [ ] GameCI `editmode`, `playmode` and the Linux player job are green. These need the `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` secrets.
- [ ] The PR carries the screenshot-tour comment with six fresh captures (overview, clear, fog, rain, overcast, night storm).
- [ ] The nightly soak (`soak.yml`) passed within the last 24 h: 30 min at 4x, memory stays within 1.10x of baseline, and there are no errors.
- [ ] `Tools/Balance/summarize.py` on the soak or playtest CSVs hits the Survivor targets: first death on days 4 to 7 and a win on days 25 to 35.

## 2. Full loop, menu to win

Play this in a player build, not the editor. Use Survivor difficulty and a fixed seed, and write the seed down.

- [ ] Cold start reaches the main menu in under 5 s. The menu camera drifts over the dusk street.
- [ ] New Game: pick a slot, difficulty and seed. The loading card shows, then the Sanctuary.
- [ ] Day 1 tutorial lines explain noise, stealth, loot and extraction, and each one shows only once.
- [ ] Assign camp tasks, then launch an expedition with a loadout.
- [ ] On the street, complete the objective, loot and reach extraction. The results card shows kills, haul and time.
- [ ] Back in camp the haul is in storage. Advance the day and needs, morale and stores move.
- [ ] Survive a night raid. Barricade damage and injuries carry into the next day.
- [ ] Save & Quit from the pause menu. Relaunch, choose Continue, and the day, roster, storage, base and district match.
- [ ] Lose a leader on purpose. The successor wakes at the gate, the corpse is on the map and its gear can be recovered, and the memorial lists the fallen.
- [ ] Open districts along the road until all ten are open, then clear the tower. The victory screen shows and the run ends.
- [ ] Start a new game and kill the whole roster. The outpost-falls screen is the only way forward.

## 3. Per-issue acceptance

**Automated** means an EditMode test in `Assets/Tests/EditMode` already proves the logic. **Play** means someone has to watch it.

### M0: Playability blockers and foundations

- [ ] PRO-16 **Play**: the pistol kills a Walker in 2 shots, shotgun pellets register, and the machete kills in 2 swings. A zombie in Wander behind `Building_Warehouse_NE` does not start chasing from sight alone.
- [ ] PRO-17 **Play**: rebuilding the arena logs no `[PrototypeSceneBuilder] No prefab or model` errors.
- [ ] PRO-18 **Play**: walking over the medkit raises the HUD count. Q heals 50 HP. An ammo box adds reserve to the matching gun.
- [ ] PRO-19 **Play**: the Profiler shows zero GC alloc per frame from `ZombieAI.Update` in steady state, and `Player.log` has no stack traces in normal combat.
- [ ] PRO-20 **Automated** (weapon and archetype values) and **Play** (tuning a `WeaponDefinition` in the Inspector changes the gun).
- [ ] PRO-21 **Play**: clone to a fresh path and regenerate all models headless with exit code 0, as in `docs/ASSET_PIPELINE.md`.
- [ ] PRO-22 **Play**: a no-op PR is green, and a PR that breaks the compile or removes a model goes red.

### M1: Gameplay mechanics

- [ ] PRO-23 **Play**: E opens containers, weight limits pickup, and dropping an item puts it on the ground.
- [ ] PRO-24 **Play**: the rifle fires full-auto with growing spread. Suppressed pistol shots don't raise a loud-noise alert. Swapping to a gun on the ground works.
- [ ] PRO-25 **Play**: with your eyes shut, each weapon is recognisable from its shake and hit-stop. Every hit gets a visible reaction.
- [ ] PRO-26 **Play**: the red barrel next to 3 zombies kills them, alerts the map and brings reinforcements. Barrels chain. The toxic cloud ticks.
- [ ] PRO-27 **Play**: a Runner lunge can be dodged. A Brute charge breaks a wood barricade. A scream pulls a group through an open door but not through a wall.
- [ ] PRO-28 **Play**: no zombie is ever seen spawning, pressure ebbs and flows, and Nightmare is clearly harder than Scavenger.
- [ ] PRO-29 **Play**: crouched with the light off at night, you pass 5 m from a Walker unseen. Turning the flashlight on gets you spotted. A lure thrown 15 m (G) pulls a group away. V takes down an unalerted zombie from behind.
- [ ] PRO-30 **Play**: hunger, thirst, fatigue, bleeding and infection each visibly change play. Medical items cure them.
- [ ] PRO-31 **Play**: start, complete the objective, extract, see the results, and return to camp with the inventory kept.
- [ ] PRO-32 **Play**: every state animates, the reload matches its clip, and feet don't slide at walk speed.
- [ ] PRO-33 **Automated** (`SourceHygieneTests`: no legacy `Input.` calls) and **Play**: a full expedition works on an Xbox pad, a PlayStation pad and keyboard and mouse.
- [ ] PRO-34 **Play**: Tab opens the pack. Use, drop and equip work with the mouse and the pad. The weight bar is right and every item has an icon. The Show button cycles categories, the equipped block lists the four weapon slots, belt and pack tier, and Stow moves a stack into an open crate.

### M2: Graphics detail

- [ ] PRO-35 **Play**: contact shadows ground the props, and Medium holds 60 fps. Compare with the tour screenshots.
- [ ] PRO-36 **Play**: an expedition runs from afternoon to night, lamps pool on wet asphalt, and the flashlight casts shadows. No light leaks through `Building_*` walls.
- [ ] PRO-37 **Play**: rain plays and looks different from clear weather, and fog hides the map edge.
- [ ] PRO-38 **Play**: after a firefight the street and the walls behind kills are marked, and the decal budget recycles without popping.
- [ ] PRO-39 **Play**: every shot, hit, kill and explosion has distinct VFX, and the VFX pool counts in the Profiler's hierarchy don't climb.
- [ ] PRO-40 **Play**: no grey default materials, tiling and normal detail show under the flashlight, and the Frame Debugger shows SRP-batched draws.
- [ ] PRO-41 **Play**: any screenshot is recognisably commercial, industrial, ruins or camp.
- [ ] PRO-42 **Play**: three Walkers side by side look different, and their eyes show at night before their bodies do.
- [ ] PRO-43 **Play**: follow, aim zoom and impulse shake feel right, and the camp overview camera works while placing modules.
- [ ] PRO-44 **Automated** (no `OnGUI`) and **Play**: the HUD is readable at every resolution in section 5, with no per-frame GC from UI.
- [ ] PRO-45 **Automated** (`PerfTests`, category `Perf`, in the PlayMode CI job: 30 zombies on the street for 600 frames, median within the Medium 16.6 ms budget, p95 within twice that, no frame over 250 ms, at most 16 KB GC per frame; the `[Perf]` log line keeps the numbers) and **Play**: under 300 draw calls at Medium in the busiest raid, as shown in the Frame Debugger.

### M3: Artifacts and asset pipeline

- [ ] PRO-46 **Play**: `blender -b -P BlenderScripts/pipeline.py -- --only Prop_Dumpster` finishes in under 10 s, and a full rebuild of unchanged generators matches byte for byte.
- [ ] PRO-47 **Automated** (textures and UVs per manifest entry) and **Play**: the flashlight reveals normal detail on the sedan and the brick wall.
- [ ] PRO-48 **Play**: every character imports as Humanoid with no avatar errors, all clips play without tearing, and the Walker still shambles.
- [ ] PRO-49 **Automated** (a kit assembles an enterable three-storey block; `KitMeshTests` and `test_kit_prefab_set.py` check that every catalog piece resolves to its baked prefab, the mesh half-turn lands on the catalog boxes, and the assembler snaps to the grid) and **Play**: the storefront and warehouse interiors can be entered and show the baked kit meshes. *Tools > Outpost Zero > Kit Assembler* places pieces and exports a recipe.
- [ ] PRO-50 **Automated** (`ItemDatabaseTests`: every item has a definition, FBX, prefab and 64 px rendered icon, all linked; `ItemDatabase.Get("ammo_9mm")`) and **Play**: icons render in the pack and a dropped item lands as its model, not a cube.
- [ ] PRO-51 **Automated** (the scene builder has no FBX paths, every builder id has a prefab, every prefab carries its SurfaceTag and layer, and zombies come from `Assets/Prefabs/Enemies/*_Actor.prefab`) and **Play**: reimporting an FBX regenerates its prefab and keeps hand edits, footsteps change between concrete, wood and metal, and after **Build Prototype Test Arena** the scene has no parked zombie prototypes.
- [ ] PRO-52 **Play**: deleting a texture or renaming an FBX turns CI red with a clear message, and the suite runs in under 60 s. (Covered off-engine by `Artifacts/ArtifactSuiteTests`; confirm the same in the Unity job.)
- [ ] PRO-53 **Play**: pushing a `v*` tag produces three zips that launch to the menu, and the version in the menu matches the tag.
- [ ] PRO-54 **Play**: a new contributor can clone, regenerate assets, build the scene and run the tests from the docs alone.

### M4: Sanctuary and colony

- [ ] PRO-55 **Automated** (two seeds give different rosters) and **Play**: leader skills change expedition play.
- [ ] PRO-56 **Play**: run 5 days in a row, each with tasks, an expedition and a return, and see events fire.
- [ ] PRO-57 **Play**: survivors walk between modules. A starving, grieving camp collapses in about 3 days and a well-run one thrives.
- [ ] PRO-58 **Play**: B places walls, a generator with lights, a farm and a purifier. Each changes the daily numbers and raid behaviour.
- [ ] PRO-59 **Play**: crafting solves ammo scarcity at a real material cost, and T2 recipes need base investment.
- [ ] PRO-60 **Play**: see the leader death in section 2.
- [ ] PRO-61 **Play**: a day 3 raid against a weak wall can be lost, while walls, guards and lights make day 10 survivable.
- [ ] PRO-62 **Play**: scrap buys from the merchant, caravan visits can be planned around, and reputation changes prices and unlocks stock.

### M5: Shell and release

- [ ] PRO-63 **Play**: see the full loop in section 2. Esc backs out of every menu one level at a time.
- [ ] PRO-64 **Automated** (round trip, `.bak` recovery, v1 fixture migration) and **Play**: quit at each camp phase and Continue restores it.
- [ ] PRO-65 **Automated** (`SettingsFlowTests`: render scale round-trips, closing with changes asks keep/revert, focus loss mutes, fixed and taken keys are refused with a reason, and `{key:Action}` tokens in hints, lessons and the interact prompt follow a rebind) and **Play**: every setting survives a restart and applies at once. Rebinding a key updates the HUD prompts.
- [ ] PRO-66 **Automated** (audio buses and clip validation; `AudioCoverageTests` checks every played, computed and mixer-named id has a clip, and that `SfxLibrary` ids are real) and **Play**: playing blind, you can tell the weapon, surface, zombie type, distance and tension.
- [ ] PRO-67 **Automated** (`TutorialFlowTests`: the Day 1 camp track (assign a task, read the stores, place a barricade, craft a bandage, launch) and the expedition track advance in order; every gate is emitted somewhere; the camp track is remembered in the codex; prompts show pad buttons while a pad is in use; each Day 1 step rings the control it names and dims the other camp rows; every zombie, module and faction codex entry shows its rendered icon; the first expedition is a quiet street with three scripted walkers ahead of the start and no waves; the crouch, reload and flashlight hints arrive when a zombie is near, the magazine runs dry and you step into a dark room) and **Play**: 3 fresh testers finish Day 1 and the first expedition with no README.
- [ ] PRO-68 **Automated** (same seed gives the same street, blocks stay walkable) and **Play**: a campaign win takes about 6 to 8 hours.
- [ ] PRO-69 **Play**: the game is playable in each colour-blind mode (off, blue-yellow, mono) and with sound off using captions. Switching EN/ES re-renders every screen.
- [ ] PRO-70 **Automated** (balance CSV rows) and this checklist is complete.

## 4. Device matrix

Run section 2 at least once per row, and a 20 minute expedition on the others.

| Platform | Hardware | Input | Tier | Result |
| --- | --- | --- | --- | --- |
| Windows 11 | Discrete GPU (GTX 1060 class or better) | Keyboard and mouse | High | |
| Windows 11 | Integrated GPU (Iris Xe class) | Keyboard and mouse | Low | |
| Windows 11 | Any | Xbox controller | Medium | |
| Windows 11 | Any | PlayStation controller | Medium | |
| Ubuntu 22.04+ | Discrete GPU | Keyboard and mouse | Medium | |
| macOS 13+ | Apple Silicon | Keyboard and mouse | Medium | |
| Steam Deck | Deck (Proton or native Linux) | Built-in pad | Low | |

## 5. Resolutions

Check the HUD, pause menu, settings, pack and results card at each size. Text must not clip or overlap, and there must be no stretched UI.

- [ ] 1280 x 720
- [ ] 1366 x 768
- [ ] 1920 x 1080
- [ ] 2560 x 1440
- [ ] 3840 x 2160
- [ ] 1280 x 800 (16:10, Steam Deck)
- [ ] 2560 x 1080 (21:9)
- [ ] Windowed resize, and switching fullscreen on and off while playing
- [ ] Largest UI scale at 1280 x 720

## 6. Quality tiers

Use the busiest raid and a rainy night street for each tier. Read fps and draw calls from the Game view Stats panel, or from the Profiler attached to a development player.

| Tier | Target | Zombie cap | Busiest raid fps | Rainy night fps | Draw calls | Result |
| --- | --- | --- | --- | --- | --- | --- |
| Low | 30 fps, integrated GPU | 16 | | | | |
| Medium | 60 fps | 32 | | | < 300 | |
| High | 60 fps | 32 | | | | |
| Ultra | 60 fps | 40 | | | | |

- [ ] Changing the tier while playing takes effect without a restart and survives a relaunch.
- [ ] The performance warning stays quiet on Medium with the default horde.

## 7. Logs and crash handling

- [ ] `Logs/outpost.log` under the persistent data folder rotates, keeping 5 files of up to 2 MB each.
- [ ] A forced exception is written to the log, and the next launch warns `[CrashLog] The last session ended badly`.
- [ ] Crash upload stays off unless the `outpost.crash_upload` PlayerPrefs key is set to 1.
- [ ] With `-telemetry` on a release build, `Telemetry/expeditions.csv` and `Telemetry/days.csv` fill in.

## Sign-off

A release candidate ships when sections 1 and 2 are fully checked, every row in sections 4 and 6 has a result, and any unchecked item in section 3 has a linked issue.

| Role | Name | Date |
| --- | --- | --- |
| QA | | |
| Owner | | |
