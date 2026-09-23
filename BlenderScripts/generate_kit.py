"""Snap-kit pieces: one manifest entry per piece in kit_catalog. Run inside Blender."""

import os
import sys

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.append(script_dir)

import bpy

from blender_utils import create_box, get_or_create_material, join_objects
from kit_catalog import all_pieces

COLORS = {
    "concrete": (0.45, 0.43, 0.4, 1.0),
    "metal": (0.32, 0.34, 0.36, 1.0),
    "wood": (0.4, 0.26, 0.14, 1.0),
    "glass": (0.55, 0.7, 0.75, 1.0),
}

PREFIX = "Kit_"


def piece_for(asset_id):
    wanted = asset_id[len(PREFIX):] if asset_id.startswith(PREFIX) else asset_id
    for item in all_pieces():
        if item["id"] == wanted:
            return item
    raise KeyError("No kit piece named " + wanted)


def set_origin_at_world_zero(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")


def build_piece(ctx):
    item = piece_for(ctx.id)
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
        mesh.name = PREFIX + item["id"]
    else:
        mesh = join_objects(parts, PREFIX + item["id"])
    set_origin_at_world_zero(mesh)
    return mesh


if __name__ == "__main__":
    import pipeline
    sys.exit(pipeline.main(["--category", "Kit"]))
