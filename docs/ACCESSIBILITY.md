# Accessibility review

This checklist tracks Outpost Zero against the [Game Accessibility Guidelines](https://gameaccessibilityguidelines.com/) basic and intermediate levels (PRO-69). **Done** means the option exists, is saved in `settings.json`, and has an EditMode test. Every **Done** item still needs the play check in `docs/QA_CHECKLIST.md` before release.

## Motor

| Guideline | Level | Status | Where |
| --- | --- | --- | --- |
| Allow controls to be remapped | Basic | Done | Settings: keyboard and pad rebinding. Fixed keys and keys already in use are refused with a reason. `ControlBindings`, `PadBindings`, `SettingsFlowTests` |
| Ensure controls are as simple as possible, or provide a simpler alternative | Basic | Done | Crouch and sprint can be held or toggled. Gamepad aim assist has off, low and high. `SettingsService.ToggleCrouchMode`, `ToggleSprintMode`, `CycleAim` |
| Avoid repeated inputs (button mashing) | Basic | Done | No mash prompts. Automatic weapons fire while the trigger is held (`TriggerGate`). |
| Include an option to adjust the sensitivity of controls | Basic | Done | Invert look and field of view in Settings. |
| Support more than one input device | Intermediate | Done | Keyboard with mouse, or a gamepad. Prompts show the active device's glyphs (`InputGlyphs`, `TutorialFlowTests`). |
| Allow gameplay to be paused | Intermediate | Done | The pause menu opens in camp and on the street. Focus loss mutes audio. |

## Cognitive

| Guideline | Level | Status | Where |
| --- | --- | --- | --- |
| Allow the game to be started without navigating multiple levels of menus | Basic | Done | Main menu Continue and New Game. |
| Use an easily readable default font size | Basic | Done | 14 px base at 1080p, text scale 80 to 160%. |
| Use simple clear language | Basic | Done | Short hint and codex lines. Everything is in the string table (`Loc`). |
| Include tutorials | Intermediate | Done | Day 1 camp track and a scripted first expedition. Each step rings the control it names, and it can be skipped (`TutorialDirector`, `TutorialRun`). |
| Include a means of practising without failure | Intermediate | Done | Scavenger difficulty and the merciful (non-permadeath) toggle. |
| Provide an option to adjust game speed or difficulty | Intermediate | Done | Three difficulties, set per run and changeable for the next run. |
| Allow players to progress through text prompts at their own pace | Intermediate | Done | Tutorial read steps wait for Next. |
| Offer a means to bypass gameplay elements that aren't part of the core mechanic | Intermediate | Done | Skip tutorial in New Game and the pause menu. |
| Provide a codex or glossary to recap information | Intermediate | Done | Codex with zombie, item, module, faction and mechanic entries, and rendered icons. Hints can be replayed from it. |

## Vision

| Guideline | Level | Status | Where |
| --- | --- | --- | --- |
| Ensure no essential information is conveyed by colour alone | Basic | Done | Colour-blind modes add shape marks to the noise meter (`NoiseCue`). Exposure shows as a number. |
| Provide high contrast between text/UI and background | Basic | Done | Light text on a dark veil. HUD opacity is adjustable. |
| Use an easily readable default font size | Basic | Done | See Cognitive. |
| Provide colour-blind options | Intermediate | Done | Blue-yellow (deuteranopia and protanopia), red-teal (tritanopia) and mono palettes. They recolour the HUD and zombie eyes and rims (`HudPalette`, `CharacterLook`, `VisionModeTests`). |
| Provide an option to make enemies more distinct | Intermediate | Done | Enemy outline option hardens the zombie rim light. |
| Allow the interface to be resized | Intermediate | Done | Interface size 80 to 150% scales every UI Toolkit panel (`PanelScale`). |
| Provide an option to adjust brightness | Intermediate | Done | Brightness slider. |
| Provide a font that covers every supported script | Intermediate | Done, needs a Unity check | Latin languages (EN, ES) keep the default font. For Cyrillic and CJK languages, `FontFallback` sets every panel's font to an installed OS font that holds the script, checked with sample characters, and chains the other scripts behind it (`FontChain`). No font files ship. A machine with none of the listed families falls back to the default font and logs a warning. |

## Hearing

| Guideline | Level | Status | Where |
| --- | --- | --- | --- |
| Provide subtitles for all important speech | Basic | Done | There is no voiced dialogue. Survivor barks show as text in the camp panel and alerts as toasts. The captions toggle covers sound effects. |
| Provide separate volume controls | Basic | Done | Master, sound effects, music, ambience and UI sliders. |
| Ensure no essential information is conveyed by sound alone | Intermediate | Done | Directional captions such as "[Zombie scream, north]" (`Presentation.Caption`). The noise meter shows how loud the player is. |
| Provide a visual indicator of sound direction | Intermediate | Done | Captions name the compass direction. |

## General and photosensitivity

| Guideline | Level | Status | Where |
| --- | --- | --- | --- |
| Avoid flickering images and repetitive patterns | Basic | Done | Muzzle flash and lightning stay under three flashes a second (`FlashCap`). Flashes can be turned off altogether. |
| Allow screen shake and motion effects to be reduced | Intermediate | Done | Screen shake slider. Hit stop and motion blur toggles. |
| Offer a choice of gore | Intermediate | Done | Gore levels and damage numbers toggle. |
| Test with players with disabilities | Intermediate | Open | Needs a playable build and outside testers. Tracked in the QA sign-off. |
| Provide a pseudo-localised build to find hard-coded text | Intermediate | Done | Dev builds offer a pseudo language that wraps every table string (`PseudoLoc`). Captions and compass words moved into the table as a result. |
