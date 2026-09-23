"""Humanoid bone layout and clip library for the blockout characters.

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

SURVIVOR_CLIPS = (
    "Idle",
    "Walk",
    "Sprint",
    "CrouchIdle",
    "CrouchWalk",
    "Aim",
    "Fire",
    "Reload",
    "Melee",
    "Hit",
    "Death",
    "DeathB",
    "Interact",
    "Takedown",
)

WALKER_CLIPS = (
    "Idle",
    "IdleB",
    "Walk",
    "Shamble",
    "Sprint",
    "Attack",
    "AttackB",
    "Scream",
    "Stagger",
    "Hit",
    "Death",
    "DeathB",
    "DeathC",
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


def _gait(amount, frames=(1, 9, 17)):
    start, mid, end = frames
    return (
        ("LeftUpperLeg", start, (amount, 0.0, 0.0)),
        ("RightUpperLeg", start, (-amount, 0.0, 0.0)),
        ("LeftLowerLeg", start, (0.0, 0.0, 0.0)),
        ("RightLowerLeg", start, (amount * 0.55, 0.0, 0.0)),
        ("LeftUpperArm", start, (-amount * 0.65, 0.0, 0.0)),
        ("RightUpperArm", start, (amount * 0.65, 0.0, 0.0)),
        ("LeftUpperLeg", mid, (-amount, 0.0, 0.0)),
        ("RightUpperLeg", mid, (amount, 0.0, 0.0)),
        ("LeftLowerLeg", mid, (amount * 0.55, 0.0, 0.0)),
        ("RightLowerLeg", mid, (0.0, 0.0, 0.0)),
        ("LeftUpperArm", mid, (amount * 0.65, 0.0, 0.0)),
        ("RightUpperArm", mid, (-amount * 0.65, 0.0, 0.0)),
        ("LeftUpperLeg", end, (amount, 0.0, 0.0)),
        ("RightUpperLeg", end, (-amount, 0.0, 0.0)),
        ("LeftLowerLeg", end, (0.0, 0.0, 0.0)),
        ("RightLowerLeg", end, (amount * 0.55, 0.0, 0.0)),
        ("LeftUpperArm", end, (-amount * 0.65, 0.0, 0.0)),
        ("RightUpperArm", end, (amount * 0.65, 0.0, 0.0)),
    )


FPS = 24
GAIT_CLIPS = ("Walk", "Sprint", "CrouchWalk", "Shamble", "Charge")
LEG = 0.84
"""Hip height above the soles; a planted straight leg swings about this length."""


def _held(keys, end):
    """Retime an overlay pose so its last key lands on frame ``end``, so it loops with the gait."""
    last = max(frame for _bone, frame, _rotation in keys)
    if last <= 1:
        return keys
    return tuple((bone, 1 + int(round((frame - 1) * (end - 1) / float(last - 1))), rotation) for bone, frame, rotation in keys)


def cycle_frames(keys):
    """Frames from the first key to the last, which is the clip's loop length once exported."""
    frames = [frame for _bone, frame, _rotation in keys]
    return max(frames) - min(frames)


def ground_speed(keys, fps=FPS, leg=LEG):
    """Metres per second the planted foot sweeps back at 1x playback, or 0 for a clip with no gait.

    Each half cycle the stance leg swings from +a to -a, carrying the body 2 * leg * sin(a),
    and a cycle has two steps.
    """
    import math

    keyed = {}
    for bone, frame, rotation in keys:
        if bone == "LeftUpperLeg":
            keyed[frame] = rotation[0]
    swing = list(keyed.values())
    frames = list(keyed)
    if len(swing) < 3:
        return 0.0
    amplitude = (max(swing) - min(swing)) * 0.5
    cycle = (max(frames) - min(frames)) / float(fps)
    if amplitude <= 0.0 or cycle <= 0.0:
        return 0.0
    return 4.0 * leg * math.sin(math.radians(amplitude)) / cycle


def ground_speeds(role):
    """Ground speed per gait clip for one role, for the runtime stride table."""
    speeds = {}
    for name, keys in clips_for(role).items():
        speed = ground_speed(keys)
        if speed > 0.0 and name in GAIT_CLIPS:
            speeds[name] = round(speed, 3)
    return speeds


def _plus(*parts):
    keys = []
    for part in parts:
        keys.extend(part)
    return tuple(keys)


def _survivor_clips():
    idle = (
        ("Spine", 1, (0.0, 0.0, 0.0)),
        ("Spine", 16, (4.0, 0.0, 0.0)),
        ("Spine", 32, (0.0, 0.0, 0.0)),
        ("RightUpperArm", 1, (2.0, 0.0, 8.0)),
        ("RightUpperArm", 16, (2.0, 0.0, 14.0)),
        ("RightUpperArm", 32, (2.0, 0.0, 8.0)),
    )
    crouch_idle = (
        ("Hips", 1, (0.0, 0.0, 0.0)),
        ("Spine", 1, (28.0, 0.0, 0.0)),
        ("LeftUpperLeg", 1, (42.0, 0.0, 0.0)),
        ("RightUpperLeg", 1, (42.0, 0.0, 0.0)),
        ("LeftLowerLeg", 1, (-50.0, 0.0, 0.0)),
        ("RightLowerLeg", 1, (-50.0, 0.0, 0.0)),
        ("Spine", 16, (32.0, 0.0, 0.0)),
        ("Spine", 32, (28.0, 0.0, 0.0)),
    )
    aim = (
        ("RightUpperArm", 1, (-78.0, 0.0, 18.0)),
        ("RightLowerArm", 1, (-18.0, 0.0, 0.0)),
        ("LeftUpperArm", 1, (-40.0, 0.0, -10.0)),
        ("Spine", 1, (6.0, -8.0, 0.0)),
        ("Head", 1, (-4.0, -6.0, 0.0)),
        ("RightUpperArm", 8, (-78.0, 0.0, 18.0)),
    )
    fire = _plus(aim, (
        ("Spine", 2, (14.0, -8.0, 0.0)),
        ("RightUpperArm", 2, (-64.0, 0.0, 18.0)),
        ("Spine", 6, (6.0, -8.0, 0.0)),
    ))
    reload = (
        ("LeftUpperArm", 1, (-20.0, 0.0, -8.0)),
        ("RightUpperArm", 1, (-70.0, 10.0, 20.0)),
        ("LeftUpperArm", 8, (-90.0, 20.0, -30.0)),
        ("RightLowerArm", 8, (-40.0, 0.0, 0.0)),
        ("Spine", 8, (8.0, 6.0, 0.0)),
        ("LeftUpperArm", 16, (-20.0, 0.0, -8.0)),
        ("RightUpperArm", 16, (-70.0, 10.0, 20.0)),
    )
    melee = (
        ("RightUpperArm", 1, (-10.0, 0.0, 20.0)),
        ("Spine", 1, (0.0, 12.0, 0.0)),
        ("RightUpperArm", 6, (-110.0, -20.0, 30.0)),
        ("Spine", 6, (16.0, -24.0, 0.0)),
        ("RightUpperArm", 12, (-10.0, 0.0, 20.0)),
        ("Spine", 12, (0.0, 0.0, 0.0)),
    )
    hit = (
        ("Spine", 1, (0.0, 0.0, 0.0)),
        ("Spine", 4, (-12.0, 18.0, 8.0)),
        ("Head", 4, (-16.0, 10.0, 0.0)),
        ("Hips", 4, (0.0, 8.0, 0.0)),
        ("Spine", 12, (0.0, 0.0, 0.0)),
        ("Head", 12, (0.0, 0.0, 0.0)),
    )
    death = (
        ("Spine", 1, (8.0, 0.0, 0.0)),
        ("Hips", 1, (0.0, 0.0, 0.0)),
        ("Spine", 10, (70.0, 10.0, 0.0)),
        ("Hips", 10, (40.0, 0.0, 0.0)),
        ("LeftUpperLeg", 10, (20.0, 0.0, 0.0)),
        ("RightUpperLeg", 10, (-10.0, 0.0, 0.0)),
        ("Spine", 20, (88.0, 6.0, 0.0)),
        ("Hips", 20, (80.0, 0.0, 0.0)),
    )
    death_b = (
        ("Spine", 1, (0.0, -10.0, 0.0)),
        ("Spine", 8, (40.0, -50.0, 12.0)),
        ("Hips", 8, (20.0, -20.0, 0.0)),
        ("RightUpperArm", 8, (30.0, 0.0, 40.0)),
        ("Spine", 18, (80.0, -30.0, 0.0)),
        ("Hips", 18, (70.0, -16.0, 0.0)),
    )
    interact = (
        ("RightUpperArm", 1, (-20.0, 0.0, 10.0)),
        ("Spine", 1, (4.0, -6.0, 0.0)),
        ("RightUpperArm", 8, (-86.0, -8.0, 6.0)),
        ("Spine", 8, (18.0, -12.0, 0.0)),
        ("RightUpperArm", 16, (-20.0, 0.0, 10.0)),
        ("Spine", 16, (4.0, 0.0, 0.0)),
    )
    takedown = (
        ("Spine", 1, (10.0, 0.0, 0.0)),
        ("RightUpperArm", 1, (-30.0, 0.0, 16.0)),
        ("Spine", 6, (48.0, 0.0, 0.0)),
        ("RightUpperArm", 6, (-120.0, 0.0, 10.0)),
        ("LeftUpperLeg", 6, (30.0, 0.0, 0.0)),
        ("Spine", 14, (16.0, 0.0, 0.0)),
        ("RightUpperArm", 14, (-40.0, 0.0, 12.0)),
    )
    return {
        "Idle": idle,
        "Walk": _gait(25.0),
        "Sprint": _gait(48.0, (1, 6, 12)),
        "CrouchIdle": crouch_idle,
        "CrouchWalk": _plus(_held(crouch_idle, 17), _gait(16.0)),
        "Aim": aim,
        "Fire": fire,
        "Reload": reload,
        "Melee": melee,
        "Hit": hit,
        "Death": death,
        "DeathB": death_b,
        "Interact": interact,
        "Takedown": takedown,
    }


def _slouch(pitch):
    return (
        ("Spine", 1, (pitch, 0.0, 2.0)),
        ("Head", 1, (pitch * 0.6, 0.0, 0.0)),
        ("Spine", 12, (pitch + 6.0, 0.0, -2.0)),
        ("Head", 12, (pitch * 0.7, 0.0, 0.0)),
        ("Spine", 24, (pitch, 0.0, 2.0)),
        ("Head", 24, (pitch * 0.6, 0.0, 0.0)),
    )


def _attack(arm, reach):
    return (
        (arm, 1, (-20.0, 0.0, 16.0)),
        ("Spine", 1, (12.0, 0.0, 0.0)),
        (arm, 5, (-reach, 0.0, 8.0)),
        ("Spine", 5, (36.0, 0.0, 0.0)),
        (arm, 12, (-20.0, 0.0, 16.0)),
        ("Spine", 12, (12.0, 0.0, 0.0)),
    )


def _death_side(yaw):
    return (
        ("Spine", 1, (16.0, yaw, 0.0)),
        ("Hips", 1, (0.0, 0.0, 0.0)),
        ("Spine", 8, (60.0, yaw * 2.0, 10.0)),
        ("Hips", 8, (30.0, yaw, 0.0)),
        ("Spine", 18, (90.0, yaw, 0.0)),
        ("Hips", 18, (84.0, yaw * 0.5, 0.0)),
    )


def _walker_clips():
    slouch = _slouch(20.0)
    return {
        "Idle": slouch,
        "IdleB": _slouch(24.0),
        "Walk": _plus(_held(slouch, 17), _gait(18.0)),
        "Shamble": _plus(_slouch(28.0), _gait(14.0, (1, 12, 24))),
        "Sprint": _plus(_held(_slouch(16.0), 14), _gait(32.0, (1, 7, 14))),
        "Attack": _attack("RightUpperArm", 100.0),
        "AttackB": _attack("LeftUpperArm", 96.0),
        "Scream": (
            ("Head", 1, (8.0, 0.0, 0.0)),
            ("Head", 6, (-28.0, 0.0, 0.0)),
            ("LeftUpperArm", 6, (-70.0, 0.0, -30.0)),
            ("RightUpperArm", 6, (-70.0, 0.0, 30.0)),
            ("Spine", 6, (-10.0, 0.0, 0.0)),
            ("Head", 14, (8.0, 0.0, 0.0)),
        ),
        "Stagger": (
            ("Spine", 1, (18.0, 0.0, 0.0)),
            ("Spine", 4, (10.0, 0.0, 22.0)),
            ("Hips", 4, (0.0, 0.0, 12.0)),
            ("Spine", 12, (18.0, 0.0, 0.0)),
        ),
        "Hit": (
            ("Spine", 1, (20.0, 0.0, 0.0)),
            ("Head", 3, (-20.0, 16.0, 0.0)),
            ("Spine", 3, (4.0, 20.0, 0.0)),
            ("Spine", 10, (20.0, 0.0, 0.0)),
            ("Head", 10, (12.0, 0.0, 0.0)),
        ),
        "Death": _death_side(0.0),
        "DeathB": _death_side(18.0),
        "DeathC": _death_side(-22.0),
    }


def _runner_clips():
    clips = _walker_clips()
    clips["Idle"] = _slouch(8.0)
    clips["Lunge"] = (
        ("Spine", 1, (12.0, 0.0, 0.0)),
        ("LeftUpperLeg", 1, (10.0, 0.0, 0.0)),
        ("RightUpperLeg", 1, (-20.0, 0.0, 0.0)),
        ("Spine", 4, (58.0, 0.0, 0.0)),
        ("LeftUpperLeg", 4, (70.0, 0.0, 0.0)),
        ("RightUpperLeg", 4, (-30.0, 0.0, 0.0)),
        ("LeftUpperArm", 4, (-40.0, 0.0, -20.0)),
        ("RightUpperArm", 4, (-40.0, 0.0, 20.0)),
        ("Spine", 10, (18.0, 0.0, 0.0)),
        ("LeftUpperLeg", 10, (16.0, 0.0, 0.0)),
    )
    return clips


def _brute_clips():
    clips = _walker_clips()
    clips["Idle"] = _slouch(10.0)
    clips["Charge"] = _plus(_held(_slouch(22.0), 10), _gait(40.0, (1, 5, 10)))
    clips["Roar"] = (
        ("Head", 1, (6.0, 0.0, 0.0)),
        ("Spine", 1, (8.0, 0.0, 0.0)),
        ("Head", 8, (-36.0, 0.0, 0.0)),
        ("Spine", 8, (-16.0, 0.0, 0.0)),
        ("LeftUpperArm", 8, (-100.0, 0.0, -40.0)),
        ("RightUpperArm", 8, (-100.0, 0.0, 40.0)),
        ("Head", 18, (6.0, 0.0, 0.0)),
        ("LeftUpperArm", 18, (-20.0, 0.0, -10.0)),
        ("RightUpperArm", 18, (-20.0, 0.0, 10.0)),
    )
    return clips


def _npc_clips():
    clips = _survivor_clips()
    clips["Work"] = (
        ("RightUpperArm", 1, (-30.0, 0.0, 20.0)),
        ("Spine", 1, (12.0, 0.0, 0.0)),
        ("RightUpperArm", 6, (-100.0, 0.0, 8.0)),
        ("Spine", 6, (24.0, 0.0, 0.0)),
        ("RightUpperArm", 12, (-30.0, 0.0, 20.0)),
        ("Spine", 12, (12.0, 0.0, 0.0)),
    )
    clips["Talk"] = (
        ("Spine", 1, (4.0, -8.0, 0.0)),
        ("Head", 1, (0.0, -10.0, 0.0)),
        ("RightUpperArm", 1, (-24.0, 0.0, 18.0)),
        ("Spine", 10, (4.0, 8.0, 0.0)),
        ("Head", 10, (0.0, 10.0, 0.0)),
        ("RightUpperArm", 10, (-36.0, 0.0, 10.0)),
        ("Spine", 20, (4.0, -8.0, 0.0)),
        ("Head", 20, (0.0, -10.0, 0.0)),
    )
    return clips


def locomotion_clips():
    """Survivor take list. Other roles come from clips_for."""
    return _survivor_clips()


def clips_for(role):
    """Pick the take list from a mesh or file name."""
    name = (role or "").lower()
    if "walker" in name:
        return _walker_clips()
    if "runner" in name:
        return _runner_clips()
    if "brute" in name:
        return _brute_clips()
    if "merchant" in name:
        return _npc_clips()
    return _survivor_clips()


def bone_names():
    return tuple(bone[0] for bone in humanoid_bones())
