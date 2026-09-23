# Outpost Zero Blender pipeline

Supported Blender version: **4.2 LTS** or newer. The runner exits if `bpy.app.version` is older.

Every model is one entry in `assets.manifest.json`. `pipeline.py` builds entries into
`<repo>/Assets/Models/<Category>/<id>.fbx`, bakes the texture set beside each one, writes an
`<id>.meta.json` sidecar for Unity, and records a fingerprint per entry in
`Assets/Models/.pipeline-cache.json` so unchanged assets can be skipped.

## Build

Windows (PowerShell):

```powershell
$blender = "C:\Program Files\Blender Foundation\Blender 4.2\blender.exe"
& $blender -b -P BlenderScripts\pipeline.py                                  # everything
& $blender -b -P BlenderScripts\pipeline.py -- --only Prop_Dumpster          # one asset
& $blender -b -P BlenderScripts\pipeline.py -- --category props,weapons      # categories
& $blender -b -P BlenderScripts\pipeline.py -- --changed                     # only what moved
python BlenderScripts\pipeline.py --changed --dry-run                        # plan, no Blender
```

Linux and macOS:

```bash
blender -b -P BlenderScripts/pipeline.py -- --only Prop_Dumpster
blender -b -P BlenderScripts/pipeline.py -- --category props --models-dir /tmp/outpost-models
python3 BlenderScripts/pipeline.py --changed --dry-run --json plan.json
```

| Flag | Meaning |
| --- | --- |
| `--only a,b` | Build these asset ids. An unknown id is an error. |
| `--category c,d` | Build these categories (case-blind; the old names `architecture` and `base` still work). |
| `--changed` | Skip entries whose fingerprint matches the cache and whose FBX exists. |
| `--dry-run` | Print the plan and exit. Works with plain Python. |
| `--json path` | Write the planned ids and output paths as JSON (CI uses this for the PR comment). |
| `--cache path` | With `--dry-run`: compare against another build's cache, e.g. the base branch's. |
| `--models-dir path` | Write somewhere else. The repo cache is neither read nor written. |

Each run prints one `[pipeline] {...}` JSON line per event (`plan`, `built`, `failed`, `done`), then a
summary table. The exit code is 0 when everything built, 1 when any asset failed, and 2 for a bad
manifest or bad arguments. `build_all_assets.py` still works and forwards to `pipeline.py`.

## Manifest entry

```json
{
  "id": "Prop_Dumpster",
  "category": "Props",
  "generator": "generate_props.build_dumpster",
  "output": "Props/Prop_Dumpster.fbx",
  "tags": ["container"],
  "bake": {"uv": true, "textures": ["albedo", "normal", "ao", "mask", "icon"], "size": 128},
  "lods": [1.0],
  "collider": "box",
  "pivot": "bottom"
}
```

Entries only spell out what differs from the manifest's `defaults` block.

| Field | Values |
| --- | --- |
| `output` | Must be `<category>/<id>.fbx`. |
| `lods` | Starts at `1.0`, then falling ratios. `[1.0, 0.5]` adds a decimated `<name>_LOD1`, and Unity builds the LODGroup from those names. |
| `collider` | `box`, `mesh`, `convex`, or `none`. The Unity importer adds it to LOD0 only. |
| `pivot` | `bottom` (origin on the floor), `center` (bounds centre at the world origin), or `authored` (leave what the generator set). |
| `rig` | `humanoid` skins the mesh to the shared armature and exports its clips. Rigged models keep one LOD. |

## Generator contract

A generator is a function `build_<thing>(ctx)` that builds into a freshly reset scene and **returns
the mesh object**. It must not call `reset_scene` or `export_fbx`: the runner resets, applies the
pivot, rig, and LODs, exports, and bakes. `ctx` carries `id`, `category`, `tags`, `output`, and
the full `entry`.

```python
def build_road_cone(ctx):
    orange = get_or_create_material("Mat_Cone_Orange", (0.9, 0.35, 0.08, 1.0), roughness=0.6)
    white = get_or_create_material("Mat_Cone_Stripe", (0.92, 0.92, 0.9, 1.0), roughness=0.5)
    base = create_box("ConeBase", (0, 0, 0.02), (0.4, 0.4, 0.04), orange)
    body = create_cone("ConeBody", (0, 0, 0.36), 0.16, 0.03, 0.64, vertices=16, material=orange)
    stripe = create_cone("ConeStripe", (0, 0, 0.42), 0.12, 0.09, 0.1, vertices=16, material=white)
    return join_objects([base, body, stripe], "Prop_Road_Cone")
```

Add the function to a `generate_*.py` module, add a manifest entry, then build it with
`--only Prop_Road_Cone` and commit the FBX, texture set, sidecar, and cache together.

## Reproducible bytes

Building the same entry twice gives byte-identical FBX and PNG files, in any order, in any process.
`blender_utils.pin_fbx_exporter` fixes the FBX header time and hashes object ids from their names
rather than Python's per-process string hash, and `canonical_faces` puts the UV sphere's faces into
a fixed order before unwrapping. CI rebuilds everything into a temp folder and fails if any committed
FBX or PNG differs.

## Change detection

An entry's fingerprint is a SHA-256 over its effective manifest entry, its generator module's source,
the shared build code (`pipeline.py`, `blender_utils.py`, `blender_paths.py`, `texture_set.py`,
`character_rig.py`, `character_detail.py`, `kit_catalog.py`), and the manifest's pinned Blender
version, axes, and texture settings. Line endings are normalised, so a Windows checkout does not
look changed. Editing one generator module rebuilds only that module's entries, and editing shared
code rebuilds everything. `test_pipeline_plan.py` fails if the committed cache is out of date.

## Sidecar

`Assets/Models/<Category>/<id>.meta.json` records the id, generator, `generatorHash`, `gitSha`,
Blender version, tags, pivot, collider, rig, LOD ratios, `tris` and `lodTris`, size in metres
(`width`, `depth`, `height`), floor height, and source material names. Unity's
`FbxPrefabPostprocessor` reads it through `ModelSidecar` to choose the prefab collider and remap the
listed materials onto the baked surface material.

## Tests without Blender

```powershell
python -m unittest discover -s BlenderScripts -p "test_*.py"
```
