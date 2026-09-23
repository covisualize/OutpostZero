"""Write Assets/Resources/KitPrefabs.asset: every kit catalog id pointing at its generated prefab.

KitStructure loads it at runtime to raise districts from the baked kit meshes. Unity's
"Tools/Outpost Zero/Sync Kit Prefabs" does the same from inside the editor.

    python3 BlenderScripts/kit_prefab_set.py          # report
    python3 BlenderScripts/kit_prefab_set.py --fix    # rewrite the asset
"""

import json
import os
import re
import sys

KIT_PREFAB_SET_GUID = "c75b3bf8091a2b3c4d5e6f708192a469"
ASSET_GUID = "c75b3cf8091a2b3c4d5e6f708192a46a"
ROOT_OBJECT = "919132149155446097"
ASSET = "Assets/Resources/KitPrefabs.asset"
PREFABS = "Assets/Prefabs/Kit"
CATALOG = "Assets/Resources/KitCatalog.json"


def repo_root():
    return os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))


def meta_guid(meta_text):
    match = re.search(r"^guid: ([0-9a-f]{32})\s*$", meta_text, re.M)
    return match.group(1) if match else None


def root_object(prefab_text):
    """fileID of the prefab's root GameObject, the object a reference to the prefab asset names."""
    blocks = re.split(r"^(?=--- )", prefab_text, flags=re.M)
    for block in blocks:
        head = re.match(r"--- !u!1 &(-?\d+) stripped\n", block)
        if head and "m_CorrespondingSourceObject: {fileID: " + ROOT_OBJECT + "," in block:
            return head.group(1)
    return None


def entries(root):
    with open(os.path.join(root, CATALOG), encoding="utf-8") as handle:
        pieces = json.load(handle)["pieces"]
    found, missing = [], []
    for piece in pieces:
        path = os.path.join(root, PREFABS, "Kit_" + piece["id"] + ".prefab")
        if not os.path.exists(path) or not os.path.exists(path + ".meta"):
            missing.append(piece["id"])
            continue
        with open(path + ".meta", encoding="utf-8") as handle:
            guid = meta_guid(handle.read())
        with open(path, encoding="utf-8") as handle:
            file_id = root_object(handle.read())
        if not guid or not file_id:
            missing.append(piece["id"])
            continue
        found.append((piece["id"], file_id, guid))
    return found, missing


def render(found):
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        "  m_Script: {fileID: 11500000, guid: " + KIT_PREFAB_SET_GUID + ", type: 3}",
        "  m_Name: KitPrefabs",
        "  m_EditorClassIdentifier: OutpostZero::OutpostZero.Expedition.KitPrefabSet",
        "  entries:",
    ]
    for piece_id, file_id, guid in found:
        lines.append("  - id: " + piece_id)
        lines.append("    prefab: {fileID: " + file_id + ", guid: " + guid + ", type: 3}")
    return "\n".join(lines) + "\n"


def render_meta():
    return "\n".join([
        "fileFormatVersion: 2",
        "guid: " + ASSET_GUID,
        "NativeFormatImporter:",
        "  externalObjects: {}",
        "  mainObjectFileID: 11400000",
        "  userData: ",
        "  assetBundleName: ",
        "  assetBundleVariant: ",
    ]) + "\n"


def main(argv):
    root = repo_root()
    found, missing = entries(root)
    path = os.path.join(root, ASSET)
    text = render(found)
    current = open(path, encoding="utf-8").read() if os.path.exists(path) else ""
    for piece_id in missing:
        print("missing prefab for kit piece " + piece_id)
    if current == text:
        print("KitPrefabs.asset is current ({0} pieces)".format(len(found)))
        return 1 if missing else 0
    if "--fix" not in argv:
        print("KitPrefabs.asset is stale ({0} pieces); run with --fix".format(len(found)))
        return 1
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)
    with open(path + ".meta", "w", encoding="utf-8", newline="\n") as handle:
        handle.write(render_meta())
    print("wrote KitPrefabs.asset ({0} pieces)".format(len(found)))
    return 1 if missing else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
