"""Check the committed model set: manifest, textures, UVs, and triangle budgets."""

import json
import os
import struct
import sys
import zlib

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from icon_render import rendered as icon_rendered  # noqa: E402

TEXTURES = ("Albedo", "Normal", "AO", "Mask", "Icon")
RENDERED_ICON = 64
MIN_ICON_COVERAGE = 0.08
BUDGETS = (
    ("/Characters/", 6000),
    ("/Kit/", 1500),
    ("/Props/", 2000),
    ("/Weapons/", 2000),
    ("/Environment/", 8000),
    ("/BaseBuilding/", 8000),
)


def repo_root():
    return os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))


def load_manifest(root):
    path = os.path.join(root, "BlenderScripts", "assets.manifest.json")
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


def triangle_count(path):
    with open(path, "rb") as handle:
        data = handle.read()
    if not data.startswith(b"Kaydara FBX Binary"):
        return None
    version = struct.unpack_from("<I", data, 23)[0]
    wide = version >= 7500
    total = 0
    found = False

    def walk(start, end):
        nonlocal total, found
        header = 25 if wide else 13
        sentinel = 25 if wide else 13
        pos = start
        while pos + header <= end:
            if wide:
                end_offset, num_props, prop_len = struct.unpack_from("<QQQ", data, pos)
            else:
                end_offset, num_props, prop_len = struct.unpack_from("<III", data, pos)
            if end_offset == 0:
                return
            name_len = data[pos + (24 if wide else 12)]
            name_at = pos + (25 if wide else 13)
            name = data[name_at:name_at + name_len]
            prop_at = name_at + name_len
            if name == b"PolygonVertexIndex" and num_props >= 1 and data[prop_at:prop_at + 1] == b"i":
                values = _int_array(data, prop_at)
                total += _triangles(values)
                found = True
            child = prop_at + prop_len
            if child < end_offset:
                walk(child, end_offset)
            pos = end_offset
            if pos < start + sentinel:
                return

    walk(27, len(data))
    return total if found else None


def _int_array(data, offset):
    length, encoding, comp = struct.unpack_from("<III", data, offset + 1)
    raw = data[offset + 13:offset + 13 + comp]
    if encoding == 1:
        raw = zlib.decompress(raw)
    return struct.unpack_from("<" + str(length) + "i", raw)


def _triangles(values):
    count = 0
    run = 0
    for value in values:
        run += 1
        if value < 0:
            count += max(0, run - 2)
            run = 0
    return count


def icon_coverage(path):
    from icon_render import coverage
    from texture_set import decode_png

    with open(path, "rb") as handle:
        _w, _h, pixels = decode_png(handle.read())
    return coverage(pixels)


def budget_for(relative):
    for fragment, limit in BUDGETS:
        if fragment in relative.replace("\\", "/"):
            return limit
    return 8000


def png_size(path):
    with open(path, "rb") as handle:
        header = handle.read(24)
    if header[:8] != b"\x89PNG\r\n\x1a\n":
        return None
    return struct.unpack(">II", header[16:24])


def sidecar_problems(relative, path, entry, tris):
    """The sidecar must describe the FBX beside it: same id, settings, and triangle total."""
    sidecar = path[:-4] + ".meta.json"
    if not os.path.isfile(sidecar):
        return [relative + " missing sidecar"]
    problems = []
    if not os.path.isfile(sidecar + ".meta"):
        problems.append(relative + " missing sidecar meta")
    with open(sidecar, encoding="utf-8") as handle:
        record = json.load(handle)
    for field in ("id", "category", "generator", "collider", "pivot", "lods"):
        if record.get(field) != entry.get(field):
            problems.append(relative + " sidecar " + field + " is stale")
    lod_tris = record.get("lodTris") or []
    if len(lod_tris) != len(entry.get("lods") or [1.0]):
        problems.append(relative + " sidecar lod count")
    if tris is not None and sum(lod_tris) != tris:
        problems.append(relative + " sidecar tris " + str(sum(lod_tris)) + " but fbx has " + str(tris))
    return problems


def audit(root=None, manifest=None):
    from pipeline_plan import entries

    root = root or repo_root()
    if manifest is None:
        manifest = load_manifest(root)
    problems = []
    listed = set()
    textures = manifest.get("textures") or list(TEXTURES)
    full = int(manifest.get("textureSize") or 128)
    icon = 32

    for entry in entries(manifest):
        relative = "Assets/Models/" + entry["output"]
        listed.add(relative)
        path = os.path.join(root, relative)
        if not os.path.isfile(path):
            problems.append(relative + " missing fbx")
            continue
        if not os.path.isfile(path + ".meta"):
            problems.append(relative + " missing fbx meta")
        with open(path, "rb") as handle:
            blob = handle.read()
        text = blob.decode("latin1", errors="ignore")
        if "LayerElementUV" not in text:
            problems.append(relative + " missing uvs")
        if "/Characters/" in relative and "Hips" not in text:
            problems.append(relative + " missing hips")
        tris = triangle_count(path)
        limit = budget_for(relative)
        if tris is None:
            problems.append(relative + " unreadable mesh")
        elif tris > limit:
            problems.append(relative + " tris " + str(tris) + " over " + str(limit))
        problems.extend(sidecar_problems(relative, path, entry, tris))
        stem = relative[:-4]
        for suffix in textures:
            map_path = os.path.join(root, stem + "_" + suffix + ".png")
            meta_path = map_path + ".meta"
            if not os.path.isfile(map_path):
                problems.append(stem + "_" + suffix + " missing texture")
                continue
            size = png_size(map_path)
            rendered = suffix == "Icon" and icon_rendered(entry.get("tags"))
            side = RENDERED_ICON if rendered else icon
            expected = (side, side) if suffix == "Icon" else (full, full)
            if size != expected:
                problems.append(stem + "_" + suffix + " size " + str(size))
            elif rendered and icon_coverage(map_path) < MIN_ICON_COVERAGE:
                problems.append(stem + "_Icon blank render")
            if not os.path.isfile(meta_path):
                problems.append(stem + "_" + suffix + " missing meta")
                continue
            with open(meta_path, encoding="utf-8") as handle:
                meta = handle.read()
            if suffix == "Normal" and "textureType: 1" not in meta:
                problems.append(stem + "_Normal importer")
            if suffix in ("Normal", "AO", "Mask") and "sRGBTexture: 0" not in meta:
                problems.append(stem + "_" + suffix + " srgb")
            if suffix in ("Albedo", "Icon") and "sRGBTexture: 1" not in meta:
                problems.append(stem + "_" + suffix + " srgb")

    models = os.path.join(root, "Assets", "Models")
    if os.path.isdir(models):
        for folder, _dirs, files in os.walk(models):
            for name in files:
                if not name.endswith(".fbx"):
                    continue
                full_path = os.path.join(folder, name)
                relative = os.path.relpath(full_path, root).replace("\\", "/")
                if relative not in listed:
                    problems.append(relative + " orphan fbx")

    builder = os.path.join(root, "Assets", "Scripts", "Editor", "PrototypeSceneBuilder.cs")
    if os.path.isfile(builder):
        with open(builder, encoding="utf-8") as handle:
            problems.extend(scene_paths(root, handle.read()))
    paths = os.path.join(root, "Assets", "Scripts", "Core", "ModelPaths.cs")
    if os.path.isfile(paths):
        with open(paths, encoding="utf-8") as handle:
            problems.extend(scene_paths(root, handle.read()))
    return problems


def scene_paths(root, source):
    problems = []
    cursor = 0
    while True:
        start = source.find(".fbx", cursor)
        if start < 0:
            break
        quote = max(source.rfind('"', 0, start), source.rfind("'", 0, start))
        cursor = start + 4
        if quote < 0:
            continue
        token = source[quote + 1:start + 4]
        if token.startswith("Assets/"):
            relative = token
        else:
            relative = "Assets/Models/" + token
        if not os.path.isfile(os.path.join(root, relative)):
            problems.append(relative + " scene path missing")
    return problems
