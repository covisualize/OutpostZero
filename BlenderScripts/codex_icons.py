"""Write Assets/Resources/CodexIcons.asset: every manifest entry tagged "codex" pointing at its rendered icon.

The codex screen loads it to show the mesh render beside each zombie, module and faction entry.

    python3 BlenderScripts/codex_icons.py          # report
    python3 BlenderScripts/codex_icons.py --fix    # rewrite the asset
"""

import json
import os
import re
import sys

CODEX_ICONS_GUID = "c75b42f8091a2b3c4d5e6f708192a470"
ASSET_GUID = "c75b43f8091a2b3c4d5e6f708192a471"
TEXTURE_FILE_ID = "2800000"
ASSET = "Assets/Resources/CodexIcons.asset"
MANIFEST = "BlenderScripts/assets.manifest.json"
TAG = "codex"


def repo_root():
    return os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))


def meta_guid(meta_text):
    match = re.search(r"^guid: ([0-9a-f]{32})\s*$", meta_text, re.M)
    return match.group(1) if match else None


def icon_path(output):
    return os.path.join("Assets", "Models", os.path.splitext(output)[0] + "_Icon.png")


def entries(root):
    with open(os.path.join(root, MANIFEST), encoding="utf-8") as handle:
        manifest = json.load(handle)
    found, missing = [], []
    for entry in manifest["entries"]:
        if TAG not in (entry.get("tags") or ()):
            continue
        meta = os.path.join(root, icon_path(entry["output"])) + ".meta"
        guid = None
        if os.path.exists(meta):
            with open(meta, encoding="utf-8") as handle:
                guid = meta_guid(handle.read())
        if not guid:
            missing.append(entry["id"])
            continue
        found.append((entry["id"], guid))
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
        "  m_Script: {fileID: 11500000, guid: " + CODEX_ICONS_GUID + ", type: 3}",
        "  m_Name: CodexIcons",
        "  m_EditorClassIdentifier: OutpostZero::OutpostZero.Shell.CodexIcons",
        "  entries:",
    ]
    for model_id, guid in found:
        lines.append("  - id: " + model_id)
        lines.append("    icon: {fileID: " + TEXTURE_FILE_ID + ", guid: " + guid + ", type: 3}")
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
    for model_id in missing:
        print("missing icon for codex model " + model_id)
    if current == text:
        print("CodexIcons.asset is current ({0} icons)".format(len(found)))
        return 1 if missing else 0
    if "--fix" not in argv:
        print("CodexIcons.asset is stale ({0} icons); run with --fix".format(len(found)))
        return 1
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)
    with open(path + ".meta", "w", encoding="utf-8", newline="\n") as handle:
        handle.write(render_meta())
    print("wrote CodexIcons.asset ({0} icons)".format(len(found)))
    return 1 if missing else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
