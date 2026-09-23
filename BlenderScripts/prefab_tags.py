"""Keep each generated prefab's SurfaceTag and root layer in step with its sidecar.

Mirrors OutpostZero.Core.SurfaceTag.Guess and GameLayers.ForAsset; the Unity artifact suite
fails if the two drift. Unity's postprocessor does the same on import; this writes the YAML so
prefabs are right without opening the editor.

    python3 BlenderScripts/prefab_tags.py          # report
    python3 BlenderScripts/prefab_tags.py --fix    # rewrite drifted prefabs
"""

import hashlib
import json
import os
import re
import sys

SURFACE_TAG_GUID = "c75b34f8091a2b3c4d5e6f708192a462"
ROOT_OBJECT = "919132149155446097"

SURFACES = {"Default": 0, "Concrete": 1, "Metal": 2, "Wood": 3, "Glass": 4, "Gravel": 5, "Water": 6, "Flesh": 7}
ORDER = ("Concrete", "Metal", "Wood", "Glass", "Gravel")
WORDS = {
    "Concrete": ("concrete", "asphalt", "road", "sidewalk", "curb", "brick", "plaster", "foundation", "tile", "bldg", "ruin", "stone", "rubble"),
    "Metal": ("metal", "steel", "iron", "rust", "gunmetal", "corrugated", "tin", "brass", "car", "truck", "dumpster", "barrel", "ibeam", "gen"),
    "Wood": ("wood", "timber", "plank", "crate", "bench", "log"),
    "Glass": ("glass",),
    "Gravel": ("sand", "gravel", "dirt", "soil"),
}
CATEGORY_SURFACE = {"Characters": "Flesh", "BaseBuilding": "Wood", "Props": "Metal", "Weapons": "Metal", "Kit": "Concrete", "Environment": "Concrete"}
LAYERS = {"Environment": 8, "Loot": 9, "Enemy": 7}


def repo_root():
    return os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))


def guess(category, materials):
    fallback = CATEGORY_SURFACE.get(category, "Default")
    if fallback == "Flesh":
        return fallback
    votes = {}
    for material in materials or ():
        for token in material.lower().split("_"):
            if not token or token == "mat":
                continue
            for kind in ORDER:
                if any(word in token for word in WORDS[kind]):
                    votes[kind] = votes.get(kind, 0) + 1
                    break
    if not votes:
        return fallback
    best = max(votes.values())
    for kind in ORDER:
        if votes.get(kind) == best:
            return kind
    return fallback


def layer_for(category, asset_id):
    if category in ("Kit", "Environment", "Props", "BaseBuilding"):
        return LAYERS["Environment"]
    if category == "Weapons":
        return LAYERS["Loot"] if asset_id.startswith("Loot_") else 0
    if category == "Characters":
        return LAYERS["Enemy"] if asset_id.startswith("Zombie_") else 0
    return 0


def component_id(prefab_path):
    digest = hashlib.md5(("surface:" + prefab_path.replace("\\", "/")).encode("utf-8")).hexdigest()
    return str(int(digest[:15], 16) + 1)


def read_state(text):
    layer = re.search(r"target: \{fileID: " + ROOT_OBJECT + r", guid: \w+, type: 3\}\n\s+propertyPath: m_Layer\n\s+value: (\d+)", text)
    kind = re.search(r"m_Script: \{fileID: 11500000, guid: " + SURFACE_TAG_GUID + r", type: 3\}\n(?:.*\n)*?\s+kind: (\d+)", text)
    return (int(layer.group(1)) if layer else 0), (int(kind.group(1)) if kind else None)


def rewrite(text, prefab_path, layer, kind):
    source = re.search(r"m_SourcePrefab: \{fileID: 100100000, guid: (\w+), type: 3\}", text).group(1)
    stripped = re.search(r"--- !u!1 &(\d+) stripped", text).group(1)
    layer_mod = re.compile(r"    - target: \{fileID: " + ROOT_OBJECT + r", guid: \w+, type: 3\}\n\s+propertyPath: m_Layer\n\s+value: \d+\n\s+objectReference: \{fileID: 0\}\n")
    text = layer_mod.sub("", text)
    if layer:
        block = ("    - target: {fileID: " + ROOT_OBJECT + ", guid: " + source + ", type: 3}\n"
                 "      propertyPath: m_Layer\n      value: " + str(layer) + "\n      objectReference: {fileID: 0}\n")
        text = text.replace("    m_RemovedComponents:", block + "    m_RemovedComponents:", 1)
    existing = re.search(r"(m_Script: \{fileID: 11500000, guid: " + SURFACE_TAG_GUID + r", type: 3\}\n(?:.*\n)*?\s+kind: )(\d+)", text)
    if existing:
        return text[:existing.start(2)] + str(kind) + text[existing.end(2):]
    cid = component_id(prefab_path)
    added = ("    - targetCorrespondingSourceObject: {fileID: " + ROOT_OBJECT + ", guid: " + source + ", type: 3}\n"
             "      insertIndex: -1\n      addedObject: {fileID: " + cid + "}\n")
    if "    m_AddedComponents: []\n" in text:
        text = text.replace("    m_AddedComponents: []\n", "    m_AddedComponents:\n" + added, 1)
    else:
        text = re.sub(r"(    m_AddedComponents:\n(?:    - .*\n(?:      .*\n)*)*)", lambda found: found.group(1) + added, text, count=1)
    document = ("--- !u!114 &" + cid + "\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n"
                "  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: " + stripped + "}\n"
                "  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {fileID: 11500000, guid: " + SURFACE_TAG_GUID + ", type: 3}\n"
                "  m_Name: \n  m_EditorClassIdentifier: \n  kind: " + str(kind) + "\n")
    if not text.endswith("\n"):
        text += "\n"
    return text + document


def audit(root, fix=False):
    with open(os.path.join(root, "BlenderScripts", "assets.manifest.json"), encoding="utf-8") as handle:
        manifest = json.load(handle)
    report = []
    for entry in manifest["entries"]:
        stem = entry["output"][:-4]
        relative = "Assets/Prefabs/" + stem + ".prefab"
        prefab = os.path.join(root, relative)
        sidecar_path = os.path.join(root, "Assets", "Models", stem + ".meta.json")
        if not os.path.exists(prefab) or not os.path.exists(sidecar_path):
            continue
        with open(sidecar_path, encoding="utf-8") as handle:
            sidecar = json.load(handle)
        want_kind = SURFACES[guess(sidecar["category"], sidecar.get("materials"))]
        want_layer = layer_for(sidecar["category"], sidecar["id"])
        with open(prefab, encoding="utf-8", newline="") as handle:
            text = handle.read()
        layer, kind = read_state(text)
        if layer == want_layer and kind == want_kind:
            continue
        report.append("{0}: layer {1} want {2}, surface {3} want {4}".format(relative, layer, want_layer, kind, want_kind))
        if fix:
            with open(prefab, "w", encoding="utf-8", newline="") as handle:
                handle.write(rewrite(text, relative, want_layer, want_kind))
    return report


if __name__ == "__main__":
    lines = audit(repo_root(), fix="--fix" in sys.argv)
    for line in lines:
        print(line)
    sys.exit(1 if lines and "--fix" not in sys.argv else 0)
