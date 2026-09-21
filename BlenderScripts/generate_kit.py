"""Export every snap-kit piece. Run inside Blender."""

import os
import sys

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.append(script_dir)

import bpy

from blender_paths import models_dir
from blender_utils import create_box, export_fbx, get_or_create_material, join_objects, reset_scene
from kit_catalog import all_pieces, fbx_relative, repo_root

COLORS = {
    "concrete": (0.45, 0.43, 0.4, 1.0),
    "metal": (0.32, 0.34, 0.36, 1.0),
    "wood": (0.4, 0.26, 0.14, 1.0),
    "glass": (0.55, 0.7, 0.75, 1.0),
}


def set_origin_at_world_zero(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")


def generate_piece(item):
    reset_scene()
    material = get_or_create_material(
        "Mat_{0}".format(item["surface"]),
        COLORS[item["surface"]],
        metallic=0.6 if item["surface"] == "metal" else 0.0,
        roughness=0.35 if item["surface"] == "metal" else 0.8,
    )
    parts = []
    for index, collider in enumerate(item["colliders"]):
        center = (
            collider["x"] + collider["w"] / 2.0,
            collider["z"] + collider["d"] / 2.0,
            collider["y"] + collider["h"] / 2.0,
        )
        size = (collider["w"], collider["d"], collider["h"])
        parts.append(create_box("{0}_{1}".format(item["id"], index), center, size, material))
    if len(parts) == 1:
        mesh = parts[0]
        mesh.name = "Kit_{0}".format(item["id"])
    else:
        mesh = join_objects(parts, "Kit_{0}".format(item["id"]))
    set_origin_at_world_zero(mesh)
    destination = os.path.join(models_dir("Kit"), "Kit_{0}.fbx".format(item["id"]))
    export_fbx(destination)
    return destination


def generate_all():
    written = []
    for item in all_pieces():
        written.append(generate_piece(item))
    register_manifest()
    return written


def register_manifest():
    import json
    path = os.path.join(repo_root(), "BlenderScripts", "assets.manifest.json")
    with open(path, encoding="utf-8") as handle:
        manifest = json.load(handle)
    assets = [entry for entry in manifest["assets"] if "/Kit/" not in entry.replace("\\", "/")]
    assets.extend(fbx_relative(item["id"]) for item in all_pieces())
    manifest["assets"] = assets
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(manifest, handle, indent=2)
        handle.write("\n")


if __name__ == "__main__":
    generate_all()
