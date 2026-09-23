"""Build Outpost Zero models from BlenderScripts/assets.manifest.json.

    blender -b -P BlenderScripts/pipeline.py -- --only Prop_Dumpster
    blender -b -P BlenderScripts/pipeline.py -- --category props --changed
    python3 BlenderScripts/pipeline.py --changed --dry-run --json plan.json
    python3 BlenderScripts/pipeline.py --changed --dry-run --cache base-cache.json

Each manifest entry names a ``module.build_x(ctx)`` generator that returns one object.
The runner resets the scene, calls it, applies the pivot, rig, and LODs, exports the FBX,
bakes the texture set, writes ``<id>.meta.json`` beside it, and records the entry's
fingerprint in Assets/Models/.pipeline-cache.json so ``--changed`` can skip it next time.
"""

import json
import os
import sys
import time
import traceback

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.insert(0, script_dir)

import pipeline_plan as plan

REQUIRED_BLENDER = (4, 2)


class BuildContext:
    """What a generator may read about the asset it is building."""

    def __init__(self, entry, output):
        self.entry = entry
        self.id = entry["id"]
        self.category = entry["category"]
        self.tags = tuple(entry.get("tags") or ())
        self.output = output


def log(event, **fields):
    fields["event"] = event
    print("[pipeline] " + json.dumps(fields, sort_keys=True))
    sys.stdout.flush()


def in_blender():
    try:
        import bpy  # noqa: F401
    except ImportError:
        return False
    return True


def apply_pivot(obj, pivot):
    import bpy
    from blender_utils import set_origin_to_bottom

    if pivot == "bottom":
        set_origin_to_bottom(obj)
    elif pivot == "center":
        bpy.ops.object.select_all(action="DESELECT")
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.origin_set(type="ORIGIN_GEOMETRY", center="BOUNDS")
        obj.location = (0.0, 0.0, 0.0)
        bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)


def add_lods(obj, lods):
    """Decimated copies named <name>_LOD1.. so Unity's importer builds the LODGroup itself."""
    import bpy

    if len(lods) < 2:
        return [obj.name]
    base = obj.name
    obj.name = base + "_LOD0"
    names = [obj.name]
    for level, ratio in enumerate(lods[1:], start=1):
        copy = obj.copy()
        copy.data = obj.data.copy()
        copy.name = "{0}_LOD{1}".format(base, level)
        bpy.context.scene.collection.objects.link(copy)
        modifier = copy.modifiers.new(name="Decimate", type="DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = ratio
        bpy.ops.object.select_all(action="DESELECT")
        copy.select_set(True)
        bpy.context.view_layer.objects.active = copy
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        names.append(copy.name)
    return names


def lod_triangles(obj_names):
    import bpy

    counts = []
    for name in obj_names:
        obj = bpy.data.objects.get(name)
        if obj is None or obj.type != "MESH":
            continue
        obj.data.calc_loop_triangles()
        counts.append(len(obj.data.loop_triangles))
    return counts


def build_one(entry, models_root):
    import importlib

    import blender_utils

    module_name, function_name = entry["generator"].split(".")
    module = importlib.import_module(module_name)
    build = getattr(module, function_name, None)
    if build is None:
        raise plan.PlanError("{0} has no function {1}".format(module_name, function_name))

    output = plan.output_path(models_root, entry)
    blender_utils.reset_scene()
    obj = build(BuildContext(entry, output))
    if obj is None or getattr(obj, "type", None) != "MESH":
        raise plan.PlanError("{0} must return the mesh object it built".format(entry["generator"]))
    apply_pivot(obj, entry.get("pivot", "bottom"))
    rigged = entry.get("rig") == "humanoid"
    if rigged:
        blender_utils.prepare_character(obj)
    names = add_lods(obj, list(entry.get("lods") or [1.0]))

    bake = entry.get("bake") or {}
    del blender_utils.EXPORTS[:]
    blender_utils.export_fbx(output, animated=rigged, texture_size=bake.get("size"), unwrap=bake.get("uv", True))
    stats = dict(blender_utils.EXPORTS[-1])
    stats["lodTris"] = lod_triangles(names)
    if bake.get("uv", True):
        import fbx_uv

        overlapping = fbx_uv.problems(output, entry["output"])
        if overlapping:
            raise plan.PlanError("; ".join(overlapping))
    if icon_rendered(entry.get("tags")):
        render_item_icon(output)
    return stats


def icon_rendered(tags):
    import icon_render

    return icon_render.rendered(tags)


def render_item_icon(output):
    """Items show in the pack and codex subjects in the codex, so their Icon map is a render of the mesh
    rather than an albedo crop."""
    import bpy
    import icon_render
    import texture_set

    pixels = icon_render.render(icon_render.mesh_triangles(bpy.context.scene.objects))
    size = icon_render.ICON_RENDER_SIZE
    with open(texture_set.map_paths(output)["Icon"], "wb") as handle:
        handle.write(texture_set.encode_png(size, size, pixels))


def require_blender():
    import bpy

    if bpy.app.version[:2] < REQUIRED_BLENDER:
        raise SystemExit("Outpost Zero assets need Blender {0}.{1} LTS or newer, found {2}.".format(
            REQUIRED_BLENDER[0], REQUIRED_BLENDER[1], bpy.app.version_string))
    return bpy.app.version_string.split(" ")[0]


def main(argv=None):
    if argv is None:
        argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    try:
        options = plan.parse_args(argv)
    except plan.PlanError as error:
        log("error", message=str(error))
        return 2

    root = plan.repo_root()
    manifest = plan.load_manifest(root)
    problems = plan.validate(manifest)
    if problems:
        for problem in problems:
            log("invalid", message=problem)
        return 2

    try:
        chosen = plan.select(manifest, options["only"], options["categories"])
    except plan.PlanError as error:
        log("error", message=str(error))
        return 2

    models_root = options["models_dir"] or os.path.join(root, plan.MODELS)
    writes_repo = options["models_dir"] is None
    if options["cache"]:
        cache = plan.load_cache(root, options["cache"])
    else:
        cache = plan.load_cache(root) if writes_repo else {}
    if options["changed"]:
        chosen = plan.changed(root, manifest, chosen, cache, models_root)

    planned = [entry["id"] for entry in chosen]
    if options["json"]:
        with open(options["json"], "w", encoding="utf-8", newline="\n") as handle:
            json.dump({"assets": planned, "outputs": ["Assets/Models/" + entry["output"] for entry in chosen]}, handle, indent=2)
            handle.write("\n")
    log("plan", count=len(planned), assets=planned, changed=options["changed"], dry_run=options["dry_run"])
    if options["dry_run"]:
        print(plan.summary_table([{"id": name, "status": "planned", "seconds": 0.0, "tris": ""} for name in planned]))
        return 0
    if not in_blender():
        log("error", message="Building needs Blender: blender -b -P BlenderScripts/pipeline.py -- ...")
        return 2

    blender = require_blender()
    os.environ["OUTPOST_MODELS_DIR"] = models_root
    sha = plan.git_sha(root)
    rows = []
    started = time.time()
    for entry in chosen:
        began = time.time()
        digest = plan.fingerprint(root, manifest, entry)
        try:
            stats = build_one(entry, models_root)
            record = plan.sidecar(entry, stats, digest, sha, blender)
            plan.write_sidecar(plan.sidecar_path(models_root, entry), record)
            if writes_repo:
                cache[entry["id"]] = digest
            seconds = time.time() - began
            rows.append({"id": entry["id"], "status": "ok", "seconds": seconds, "tris": stats["tris"]})
            log("built", id=entry["id"], seconds=round(seconds, 3), tris=stats["tris"])
        except Exception as error:
            seconds = time.time() - began
            rows.append({"id": entry["id"], "status": "FAILED", "seconds": seconds, "tris": ""})
            log("failed", id=entry["id"], seconds=round(seconds, 3), message=str(error))
            traceback.print_exc()

    if writes_repo:
        plan.save_cache(root, cache)
    failed = [row["id"] for row in rows if row["status"] != "ok"]
    print(plan.summary_table(rows))
    log("done", built=len(rows) - len(failed), failed=failed, seconds=round(time.time() - started, 3))
    return 1 if failed else 0


if __name__ == "__main__":
    sys.exit(main())
