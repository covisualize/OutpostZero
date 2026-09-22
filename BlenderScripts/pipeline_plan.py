"""Manifest, selection, fingerprints, cache, and sidecars for pipeline.py.

Nothing here imports bpy, so plans, dry runs, and tests work without Blender.
"""

import hashlib
import json
import os

MANIFEST = os.path.join("BlenderScripts", "assets.manifest.json")
CACHE = os.path.join("Assets", "Models", ".pipeline-cache.json")
MODELS = os.path.join("Assets", "Models")
SIDECAR = ".meta.json"

COLLIDERS = ("box", "mesh", "convex", "none")
PIVOTS = ("bottom", "center", "authored")
RIGS = (None, "humanoid")
TEXTURE_NAMES = ("albedo", "normal", "ao", "mask", "icon")

# Sources every build runs through. A change to any of them invalidates every entry.
SHARED_SOURCES = (
    "pipeline.py",
    "blender_utils.py",
    "blender_paths.py",
    "texture_set.py",
    "character_rig.py",
    "character_detail.py",
    "kit_catalog.py",
)

ALIASES = {
    "architecture": "Environment",
    "base": "BaseBuilding",
}


class PlanError(Exception):
    pass


def repo_root():
    from blender_paths import get_repo_root

    return get_repo_root(os.path.dirname(os.path.abspath(__file__)))


def load_manifest(root):
    with open(os.path.join(root, MANIFEST), encoding="utf-8") as handle:
        return json.load(handle)


def save_manifest(root, manifest):
    with open(os.path.join(root, MANIFEST), "w", encoding="utf-8", newline="\n") as handle:
        json.dump(manifest, handle, indent=2)
        handle.write("\n")


DEFAULTS = {
    "tags": [],
    "bake": {"uv": True, "textures": list(TEXTURE_NAMES), "size": 128},
    "lods": [1.0],
    "collider": "box",
    "pivot": "bottom",
}


def effective(manifest, entry):
    """An entry with the manifest's defaults filled in. Entries only spell out what differs."""
    defaults = dict(DEFAULTS)
    defaults.update(manifest.get("defaults") or {})
    merged = dict(defaults)
    merged.update(entry)
    bake = dict(DEFAULTS["bake"])
    bake.update(defaults.get("bake") or {})
    bake.update(entry.get("bake") or {})
    merged["bake"] = bake
    return merged


def entries(manifest):
    return [effective(manifest, entry) for entry in manifest.get("entries") or []]


def asset_paths(manifest):
    """Repo-relative FBX paths, in manifest order."""
    return ["Assets/Models/" + entry["output"] for entry in entries(manifest)]


def output_path(models_root, entry):
    return os.path.join(models_root, *entry["output"].split("/"))


def sidecar_path(models_root, entry):
    folder, name = os.path.split(output_path(models_root, entry))
    return os.path.join(folder, os.path.splitext(name)[0] + SIDECAR)


def validate(manifest):
    """Return a list of human-readable problems. An empty list means the manifest is usable."""
    problems = []
    categories = set(manifest.get("categories") or [])
    seen = set()
    outputs = set()
    for index, entry in enumerate(entries(manifest)):
        label = entry.get("id") or "#{0}".format(index)
        for field in ("id", "category", "generator", "output"):
            if not entry.get(field):
                problems.append("{0}: missing {1}".format(label, field))
        if label in seen:
            problems.append("{0}: duplicate id".format(label))
        seen.add(label)
        output = entry.get("output") or ""
        if output in outputs:
            problems.append("{0}: output {1} is written twice".format(label, output))
        outputs.add(output)
        if entry.get("category") and entry["category"] not in categories:
            problems.append("{0}: category {1} is not listed".format(label, entry["category"]))
        if output and output != "{0}/{1}.fbx".format(entry.get("category"), entry.get("id")):
            problems.append("{0}: output must be <category>/<id>.fbx, got {1}".format(label, output))
        generator = entry.get("generator") or ""
        if generator.count(".") != 1:
            problems.append("{0}: generator must be module.function, got {1}".format(label, generator))
        if entry.get("collider", "box") not in COLLIDERS:
            problems.append("{0}: collider must be one of {1}".format(label, "|".join(COLLIDERS)))
        if entry.get("pivot", "bottom") not in PIVOTS:
            problems.append("{0}: pivot must be one of {1}".format(label, "|".join(PIVOTS)))
        if entry.get("rig") not in RIGS:
            problems.append("{0}: rig must be humanoid or absent".format(label))
        lods = entry.get("lods") or [1.0]
        if lods[0] != 1.0 or any(not 0.0 < ratio <= 1.0 for ratio in lods) or sorted(lods, reverse=True) != lods:
            problems.append("{0}: lods must start at 1.0 and fall in (0, 1]".format(label))
        if entry.get("rig") and len(lods) > 1:
            problems.append("{0}: rigged models carry one LOD".format(label))
        bake = entry.get("bake") or {}
        unknown = [name for name in bake.get("textures") or [] if name not in TEXTURE_NAMES]
        if unknown:
            problems.append("{0}: unknown textures {1}".format(label, ", ".join(unknown)))
    return problems


def category_for(name, manifest):
    wanted = ALIASES.get(name.lower(), name)
    for category in manifest.get("categories") or []:
        if category.lower() == wanted.lower():
            return category
    raise PlanError("Unknown category {0}. Choose from {1}.".format(name, ", ".join(manifest.get("categories") or [])))


def select(manifest, only=None, categories=None):
    """Entries named by --only and/or --category, in manifest order. Unknown names are an error."""
    chosen = entries(manifest)
    if categories:
        wanted = {category_for(name, manifest) for name in categories}
        chosen = [entry for entry in chosen if entry["category"] in wanted]
    if only:
        known = {entry["id"] for entry in entries(manifest)}
        missing = [name for name in only if name not in known]
        if missing:
            raise PlanError("Unknown asset id(s): {0}".format(", ".join(missing)))
        picked = set(only)
        chosen = [entry for entry in chosen if entry["id"] in picked]
    return chosen


def _digest_file(hasher, path):
    hasher.update(os.path.basename(path).encode("utf-8"))
    with open(path, "rb") as handle:
        hasher.update(handle.read().replace(b"\r\n", b"\n"))


def fingerprint(root, manifest, entry):
    """Hash of everything that decides the bytes of one asset: its entry, its generator module,
    the shared build code, and the Blender version and texture settings the manifest pins."""
    scripts = os.path.join(root, "BlenderScripts")
    hasher = hashlib.sha256()
    pinned = {key: manifest.get(key) for key in ("blender", "axis", "export", "textures", "textureSize")}
    hasher.update(json.dumps(pinned, sort_keys=True).encode("utf-8"))
    hasher.update(json.dumps(entry, sort_keys=True).encode("utf-8"))
    module = entry["generator"].split(".")[0]
    _digest_file(hasher, os.path.join(scripts, module + ".py"))
    for name in SHARED_SOURCES:
        path = os.path.join(scripts, name)
        if os.path.isfile(path):
            _digest_file(hasher, path)
    return hasher.hexdigest()


def load_cache(root, path=None):
    path = path or os.path.join(root, CACHE)
    if not os.path.isfile(path):
        return {}
    try:
        with open(path, encoding="utf-8") as handle:
            data = json.load(handle)
    except (OSError, ValueError):
        return {}
    return dict(data.get("entries") or {})


def save_cache(root, cache):
    path = os.path.join(root, CACHE)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        json.dump({"entries": dict(sorted(cache.items()))}, handle, indent=2)
        handle.write("\n")


def changed(root, manifest, chosen, cache, models_root=None):
    """Entries whose fingerprint moved since the last build, or whose FBX is missing."""
    models_root = models_root or os.path.join(root, MODELS)
    stale = []
    for entry in chosen:
        if cache.get(entry["id"]) != fingerprint(root, manifest, entry):
            stale.append(entry)
        elif not os.path.isfile(output_path(models_root, entry)):
            stale.append(entry)
    return stale


def sidecar(entry, stats, digest, git_sha, blender):
    """The per-asset record Unity's importer reads next to the FBX."""
    return {
        "id": entry["id"],
        "category": entry["category"],
        "generator": entry["generator"],
        "generatorHash": digest,
        "gitSha": git_sha,
        "blender": blender,
        "tags": list(entry.get("tags") or []),
        "pivot": entry.get("pivot", "bottom"),
        "collider": entry.get("collider", "box"),
        "rig": entry.get("rig") or "",
        "lods": list(entry.get("lods") or [1.0]),
        "tris": int(stats.get("tris", 0)),
        "lodTris": list(stats.get("lodTris") or [int(stats.get("tris", 0))]),
        "size": {
            "width": stats.get("width", 0.0),
            "depth": stats.get("depth", 0.0),
            "height": stats.get("height", 0.0),
        },
        "floor": stats.get("floor", 0.0),
        "materials": list(stats.get("materials") or []),
    }


def write_sidecar(path, record):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(record, handle, indent=2)
        handle.write("\n")
    with open(path + ".meta", "w", encoding="utf-8", newline="\n") as handle:
        handle.write(sidecar_meta(path))


def sidecar_meta(path):
    """Unity .meta for a sidecar, with a GUID fixed by its path so rebuilds never churn it."""
    normalized = path.replace("\\", "/")
    index = normalized.find("Assets/Models/")
    key = normalized[index:] if index >= 0 else os.path.basename(normalized)
    guid = hashlib.md5(key.encode("utf-8")).hexdigest()
    return (
        "fileFormatVersion: 2\n"
        "guid: {0}\n"
        "TextScriptImporter:\n"
        "  externalObjects: {{}}\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    ).format(guid)


def git_sha(root):
    head = os.path.join(root, ".git", "HEAD")
    try:
        with open(head, encoding="utf-8") as handle:
            ref = handle.read().strip()
        if ref.startswith("ref: "):
            name = ref[5:]
            loose = os.path.join(root, ".git", *name.split("/"))
            if os.path.isfile(loose):
                with open(loose, encoding="utf-8") as handle:
                    return handle.read().strip()
            packed = os.path.join(root, ".git", "packed-refs")
            if os.path.isfile(packed):
                with open(packed, encoding="utf-8") as handle:
                    for line in handle:
                        parts = line.strip().split(" ")
                        if len(parts) == 2 and parts[1] == name:
                            return parts[0]
            return "unknown"
        return ref
    except OSError:
        return "unknown"


def summary_table(rows):
    """rows: dicts with id, status, seconds, tris. Returns a fixed-width table."""
    header = ("asset", "status", "seconds", "tris")
    body = [(row["id"], row["status"], "{0:.2f}".format(row.get("seconds", 0.0)), str(row.get("tris", ""))) for row in rows]
    widths = [max(len(header[i]), *(len(line[i]) for line in body)) if body else len(header[i]) for i in range(4)]
    def fmt(line):
        return "  ".join(line[i].ljust(widths[i]) for i in range(4)).rstrip()
    lines = [fmt(header), "  ".join("-" * width for width in widths)]
    lines.extend(fmt(line) for line in body)
    return "\n".join(lines)


def parse_args(argv):
    """Arguments after Blender's ``--``, or all of argv when run as plain Python."""
    args = list(argv)
    if "--" in args:
        args = args[args.index("--") + 1:]
    options = {"only": None, "categories": None, "changed": False, "dry_run": False, "models_dir": None, "json": None, "cache": None}
    index = 0
    while index < len(args):
        token = args[index]
        value = args[index + 1] if index + 1 < len(args) else None
        if token == "--only" and value is not None:
            options["only"] = [part.strip() for part in value.split(",") if part.strip()]
            index += 2
        elif token == "--category" and value is not None:
            options["categories"] = [part.strip() for part in value.split(",") if part.strip()]
            index += 2
        elif token == "--models-dir" and value is not None:
            options["models_dir"] = os.path.abspath(value)
            index += 2
        elif token == "--cache" and value is not None:
            options["cache"] = os.path.abspath(value)
            index += 2
        elif token == "--json" and value is not None:
            options["json"] = value
            index += 2
        elif token == "--changed":
            options["changed"] = True
            index += 1
        elif token == "--dry-run":
            options["dry_run"] = True
            index += 1
        else:
            raise PlanError("Unknown argument {0}".format(token))
    if options["cache"] and not options["dry_run"]:
        raise PlanError("--cache compares against another build's cache and only works with --dry-run")
    return options
