#!/usr/bin/env python3
"""Writes Assets/Resources/Audio/OutpostMixer.mixer from the numbers in AudioMix.cs.

Master > Music, SFX, Ambience, UI, each with a "<Bus> Duck" child that sources play into. The five
user volumes are exposed parameters on Master and the four buses; the snapshots (Normal, Paused,
Toxic, Death) set the duck groups and the master low-pass cutoff. GUIDs and file ids are hashed
from names, so the file is the same on every run. --check exits 1 when the committed file differs.
"""
import hashlib
import math
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Resources", "Audio", "OutpostMixer.mixer")

BUSES = ["Music", "SFX", "Ambience", "UI"]
EXPOSED = {"Master": "MasterVolume", "Music": "MusicVolume", "SFX": "SfxVolume", "Ambience": "AmbienceVolume", "UI": "UiVolume"}
SNAPSHOTS = ["Normal", "Paused", "Toxic", "Death"]

# Mirrors AudioMix.Duck and AudioMix.LowpassHz.
DUCK = {
    "Normal": {"Music": 1.0, "SFX": 1.0, "Ambience": 1.0, "UI": 1.0},
    "Paused": {"Music": 0.35, "SFX": 0.45, "Ambience": 0.5, "UI": 1.0},
    "Toxic": {"Music": 0.7, "SFX": 0.75, "Ambience": 1.15, "UI": 1.0},
    "Death": {"Music": 0.2, "SFX": 0.15, "Ambience": 0.25, "UI": 0.4},
}
CUTOFF = {"Normal": 22000.0, "Paused": 900.0, "Toxic": 1400.0, "Death": 480.0}


def guid(name):
    return hashlib.md5(("outpost-mixer:" + name).encode("utf-8")).hexdigest()


def file_id(name):
    return int(hashlib.md5(("outpost-mixer-id:" + name).encode("utf-8")).hexdigest()[:15], 16) + 1000000000


def decibels(level):
    if level <= 0.0001:
        return -80.0
    return max(-80.0, min(20.0, round(20.0 * math.log10(level), 4)))


def number(value):
    text = ("%.4f" % value).rstrip("0").rstrip(".")
    return "0" if text in ("-0", "") else text


def header(kind, fid, cls, hide=0):
    return [
        "--- !u!%d &%d" % (kind, fid),
        cls + ":",
        "  m_ObjectHideFlags: %d" % hide,
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
    ]


def effect(fid, name, kind, params=()):
    lines = header(244, fid, "AudioMixerEffectController", 3)
    lines += [
        "  m_Name: ",
        "  m_EffectID: " + guid("effect:" + name),
        "  m_EffectName: " + kind,
        "  m_MixLevel: " + guid("mix:" + name),
    ]
    if params:
        lines.append("  m_Parameters:")
        for param in params:
            lines += ["  - m_ParameterName: " + param, "    m_GUID: " + guid("param:" + name + ":" + param)]
    else:
        lines.append("  m_Parameters: []")
    lines += ["  m_SendTarget: {fileID: 0}", "  m_EnableWetMix: 0", "  m_Bypass: 0"]
    return lines


def group(fid, name, children, effects, color):
    lines = header(243, fid, "AudioMixerGroupController")
    lines += ["  m_Name: " + name, "  m_AudioMixer: {fileID: 24100000}", "  m_GroupID: " + guid("group:" + name)]
    if children:
        lines.append("  m_Children:")
        lines += ["  - {fileID: %d}" % child for child in children]
    else:
        lines.append("  m_Children: []")
    lines += [
        "  m_Volume: " + guid("volume:" + name),
        "  m_Pitch: " + guid("pitch:" + name),
        "  m_Send: 00000000000000000000000000000000",
        "  m_Effects:",
    ]
    lines += ["  - {fileID: %d}" % e for e in effects]
    lines += ["  m_UserColorIndex: %d" % color, "  m_Mute: 0", "  m_Solo: 0", "  m_BypassEffects: 0"]
    return lines


def snapshot_id(name):
    return 24500006 if name == SNAPSHOTS[0] else file_id("snapshot:" + name)


def build():
    lines = ["%YAML 1.1", "%TAG !u! tag:unity3d.com,2011:"]
    master_children = []
    order = ["Master"]
    for index, bus in enumerate(BUSES):
        duck = bus + " Duck"
        duck_id = file_id("group:" + duck)
        bus_id = file_id("group:" + bus)
        master_children.append(bus_id)
        order += [bus, duck]
        lines += group(bus_id, bus, [duck_id], [file_id("effect:" + bus)], index + 1)
        lines += effect(file_id("effect:" + bus), bus, "Attenuation")
        lines += group(duck_id, duck, [], [file_id("effect:" + duck)], index + 1)
        lines += effect(file_id("effect:" + duck), duck, "Attenuation")

    lines += header(241, 24100000, "AudioMixerController")
    lines += [
        "  m_Name: OutpostMixer",
        "  m_OutputGroup: {fileID: 0}",
        "  m_MasterGroup: {fileID: 24300002}",
        "  m_Snapshots:",
    ]
    lines += ["  - {fileID: %d}" % snapshot_id(s) for s in SNAPSHOTS]
    lines += [
        "  m_StartSnapshot: {fileID: 24500006}",
        "  m_SuspendThreshold: -80",
        "  m_EnableSuspend: 1",
        "  m_UpdateMode: 1",
        "  m_ExposedParameters:",
    ]
    for owner in ["Master"] + BUSES:
        lines += ["  - guid: " + guid("volume:" + owner), "    name: " + EXPOSED[owner]]
    lines += ["  m_AudioMixerGroupViews:", "  - guids:"]
    lines += ["    - " + guid("group:" + name) for name in order]
    lines += ["    name: View", "  m_CurrentViewIndex: 0", "  m_TargetSnapshot: {fileID: 24500006}"]

    lines += group(24300002, "Master", master_children, [24400004, file_id("effect:Master Lowpass")], 0)
    lines += effect(24400004, "Master", "Attenuation")
    lines += effect(file_id("effect:Master Lowpass"), "Master Lowpass", "Lowpass Simple", ["Cutoff freq"])

    cutoff = guid("param:Master Lowpass:Cutoff freq")
    for name in SNAPSHOTS:
        lines += header(245, snapshot_id(name), "AudioMixerSnapshotController")
        lines += [
            "  m_Name: " + name,
            "  m_AudioMixer: {fileID: 24100000}",
            "  m_SnapshotID: " + guid("snapshot:" + name),
            "  m_FloatValues:",
        ]
        for bus in BUSES:
            lines.append("    %s: %s" % (guid("volume:" + bus + " Duck"), number(decibels(DUCK[name][bus]))))
        lines.append("    %s: %s" % (cutoff, number(CUTOFF[name])))
        lines.append("  m_TransitionOverrides: {}")
    return "\n".join(lines) + "\n"


def main():
    text = build()
    if "--check" in sys.argv:
        current = open(OUT, encoding="utf-8").read() if os.path.exists(OUT) else ""
        if current.replace("\r\n", "\n") != text:
            print("OutpostMixer.mixer is stale: run Tools/Audio/make_mixer.py")
            return 1
        print("OutpostMixer.mixer is current")
        return 0
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)
    print("wrote " + os.path.relpath(OUT, ROOT))
    return 0


if __name__ == "__main__":
    sys.exit(main())
