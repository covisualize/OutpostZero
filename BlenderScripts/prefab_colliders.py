"""Keep each prefab's BoxCollider on the sidecar's bind-pose bounds.

Unity sizes a box from renderer bounds, and a skinned renderer's bounds cover every animation
pose, so characters came out two metres deep. The sidecar is measured in the bind pose.

    python3 BlenderScripts/prefab_colliders.py          # report
    python3 BlenderScripts/prefab_colliders.py --fix    # rewrite drifted prefabs
"""

import json
import math
import os
import re
import sys

SIZE_TOLERANCE = 0.1
CENTER_TOLERANCE = 0.05
MAX_TILT_DEGREES = 45.0
VECTOR = r"\{x: ([-\d.e]+), y: ([-\d.e]+), z: ([-\d.e]+)\}"
ROOT_TRANSFORM = "-8679921383154817045"


def repo_root():
    return os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))


def expected_box(sidecar, offset):
    """Unity (x, y, z) is Blender (-x, z, -y); the collider sits on the moved FBX root."""
    size = sidecar["size"]
    center = sidecar.get("center") or {"x": 0.0, "y": 0.0}
    middle = (-center["x"] - offset[0], sidecar.get("floor", 0.0) + size["height"] * 0.5 - offset[1], -center["y"] - offset[2])
    return middle, (size["width"], size["height"], size["depth"])


def root_value(text, prop):
    pattern = r"target: \{fileID: " + ROOT_TRANSFORM + r"[^\n]*\n\s+propertyPath: " + re.escape(prop) + r"\n\s+value: (\S+)"
    found = re.search(pattern, text)
    return float(found.group(1)) if found else 0.0


def root_offset(text):
    return tuple(root_value(text, "m_LocalPosition." + axis) for axis in "xyz")


def root_tilt(text):
    """Degrees the prefab turns its FBX root. Small tilts are set dressing; a quarter turn stands a cot on end."""
    w = min(1.0, abs(root_value(text, "m_LocalRotation.w")))
    return math.degrees(2.0 * math.acos(w))


def drift(text, sidecar):
    size = re.search(r"m_Size: " + VECTOR, text)
    middle = re.search(r"m_Center: " + VECTOR, text)
    if not size or not middle:
        return []
    tilt = root_tilt(text)
    if tilt > MAX_TILT_DEGREES:
        return ["root turned {0:.0f} degrees".format(tilt)]
    if tilt > 0.01:
        return []
    want_middle, want_size = expected_box(sidecar, root_offset(text))
    problems = []
    got_size = [float(v) for v in size.groups()]
    got_middle = [float(v) for v in middle.groups()]
    for axis, got, want in zip("xyz", got_size, want_size):
        if abs(got - want) > max(SIZE_TOLERANCE * want, 0.01):
            problems.append("size.{0} {1:.3f} != {2:.3f}".format(axis, got, want))
    for axis, got, want in zip("xyz", got_middle, want_middle):
        if abs(got - want) > CENTER_TOLERANCE:
            problems.append("center.{0} {1:.3f} != {2:.3f}".format(axis, got, want))
    return problems


def fixed(text, sidecar):
    text = unrotated(text)
    want_middle, want_size = expected_box(sidecar, root_offset(text))
    text = re.sub(r"m_Size: " + VECTOR, "m_Size: {{x: {0:g}, y: {1:g}, z: {2:g}}}".format(*want_size), text, count=1)
    return re.sub(r"m_Center: " + VECTOR, "m_Center: {{x: {0:g}, y: {1:g}, z: {2:g}}}".format(*[round(v, 5) for v in want_middle]), text, count=1)


def unrotated(text):
    for prop, value in (("m_LocalRotation.w", "1"), ("m_LocalRotation.x", "0"), ("m_LocalRotation.y", "0"), ("m_LocalRotation.z", "0")):
        pattern = r"(target: \{fileID: " + ROOT_TRANSFORM + r"[^\n]*\n\s+propertyPath: " + re.escape(prop) + r"\n\s+value: )(\S+)"
        text = re.sub(pattern, lambda found: found.group(1) + value, text, count=1)
    return text


def audit(root, fix=False):
    with open(os.path.join(root, "BlenderScripts", "assets.manifest.json"), encoding="utf-8") as handle:
        manifest = json.load(handle)
    report = []
    for entry in manifest["entries"]:
        stem = entry["output"][:-4]
        prefab = os.path.join(root, "Assets", "Prefabs", stem + ".prefab")
        sidecar_path = os.path.join(root, "Assets", "Models", stem + ".meta.json")
        if not os.path.exists(prefab) or not os.path.exists(sidecar_path):
            continue
        with open(sidecar_path, encoding="utf-8") as handle:
            sidecar = json.load(handle)
        with open(prefab, encoding="utf-8", newline="") as handle:
            text = handle.read()
        problems = drift(text, sidecar)
        if not problems:
            continue
        report.append("Assets/Prefabs/{0}.prefab: {1}".format(stem, ", ".join(problems)))
        if fix:
            with open(prefab, "w", encoding="utf-8", newline="") as handle:
                handle.write(fixed(text, sidecar))
    return report


if __name__ == "__main__":
    lines = audit(repo_root(), fix="--fix" in sys.argv)
    for line in lines:
        print(line)
    sys.exit(1 if lines and "--fix" not in sys.argv else 0)
