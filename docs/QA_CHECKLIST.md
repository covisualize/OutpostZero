# Outpost Zero v1 QA checklist

Unity is not available in the cloud agent, so this list is the playtest gate for a machine with the editor. Check a box only after watching it happen in Play Mode.

## Boot

- [ ] Open `Assets/Scenes/PrototypeArena.unity` and press Play. The street is the expedition, not a main-menu lock.
- [ ] WASD moves, the mouse aims, left click fires, and zombies take damage.
- [ ] Console stays quiet unless `OUTPOST_VERBOSE_COMBAT` is defined.

## Combat

- [ ] Pistol, shotgun, assault rifle (4), and machete all hit enemies and barrels.
- [ ] A head hit yellow-numbers for double damage. Shotgun kicks the camera. Melee kills hitch for a frame.
- [ ] Explosive, toxic, and oil barrels detonate. Toxic leaves a poison ticker.
- [ ] Runners lunge. Brutes charge, resist stun, and can knock the leader down.
- [ ] A scream pulls nearby zombies. A wall blocks that sound.
- [ ] Crouch behind a zombie that is not alert and press V for a silent kill.
- [ ] G throws a noise lure. A molotov from the pack bursts on impact.

## Stealth, needs, objectives

- [ ] The exposure line drops while crouched at night and rises in flashlight or lamplight.
- [ ] Hunger, thirst, and fatigue move during the expedition and slow stamina recovery when low.
- [ ] Kill 8 and loot 15 scrap, then walk into the sanctuary gate. The results card appears.
- [ ] Tab opens the pack. Using food, water, or a bandage changes the bars.

## Colony

- [ ] Pause, choose Sanctuary. Four survivors are listed. Assign tasks and advance a watch. Stores change.
- [ ] B and a click in the yard spends camp scrap and drops a barricade the navmesh carves around.
- [ ] Craft a suppressor and hear the next shot come up shorter on the noise bar.
- [ ] Talk to the merchant and buy a medkit.
- [ ] Endure the night. The raid ends and returns to camp.
- [ ] Die on purpose. The fallen leader stays dead, a corpse can be searched, and the chosen survivor wakes at the gate.
- [ ] Kill the whole roster. The outpost-falls card is the only way forward, and it starts a new roster.

## Shell

- [ ] Save, restart the editor play session, and Continue. Day, scrap, roster, and placed modules come back.
- [ ] Break the save's `schemaVersion` and confirm the game refuses it.
- [ ] Settings change shake, volume, text size, subtitles, and EN/ES labels.
- [ ] Clearing three districts shows the ring-is-clear line.
- [ ] The tutorial lines show once, then stay dismissed after save and load.

## Performance and builds

- [ ] The perf warning stays silent with the default horde on a 60 fps machine.
- [ ] GameCI editmode, playmode, and the Linux player job are green. That needs `UNITY_LICENSE`, `UNITY_EMAIL`, and `UNITY_PASSWORD`.
