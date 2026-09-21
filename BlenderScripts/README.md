# Outpost Zero Blender pipeline

Supported Blender version: **4.2 LTS** or newer. The build script exits if `bpy.app.version` is older.

Output goes to `<repo>/Assets/Models/<Category>`. The old hard-coded `C:\Users\...` path is gone. Override the models root with `OUTPOST_MODELS_DIR` or `--models-dir`.

## Windows

```powershell
& "C:\Program Files\Blender Foundation\Blender 4.2\blender.exe" -b -P BlenderScripts\build_all_assets.py
```

One category:

```powershell
& "C:\Program Files\Blender Foundation\Blender 4.2\blender.exe" -b -P BlenderScripts\build_all_assets.py -- --only characters,weapons
```

## Linux and macOS

```bash
blender -b -P BlenderScripts/build_all_assets.py
blender -b -P BlenderScripts/build_all_assets.py -- --only props --models-dir /tmp/outpost-models
```

Categories: `characters`, `weapons`, `architecture`, `props`, `base`.

Path helpers live in `blender_paths.py` and can be checked without Blender:

```bash
python3 -m unittest BlenderScripts/test_paths.py
```

Run that command from the repository root so `BlenderScripts` imports resolve, or:

```bash
cd BlenderScripts && python3 -m unittest test_paths.py
```
