# Audio attribution

Every tone Outpost Zero plays is generated in the game by `ClipBook`. No third-party clip is shipped.

`SoundCredit` lists each id in `ClipBook.Ids` as `generated`. The main-menu credits screen shows that line. An id that is not in the book has no credit.

## Authored clips

Recorded or licensed clips go in an `SfxLibrary` asset at `Resources/Audio/SfxLibrary`. Create it with *Create > Outpost Zero > Sfx Library*. Each entry names a `ClipBook` id and its clip variants. `AudioManager` plays a random variant, and any id left without variants keeps its generated tone. `SfxLibrary.Stray` rejects an id the game never plays, and an id listed twice.

Credit each authored clip below before shipping it:

| Id | File | Author | Licence | Source |
|----|------|--------|---------|--------|
| (none yet) | | | | |
