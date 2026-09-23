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
  - Zombies drop from their archetype's loot table (`walker`, `runner`, `brute` in `Assets/Data/Loot`): walkers sometimes leave 3 scrap or a cloth, runners 2 scrap or a bandage, and brutes always leave 3 to 6 scrap with a chance of 9mm rounds or chemicals. Scrap lands as a walk-over pickup; the rest lands as items to take with E. Rounds picked up from the ground go into the matching gun, as they do from crates, and stay in the pack only when no gun takes them.
  - Automated: `ZombieLootTests` (every archetype names a table, drops are repeatable and never empty rolls, brutes always pay, walkers stay near the old scrap rate) and the PlayMode `PickupTests` (ground rounds raise the pistol's reserve; a full pack leaves a heavy pickup on the ground).
- [ ] PRO-24 **Play**: the rifle fires full-auto with growing spread. Suppressed pistol shots don't raise a loud-noise alert. Swapping to a gun on the ground works.
- [ ] PRO-25 **Play**: with your eyes shut, each weapon is recognisable from its shake and hit-stop. Every hit gets a visible reaction.
- [ ] PRO-26 **Play**: the red barrel next to 3 zombies kills them, alerts the map and brings reinforcements. Barrels chain. The toxic cloud ticks.
- [ ] PRO-27 **Play**: a Runner lunge can be dodged. A Brute charge breaks a wood barricade. A scream pulls a group through an open door but not through a wall.
- [ ] PRO-28 **Play**: no zombie is ever seen spawning, pressure ebbs and flows, and Nightmare is clearly harder than Scavenger.
- [ ] PRO-29 **Play**: crouched with the light off at night, you pass 5 m from a Walker unseen. Turning the flashlight on gets you spotted. A lure thrown 15 m (G) pulls a group away. V takes down an unalerted zombie from behind.
- [ ] PRO-30 **Play**: hunger, thirst, fatigue, bleeding and infection each visibly change play. Medical items cure them.
- [ ] PRO-31 **Play**: start, complete the objective, extract, see the results, and return to camp with the inventory kept.
  - Deviation: the results screen returns to camp and has no Retry button. Extraction clears the district, and the next trip goes out from camp so that the day, needs and night raid still pass in between.
- [ ] PRO-32 **Automated** (`AimRigTests`: aim weight per stance, the twist limit and yaw on the correct side, socket drift cap, reload clip speed and its late-only finish event, and the controller asset's `ReloadSpeed` wiring) and **Play**: every state animates, the reload clip ends as the rounds go in (also when wounded), and feet don't slide at walk speed. The chest and head turn toward the cursor up to 70 degrees ahead of the hips, and the gun moves with the right hand. After the first import, `Assets/Resources/Animators/` holds one controller per non-player character FBX. The player's controller keeps only survivor clips. Walkers, runners and brutes play their own walk, attack, hit and death clips. The damage from a bite lands on the attack clip's contact frame, once per swing (`EachCharacterModelGetsItsOwnController`, `ASwingBitesExactlyOnceWhetherTheClipOrTheClockLandsIt`). Feet hold on the ground at walk, sprint and crouch speed for the leader and at wander and chase speed for each zombie (`StrideSheet`, `CharacterMotionTests`). A pack of walkers idles and walks in three different ways, out of step. The brute roars before a charge, and the runner braces before a lunge. Footstep noise lands on the planted foot at the old pace. A zombie killed by a pipe bomb or an exploding barrel is thrown as a ragdoll that lands on the street and never blocks the player or bullets. Any other death plays one of three falls in place, and the body stops blocking at once. A pooled zombie comes back standing and animated. A weapon bought or taken from a loadout shows its model in the hand at the same grip as the starting weapons. The leader fires and reloads while walking without the legs stopping, and a zombie bites mid-stride. The swing shows only above the waist (`UpperBody` layer, `SwingsAndReloadsPlayAboveTheWaistWhileTheLegsKeepTheirGait`).
- [ ] PRO-33 **Automated** (`SourceHygieneTests`: no legacy `Input.` calls) and **Play**: a full expedition works on an Xbox pad, a PlayStation pad and keyboard and mouse.
- [ ] PRO-34 **Play**: Tab opens the pack. Use, drop and equip work with the mouse and the pad. The weight bar is right and every item has an icon. The Show button cycles categories, the equipped block lists the four weapon slots, belt and pack tier, and Stow moves a stack into an open crate.

### M2: Graphics detail

- [ ] PRO-35 **Play**: contact shadows ground the props, and Medium holds 60 fps. Compare with the tour screenshots.
  - The global volume carries ACES tonemapping, colour adjustments (exposure +0.2, contrast +15, saturation -20), cool-teal shadows with warm highlights, bloom (threshold 1.1, 0.35), a 0.28 vignette, thin film grain at 0.25 (on in raids or where the tier allows), chromatic aberration at 0.05 (off on Low), and a lift/gamma/gain preset that deepens toward blue as night falls. Aiming down sights blends in the depth-of-field volume.
  - Below half health the vignette turns red and widens and the frame desaturates, up to -30 more at zero; the leader's death drains the rest. Poison adds the green tint and blur, and camp reads warmer than the street. `ScreenGradeTests` covers the maths and checks the rig adds every override.
  - Deviation: these are layers on the one global volume the rig builds in code, not separate local volumes or a saved `OutpostZero_PostFX.asset` profile.
- [ ] PRO-36 **Play**: an expedition runs from afternoon to night, lamps pool on wet asphalt, and the flashlight casts shadows. No light leaks through `Building_*` walls.
  - The clock drives the sun, a trilight ambient and the procedural skybox's exposure. Three in ten street lamps are dead and the rest flicker in sodium orange. Car hazard lights blink, the campfire breathes, and the generator's floodlights come on when it is fuelled. All of them feed the stealth exposure.
  - The flashlight uses the generated cookie with the issue's inner and outer angles, casts shadows, and has a visible cone. Light probes sit on a grid from the scene builder, and reflection probes refresh from script per district block.
  - Deviation: shadow distance follows the quality tier (18, 40, 60 and 80 m) rather than one 45 m value, with 2 cascades on Medium.
- [ ] PRO-37 **Play**: rain plays and looks different from clear weather, and fog hides the map edge.
  - Fog is exponential-squared, 0.006 by day and 0.014 at night, plus a per-weather sheet and a ground pool under 2.4 m. Fog weather hides the 70 m edge. Three low mist banks on the avenue give the ground-hugging layer.
  - Rain, storm and ash-fall run their own particles (ash on the Ash Market). Lightning spikes the light, and its thunder covers a gunshot so nothing is alerted. Rain cuts zombie sight to 75% and dulls every noise by 15%. `_Wetness` and `_WindStrength` are set globally for the shaders and puddles.
  - Deviation: the height fog is the mist-bank meshes rather than a `FullScreenPassRendererFeature` pass, because that feature's material needs the editor to author.
- [ ] PRO-38 **Automated** (`DecalSystemTests`: the atlas table matches the generator and the issue's counts, the cell UVs, surface-matched holes, the 2 s budget fade with no frame dropping more than 5%, the wall-behind splats and the blast scorch size, and the material's shader and atlas links; `test_decal_atlas.py`: every cell drawn and clear at its border, and the committed atlas current) and **Play**: after a firefight the street and the walls behind kills are marked, and the decal budget recycles without popping. A shotgun kill against a wall leaves three to five splats and a drip running down it. Holes in cars ring bright, holes in crates splinter, and holes in concrete chip. A pipe bomb or barrel leaves a 5 m scorch on the road. Oil from a burst barrel spreads over 3 s. Bloody prints follow the leader after walking through blood. Gore Off leaves holes and scorches but no blood.
- [ ] PRO-39 **Play**: every shot, hit, kill and explosion has distinct VFX, and the VFX pool counts in the Profiler's hierarchy don't climb.
  - Effects are `VfxEvent`s rented from `VfxPool` (`Resources/VfxLibrary.asset` maps each event to an optional authored prefab plus pool keep/life; empty entries use the procedural build). Weapons name `muzzleVfx`, zombie archetypes `hitVfx`/`deathVfx`, hazards `blastVfx`. Kills now throw a directional `DeathBurst`; dumpsters carry a looping fly swarm. Press the AI watch key: the line reads `vfx <live>  peak <n>  pooled <idle>  reused <n>%`; after a firefight stops, `vfx` must fall back to 0 while `pooled` holds and `reused` climbs. Automated: `VfxPoolTests` (11).
- [ ] PRO-40 **Play**: no grey default materials, tiling and normal detail show under the flashlight, and the Frame Debugger shows SRP-batched draws.
  - Twelve families live in `Assets/Materials/Library` (`ML_*` URP/Lit, `MT_*` world-space triplanar on `OutpostZero/EnvironmentTriplanar`). Kit boxes, district lots, street dressing, colony modules, loot, throwables and the map rim now pick a family; `UrpMaterialPass` dresses anything still on `Default-Material` from its `SurfaceTag` or name. Characters flash on hit (`_HitFlash`), zombie eyes glow from the mask's blue channel, and rims read cyan for survivors and green for zombies. Check in the Frame Debugger that `SRP Batch` draws dominate; renderers with a tint block fall out of the batch by design. Automated: `MaterialLibraryTests` (21) and `test_material_library.py` (12), plus the CI `material_library.py --check`.
- [ ] PRO-41 **Play**: any screenshot is recognisably commercial, industrial, ruins or camp.
  - Street litter is a Poisson-disk scatter (`PoissonScatter`), thick against curbs, lot walls and kit walls and thin in the open road, drawn GPU-instanced by `DebrisField` with no colliders (bottles stay objects so they can be kicked). Seeds and densities per district are committed in `Resources/DebrisProfile.asset`; tune them in **Tools > Outpost Zero > Debris Scatterer** (select the district root, Preview, Save). The yard ends at a chain-link fence on a concrete plinth, with the NavMesh carved short of it by the invisible wall behind. Past the street stand two billboards and a water tank on legs. Check in the Frame Debugger that litter draws as a few instanced batches. Automated: `EnvironmentDressingTests` (10).
- [ ] PRO-42 **Play**: three Walkers side by side look different, and their eyes show at night before their bodies do.
  - Each body gets a seeded clothing tint (3 outfits, 4 caps, ±8% per channel) and a seeded blood mask from the character shader (`_Gore`, `_GoreSeed`): zombies always carry a few splats, and under 40% health (70% with Gore High) they switch to the soaked gore variant; Gore Off clears it. The splats sit in object space, so they stay on a walking body. Eyes are HDR emissive (Walker yellow-green and Runner red at 2.5, Brute a dim orange) so bloom picks them up in the dark. Every character stands on a soft blob-shadow decal from the atlas, and the leader has a faint cyan aim stripe on the ground. Merchants and colonists get the same tint, rim and blob. A zombie reused from the pool must come back whole, not melted. Automated: `CharacterVisualTests` (9) and `test_decal_atlas.py`.
- [ ] PRO-43 **Play**: follow, aim zoom and impulse shake feel right, and the camp overview camera works while placing modules.
  - The main camera carries a Cinemachine brain. `CameraTargetDriver` (formerly `TopDownCameraFollow`, with the same GUID) moves a `CameraTarget` proxy: the leader plus pointer lead, or right-stick lead on a pad. `CM_Follow` and `CM_Aim` frame that proxy with a Position Composer. Holding ADS blends to the 42° aim shot in 0.25 s, pitched 3.5° steeper, and the depth-of-field volume's weight rides the same blend. Both shots sit inside a Confiner 3D box computed from the fence, lens and screen aspect, so at most 8 m of skyline shows past the fence and a leader at the fence stays on screen. Check on 16:9 and 21:9.
  - Shake is a Cinemachine impulse read from `Resources/CameraProfile.asset`: per-weapon recoil, explosions (fading out over 24 m) and brute swings and charges (12 m). The Screen Shake slider scales it, and 0 turns it off.
  - In camp, `CM_Camp` looks down at 65°. Press Build: the view scrolls at the screen edge (or with the right stick), stays within 24 m of the leader, and the wheel zooms between 18 and 44 m instead of cycling weapons. Placing and demolishing work from any zoom.
  - When the leader falls, the camera slowly zooms onto the body over 2.5 s. On extraction it pulls back and then settles into a high wide shot behind the results screen (`CinemachineSequencerCamera`).
  - Automated: `CameraRigTests` (11).
- [ ] PRO-44 **Automated** (no `OnGUI`) and **Play**: the HUD is readable at every resolution in section 5, with no per-frame GC from UI.
  - `HudController` draws the street HUD on its own panel (sorting 10, below the menus) from `Assets/UI/Resources/HUD.uxml` and `HUD.uss`. Theme colours and margins are USS variables on `.hud`. Both panels scale with screen height from 1920×1080, so 21:9 gains room rather than bigger elements.
  - Check each element: health bar with a trailing damage ghost; stamina bar; hunger, thirst and fatigue mini-bars with status pills; ammo as magazine / reserve, which breathes when low, plus a reload ring; four weapon slots with the rendered weapon icons and the hold-to-open wheel; noise and exposure meters; a tension pip that beats at Peak; a compass strip with POI and gate markers pinned to its ends when behind; the interaction prompt beside the cursor (or above the object on a pad); hit and kill markers on the target; the kill feed; damage-direction edge vignette; a stacked toast queue; captions and the tutorial line.
  - Take screenshots at 1280×720, 1920×1080, 2560×1440 and 2560×1080 with Interface Size at 1.0 and 1.5. Nothing should overlap, and the smallest text should be about 10 px at 720p.
  - Profiler, Memory module, during a fight with a reload and pickups: GC Alloc from `HudController.Update` and `OutpostInterface.Refresh` stays at 0 B on most frames. Text rebuilds only when a shown value changes. The menu panel skips its key during play, and the camp and pack panels hash their inputs (`UiKey`).
  - Automated: `HudTests` (22 cases). They cover the UXML matching `HudTree`, every class being styled, every name the controller queries existing, fit and readability at the tested resolutions, the toast, vignette, compass, heartbeat, hit-marker and reload math, no `OnGUI` or `GUILayout` outside editor code, and the weapon icon links. Run the tests with `HUD_WRITE=1` to regenerate `HUD.uxml` after changing `HudTree`.
- [ ] PRO-45 **Automated** (`PerfTests`, category `Perf`, in the PlayMode CI job) and **Play**: under 300 draw calls at Medium in the busiest raid, as shown in the Frame Debugger.
  - `PerfTests` runs a 60 s scripted firefight on Medium. The leader shoots the nearest zombie with each weapon in turn (switching every 12 s) while the crowd is refilled to 30. It passes when the median frame is within 16.6 ms, p95 is within twice that, no frame exceeds 250 ms, GC averages 16 KB or less per frame, and draw calls stay at or under 300 at p95. `ProfilerRecorder` numbers go to `Artifacts/Perf/perf-report.json`, which CI uploads as `perf-report`.
  - Tiers: Unity has exactly four quality levels (Low, Medium, High, Ultra), and each uses `Assets/Settings/OutpostZero_URP_<Tier>.asset` with its own shadow distance and resolution, cascades, MSAA, render scale and HDR. Low uses `OutpostZero_URP_Renderer_Lite` (decals, no SSAO); the others use the full renderer with SSAO. The decal, particle and DoF budgets come from `QualityProfile`. Low caps the crowd at 16 through `DifficultyProfile.AliveCap`, whatever the difficulty. Switching tiers in Settings calls `QualitySettings.SetQualityLevel`, so the URP asset swaps live.
  - With the Frame Debugger open on Medium, the SRP Batcher should batch the environment and characters (every Outpost shader has one `UnityPerMaterial` buffer). Baked prop and library materials have instancing on, and street litter draws instanced.
  - LODs: set pieces exported with `_LOD1` (50 % decimation) switch to LOD1 around 28 m and cull at 60 m at the default FOV (`LodBands`). The batch scene build bakes occlusion, with walls and buildings as occluders and clutter as occludees only.
  - Zombies: sight checks spread so about 8 zombies look each frame (`QualityProfile.SightGroups`), avoidance drops to low quality beyond 18 m, and Animators use `CullUpdateTransforms`.
  - Textures: each import is BC7 on desktop (BC5 for normal maps) and ASTC 6×6 on mobile (4×4 for normals), always with mipmaps. Mip streaming is on except for icons and the decal atlas (`TextureRules`, enforced by `TextureImportPolicy`).
  - Left open: the environment and gameplay additive scene split needs the editor (see PRO-63).
  - Automated: `QualityTierTests` (18 cases) and `PerfGateTests`.

### M3: Artifacts and asset pipeline

- [ ] PRO-46 **Play**: `blender -b -P BlenderScripts/pipeline.py -- --only Prop_Dumpster` finishes in under 10 s, and a full rebuild of unchanged generators matches byte for byte.
- [ ] PRO-47 **Automated** (textures and UVs per manifest entry) and **Play**: the flashlight reveals normal detail on the sedan and the brick wall.
  - UV overlap: `fbx_uv.py` reads each committed binary FBX without Blender and rasterises its first UV layer at 256². A mesh fails when more than 2% of its covered texels are claimed by two triangles. Both `asset_audit.py` and `pipeline.py` (after each export) run the check. All 114 committed meshes measure 0%.
  - Automated: `test_fbx_uv.py` and `test_asset_audit.py`.
- [ ] PRO-48 **Play**: every character imports with no avatar errors, all clips play without tearing, and the Walker still shambles.
  - Deviation: characters import as **Generic**, not Humanoid. The upper-body avatar mask and the aim rig address bones by path and name, and Humanoid retargeting would remap those. The skeleton still carries all 15 Humanoid-required bones (`character_rig.REQUIRED_BONES`), so switching later is only an importer change plus a mask rebuild.
  - Left open: `Colonist_Survivor` has no Work or Talk clips yet, and baking them needs Blender.
- [ ] PRO-49 **Automated** (a kit assembles an enterable three-storey block; `KitMeshTests` and `test_kit_prefab_set.py` check that every catalog piece resolves to its baked prefab, the mesh half-turn lands on the catalog boxes, and the assembler snaps to the grid) and **Play**: the storefront and warehouse interiors can be entered and show the baked kit meshes. *Tools > Outpost Zero > Kit Assembler* places pieces and exports a recipe.
- [ ] PRO-50 **Automated** (`ItemDatabaseTests`: every item has a definition, FBX, prefab and 64 px rendered icon, all linked; `ItemDatabase.Get("ammo_9mm")`) and **Play**: icons render in the pack and a dropped item lands as its model, not a cube.
- [ ] PRO-51 **Automated** (the scene builder has no FBX paths, every builder id has a prefab, every prefab carries its SurfaceTag and layer, and zombies come from `Assets/Prefabs/Enemies/*_Actor.prefab`) and **Play**: reimporting an FBX regenerates its prefab and keeps hand edits, footsteps change between concrete, wood and metal, and after **Build Prototype Test Arena** the scene has no parked zombie prototypes.
- [ ] PRO-52 **Play**: deleting a texture or renaming an FBX turns CI red with a clear message, and the suite runs in under 60 s. (Covered off-engine by `Artifacts/ArtifactSuiteTests`; confirm the same in the Unity job.)
- [ ] PRO-53 **Play**: pushing a `v*` tag produces three zips that launch to the menu, and the version in the menu matches the tag.
- [ ] PRO-54 **Play**: a new contributor can clone, regenerate assets, build the scene and run the tests from the docs alone.

### M4: Sanctuary and colony

- [ ] PRO-55 **Automated** (two seeds give different rosters) and **Play**: leader skills change expedition play.
  - Roster: `SurvivorDraw` opens 4 seeded survivors, each with up to three traits from 16 (trait, aside and mark, with exclusions such as Brave and Cowardly), five skills, age and backstory (`LifeLine`), and needs. `KinBoard` keeps an opinion from -100 to 100 for each pair. `Heir.Pick` chooses the uninjured survivor with the highest leadership plus morale, and `SuccessionLedger` keeps the memorials.
  - Leader skills in play: Guard sets spread, reload and stamina (`FieldHand`, `HandDepth`, `NeedsPressure`), Scavenge sets loot rolls, Medic sets medkit healing, and the traits hook in through `TraitHook`.
  - Debug: F4 (editor and development builds) shows the roster sheet (`RosterSheet`), with each survivor's traits, skills, needs, wounds, task and opinion.
  - Deviation: traits are string ids with hooks in `TraitHook`, not ScriptableObjects, and skills grow by practice up to 8 (`Practice.Cap`) instead of running 0 to 10 with XP.
  - Automated: `RosterSheetTests` and the `SurvivorDraw` and `Heir` tests in `GameSystemsTests`.
- [ ] PRO-56 **Play**: run 5 days in a row, each with tasks, an expedition and a return, and see events fire.
  - Clock: `WorldClock` runs Morning (05:00), Day (10:00), Evening (17:00) and Night (20:00, the raid window). The HUD clock shows the phase. A phase turn raises `PhaseTurned`, and in camp it autosaves. A street or raid saves when it ends, so a mid-fight save can't undo a death.
  - Board: Guard, Cook, Medic, Build, Scavenge, Clear and Rest, with skill and traits setting the output (`SurvivorRoster.TickTasks`). **Their call** lets `TaskPick` choose each morning from the survivor's skills, traits and needs and from the camp's shortages. `CampRoutine` still pulls the hungry, hurt and worn off their post.
  - End of day: `ColonyDay.Simulate` eats 1 food and 1 water per head, and shortages cut morale. It also heals wounds, drifts opinion, and fires events: grief, friendship, recovery, breakdown, argument, celebration, and rain soaking the yard. Fever, rain catch, caravans, rescues and raid warnings come from their own services.
  - Left open: menu, camp and street still share one scene (the PRO-63 editor blocker).
  - Automated: `ClockPhaseTests`, `TaskPickTests`, and the `ColonyDay` tests in `GameSystemsTests`.
- [ ] PRO-57 **Automated** (collapse and thrive arcs) and **Play**: survivors walk between modules. A starving, grieving camp collapses in about 3 days and a well-run one thrives.
  - Yard bodies are now the `Colonist_Survivor` model (from `Resources/CampCast.asset`), with the colonist clothing, rim and blob shadow. On the NavMesh they steer with a `NavMeshAgent` round modules (`CampMateBody`), and the model's controller plays the walk at the matching stride. With no prefab they fall back to capsules, and with no NavMesh they walk straight.
  - Check in play: colonists leave the gate, walk round the campfire and cots to their stations, visit friends, and stand on the wall during a raid.
  - Already in place: mood bands (Inspired above 70 gives +10% output, Depressed below 30 gives 70%, Breakdown below 10), idle rest without a cot costs 5 morale, grief is −25 (−40 for a friend), a won expedition is +10, pair opinions drift, and the camp board row serves as the inspection panel.
  - Automated: `CampArcTests`.
- [ ] PRO-58 **Play**: B places walls, a generator with lights, a farm and a purifier. Each changes the daily numbers and raid behaviour.
  - Build mode shows a ghost box on the snapped 2 m cell. It is green when the cell takes the module and red when it is taken, past the fence, or short of scrap (`BuildGhost`), and placement refuses the same cases.
  - The build row has Defence, Living and Works tabs. Each button shows its scrap cost, is dimmed when you can't afford it, and is highlighted when selected (`BuildMenu`).
  - Finished modules wear the baked base-kit prefabs (`Resources/ModuleLooks.asset`): the wood-and-wire barricade, cot, water collector (also used for the purifier), watchtower, diesel generator, workbench, campfire cooker, crates, street lamp, oil barrel and spikes. Build sites keep the plywood scaffold box. The farm and the turret keep box stand-ins. A battered module sits lower and darker.
  - Already in place: 16 module kinds (the memorial wall came with PRO-60), R rotates, demolish refunds half, carving NavMesh obstacles, and the farm, water, rain catch, generator and floodlight, cot, bench, tower, turret and trap behaviours. Wall cover feeds raid odds and strikes, and placements are saved.
  - Deviation: modules are an enum with cost and behaviour in code, not `BuildingModuleDefinition` ScriptableObjects, and costs are scrap only.
  - Automated: `BuildGhostTests`.
- [ ] PRO-59 **Play**: crafting solves ammo scarcity at a real material cost, and T2 recipes need base investment.
  - With a campfire built, the camp board offers Cooked meal (2 raw and 1 scrap for 3 food) and Purified water (1 scrap and 1 chemical for 3 water). Without a campfire the button says "Need a campfire". Bottle (1 scrap) makes a throwable street bottle anywhere.
  - With a workbench built, every carried crafted item gets a Dismantle row that returns half its recipe bill, rounded down. Dismantling is refused when the stores are full. Ammo, food, materials and bottles don't break down.
  - Already in place: 24 recipes with scrap, cloth, chemical, tape and raw costs; workbench, cot and campfire stations; medic-on-duty gates; the tier-2 bench upgrade (3 build hours); blueprint loot that opens the tier-2 recipes; the backpack T2 raise; weapon mods; and stripping a spare gun for scrap and chemicals.
  - With a workbench built, each recipe that makes a thing has a Queue button. Queuing spends the materials at once and adds the order to the bench line (up to 6, shown as "Bench orders"). A survivor on the new Craft task finishes one order per shift, or two from Engineering 4, practises Engineering, and the output goes where an instant craft would. An order that can't be delivered (a full pack or full stores) waits for the next shift. Mods, the generator repair, the wall brace and the radio spare can't be queued, because they are fitted by hand. Orders are saved, and "Their call" survivors pick Craft when orders wait and they're handy at the bench.
  - Deviation: recipes are a code table (`CraftBill`, `CraftingBench.Recipes`), not `CraftingRecipe` ScriptableObjects.
  - Automated: `CraftQueueTests` and `CraftingRecipeTests` (every recipe has a bill, a real output and EN/ES names; dismantling can't pay back more than the cheapest craft).
- [ ] PRO-60 **Play**: see the leader death in section 2.
  - When the leader dies, the death camera closes in and the frame drains to near grey (`DeathVeil`, saturation −85 at the end of the zoom). The colour comes back when the succession screen hands over.
  - The succession screen shows the fallen leader's memorial card, then every living survivor with their traits and mood. The suggested heir (fewest injuries, then leadership, then morale) is marked "Suggested" and highlighted.
  - Build a Memorial wall (Living tab, 6 scrap, boarded-wall model). Once it stands, a death costs the camp 15 morale instead of 25, and a friend 30 instead of 40.
  - Already in place: the Dying camera, `MarkLeaderDead` with the cause, the carried pack left on a corpse in that district (saved, raised again on re-entry, recovered once), the memorial list on the camp board, game over when nobody is left, and Merciful mode (the leader is dragged home wounded for 18 scrap and a camp morale hit).
  - Deviation: there is no PlayMode test. The EditMode `SuccessionFlowTests` and the older succession tests in `GameSystemsTests` cover the ledger, the corpse packing and the grief numbers.
- [ ] PRO-61 **Play**: a day 3 raid against a weak wall can be lost, while walls, guards and lights make day 10 survivable.
  - A raid is called for the night when it is due, or from day 2 when the day was loud (gunshots near camp, or a running generator on Survivor and Nightmare) and there are too few walls (`RaidCall`). Watchtowers, guards and the Watchful and Light Sleeper traits buy warning seconds before the `RaidActive` state opens, with the raid grade on the lighting (vignette, red filter, grain).
  - Waves come from one side on day 1, two from day 2, and three from day 8 or on Nightmare (`RaidPlan.Fronts`). Zombies stop to chew barricades, strikes wear walls on the approach side, and a brute phase can breach an open side.
  - Guards spend stored rounds from their posts, and a hurt guard shoots shorter. Powered turrets fire from the generator (tier 2 hits harder), spikes chip and wear, oil pits ignite, and lamps on the approach count toward holding.
  - Outcomes: held gives +6 morale and a task tick. Lost costs scrap, and a breach also spoils food. Injuries land on guards first and can kill, dead zombies are left as bodies to clear, and the morning line asks for repairs when modules are damaged. Nobody left alive is game over.
  - Automated: `RaidOutcome.Holds` in `GameSystemsTests` pins day 3 with one wall lost, two walls or one guard held, and day 10 needing two walls and a guard, or one wall and three lamps.
- [ ] PRO-62 **Play**: scrap buys from the merchant, caravan visits can be planned around, and reputation changes prices and unlocks stock.
  - At standing 30 or higher, each faction adds one item to its table, marked "(trusted)": the Caravan sells flares, the Iron Militia pipe bombs, the Clinic antibiotics and the Free Farmers raw food. Below 30 the stall says where more stock opens.
  - Every carried item with a barter value gets a Sell row. The offer is half the base price, up to 10% more at full standing, and can never beat what the same item costs to buy. Crafted gear is priced from its recipe bill (`CraftBill.Value`).
  - The leader haggles 1% off per point of Leadership, capped at 10%.
  - Already in place: four factions with standing from −100 to 100 (prices move ±30%, slow decay, a gift on each visit, saved), a visit every 3, 4 or 5 days in rotation, a permanent Caravan stall once a Trading Post stands, the Militia refusing below −20 and flagging an ambush below −40, three quests (deliver 4 medkits to the Clinic for a blueprint, clear a district for the Farmers, get back on a Caravan day), and a caravan stall with guards in districts on visit days.
  - Deviation: factions are a code table (`CaravanBook`), not `FactionDefinition` ScriptableObjects, and the trade screen is one list rather than two panes.
  - Automated: `FactionTradeTests`.

### M5: Shell and release

- [ ] PRO-63 **Play**: see the full loop in section 2. Esc backs out of every menu one level at a time. Every travel fades to black before the loading card and lifts after arrival. With screen shake at 0 it cuts with no fade.
- [ ] PRO-64 **Automated** (round trip, `.bak` recovery, v1 fixture migration) and **Play**: quit at each camp phase and Continue restores it.
- [ ] PRO-65 **Automated** (`SettingsFlowTests`: render scale round-trips, closing with changes asks keep/revert, focus loss mutes, fixed and taken keys are refused with a reason, and `{key:Action}` tokens in hints, lessons and the interact prompt follow a rebind) and **Play**: every setting survives a restart and applies at once. Rebinding a key updates the HUD prompts.
- [ ] PRO-66 **Automated** (audio buses and clip validation; `AudioCoverageTests` checks every played, computed and mixer-named id has a clip, and that `SfxLibrary` ids are real) and **Play**: playing blind, you can tell the weapon, surface, zombie type, distance and tension.
- [ ] PRO-67 **Automated** (`TutorialFlowTests`: the Day 1 camp track (assign a task, read the stores, place a barricade, craft a bandage, launch) and the expedition track advance in order; every gate is emitted somewhere; the camp track is remembered in the codex; prompts show pad buttons while a pad is in use; each Day 1 step rings the control it names and dims the other camp rows; every zombie, module and faction codex entry shows its rendered icon; the New Game card can skip the tutorial and a new run keeps that choice; the codex can replay every hint; the first expedition is a quiet street with three scripted walkers ahead of the start and no waves; the crouch, reload and flashlight hints arrive when a zombie is near, the magazine runs dry and you step into a dark room) and **Play**: 3 fresh testers finish Day 1 and the first expedition with no README.
- [ ] PRO-68 **Automated** (same seed gives the same street, blocks stay walkable) and **Play**: a campaign win takes about 6 to 8 hours.
  - The camp board lists 10 districts with tiers 1 to 4. Only neighbours of a cleared district can be reached, and unseen ones stay under fog. Travel takes 2 hours per tier and burns fuel. Each row shows the sky forecast, known sites and what was already searched.
  - Loot follows the district: Old Hospital crates roll the medical table; the Rail Yard, North Gate, Police Station, Highway Overpass and Downtown Core roll the military table; and the rest roll the general crate table. Runners join from tier 2 hordes and brutes from tier 3.
  - Radio parts come from the Old Hospital, the Police Station and Downtown Core (a crafted spare can stand in for one). With all three and a generator built, the camp can call Broadcast Night, a raid with four extra zombies. Holding it wins, shows the run stats, and offers endless mode. Losing the whole roster is game over, and both endings land on the local run board.
  - Each district adds a seeded road graph east of the street: a spine with alleys, removed cells, lots, a loot crate, a nest and a side gate, with curbs and lane dashes on the open edges. Ash Market, the Old Hospital and Downtown Core use authored layouts, and the Ash Market plays as the tutorial district.
  - Every lot is a one-tile kit house: floor, four walls, roof, and on apartment and station districts (and half the others) a second storey. The front varies between a door, boarded, and broken-window walls. Walk around a lot and check that there are no gaps at the corners and that the crate south of it can be reached.
  - Deviation: the lots are single 2 m kit tiles on the 4 m road grid, not multi-tile buildings, and only the district's main building has an interior.
  - Automated: `GameSystemsTests` rebuilds every district for seeds 1 to 100, checks the same seed gives the same street, and checks that every block stays walkable from the spawn to the point of interest and the way out. `RoadLotTests` checks every lot house uses known kit pieces, is the same for the same seed, closes all four faces, and stays inside its lot and clear of the crate.
- [ ] PRO-69 **Automated** (`VisionModeTests`: the setting cycles off, blue-yellow, red-teal and mono and saves with the enemy outline; zombie eyes and rims take the palette colour, and the outline hardens the rim; the dev-only pseudo language wraps every table string, so plain text on screen is a hard-coded string) and **Play**: the game is playable in each colour-blind mode (off, blue-yellow for deuteranopia and protanopia, red-teal for tritanopia, mono), with the enemy outline on, and with sound off using captions. The interface size slider (80 to 150%) scales the HUD, menus, loading card and dev panel together. Switching EN/ES re-renders every screen. Font chain (`StringTableTests`: each language maps to Latin, Cyrillic or CJK and the chain covers all three): in a scratch build with a Russian or Japanese table, every panel shows the glyphs instead of boxes on Windows, macOS and Linux, and EN/ES text keeps the default font. In a dev build, switch to the pseudo language and walk every screen: any line not in `[áççéñtéð ~~]` form is hard-coded, and any clipped line needs a wider box.
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
