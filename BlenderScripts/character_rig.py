"""Humanoid bone layout and locomotion keys for the blockout characters.

This module does not import bpy. Blender applies it when the character scripts run.
Bone positions are in meters above the feet, after the mesh origin sits on the ground.
"""

REQUIRED_BONES = (
    "Hips",
    "Spine",
    "Head",
    "LeftUpperArm",
    "LeftLowerArm",
    "LeftHand",
    "RightUpperArm",
    "RightLowerArm",
    "RightHand",
    "LeftUpperLeg",
    "LeftLowerLeg",
    "LeftFoot",
    "RightUpperLeg",
    "RightLowerLeg",
    "RightFoot",
)


def humanoid_bones():
    """Return (name, parent or None, head xyz, tail xyz) in parent-before-child order."""
    return (
        ("Hips", None, (0.0, 0.0, 0.86), (0.0, 0.0, 1.02)),
        ("Spine", "Hips", (0.0, 0.0, 1.02), (0.0, 0.0, 1.38)),
        ("Head", "Spine", (0.0, 0.02, 1.52), (0.0, 0.02, 1.74)),
        ("LeftUpperArm", "Spine", (-0.22, 0.0, 1.36), (-0.48, 0.0, 1.32)),
        ("LeftLowerArm", "LeftUpperArm", (-0.48, 0.0, 1.32), (-0.70, 0.04, 1.22)),
        ("LeftHand", "LeftLowerArm", (-0.70, 0.04, 1.22), (-0.80, 0.06, 1.16)),
        ("RightUpperArm", "Spine", (0.22, 0.0, 1.36), (0.48, 0.0, 1.32)),
        ("RightLowerArm", "RightUpperArm", (0.48, 0.0, 1.32), (0.70, 0.04, 1.22)),
        ("RightHand", "RightLowerArm", (0.70, 0.04, 1.22), (0.80, 0.06, 1.16)),
        ("LeftUpperLeg", "Hips", (-0.12, 0.0, 0.84), (-0.13, 0.0, 0.46)),
        ("LeftLowerLeg", "LeftUpperLeg", (-0.13, 0.0, 0.46), (-0.13, 0.02, 0.10)),
        ("LeftFoot", "LeftLowerLeg", (-0.13, 0.02, 0.10), (-0.13, 0.16, 0.04)),
        ("RightUpperLeg", "Hips", (0.12, 0.0, 0.84), (0.13, 0.0, 0.46)),
        ("RightLowerLeg", "RightUpperLeg", (0.13, 0.0, 0.46), (0.13, 0.02, 0.10)),
        ("RightFoot", "RightLowerLeg", (0.13, 0.02, 0.10), (0.13, 0.16, 0.04)),
    )


def locomotion_clips():
    """Clip name to a list of (bone, frame, xyz euler degrees)."""
    idle = (
        ("Spine", 1, (0.0, 0.0, 0.0)),
        ("Spine", 16, (4.0, 0.0, 0.0)),
        ("Spine", 32, (0.0, 0.0, 0.0)),
    )
    walk = (
        ("LeftUpperLeg", 1, (25.0, 0.0, 0.0)),
        ("RightUpperLeg", 1, (-25.0, 0.0, 0.0)),
        ("LeftUpperArm", 1, (-20.0, 0.0, 0.0)),
        ("RightUpperArm", 1, (20.0, 0.0, 0.0)),
        ("LeftUpperLeg", 9, (-25.0, 0.0, 0.0)),
        ("RightUpperLeg", 9, (25.0, 0.0, 0.0)),
        ("LeftUpperArm", 9, (20.0, 0.0, 0.0)),
        ("RightUpperArm", 9, (-20.0, 0.0, 0.0)),
        ("LeftUpperLeg", 17, (25.0, 0.0, 0.0)),
        ("RightUpperLeg", 17, (-25.0, 0.0, 0.0)),
        ("LeftUpperArm", 17, (-20.0, 0.0, 0.0)),
        ("RightUpperArm", 17, (20.0, 0.0, 0.0)),
    )
    return {"Idle": idle, "Walk": walk}


def bone_names():
    return tuple(bone[0] for bone in humanoid_bones())
