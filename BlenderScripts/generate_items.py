"""Pickups for every pack item that had no world model of its own (PRO-50)."""

import math
import os
import sys

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.append(script_dir)

from blender_utils import (
    get_or_create_material, create_box, create_cylinder,
    create_sphere, create_cone, join_objects
)


ITEM_IDS = (
    "Loot_Bandage", "Loot_Antibiotics", "Loot_Painkillers", "Loot_CannedFood", "Loot_RawFood",
    "Loot_WaterBottle", "Loot_AmmoBox_Rifle", "Loot_AmmoBox_SMG", "Loot_Cloth", "Loot_Chemicals",
    "Loot_DuctTape", "Loot_NoiseLure", "Loot_Bottle", "Loot_Molotov", "Loot_Flare",
    "Loot_PipeBomb", "Loot_Blueprint", "Loot_LampCell", "Loot_GeneratorPart",
)

UPRIGHT = (0, 0, 0)
LYING = (0, math.radians(90), 0)


def _mat(name, rgb, metallic=0.0, roughness=0.6, emission=0.0):
    return get_or_create_material(name, (rgb[0], rgb[1], rgb[2], 1.0), metallic=metallic, roughness=roughness, emission=emission)


def build_bandage(ctx):
    gauze = _mat("Mat_Item_Gauze", (0.86, 0.84, 0.78), roughness=0.9)
    clip = _mat("Mat_Item_Clip", (0.70, 0.70, 0.72), metallic=0.8, roughness=0.3)
    roll = create_cylinder("BandageRoll", (0, 0, 0.035), 0.035, 0.07, vertices=16, material=gauze, rotation=LYING)
    tail = create_box("BandageTail", (0, -0.05, 0.004), (0.06, 0.07, 0.006), gauze)
    pin = create_box("BandageClip", (0.036, 0, 0.05), (0.004, 0.02, 0.012), clip)
    return join_objects([roll, tail, pin], "Loot_Bandage")


def build_antibiotics(ctx):
    amber = _mat("Mat_Item_PillAmber", (0.78, 0.40, 0.08), roughness=0.25)
    cap = _mat("Mat_Item_PillCap", (0.92, 0.92, 0.90), roughness=0.5)
    label = _mat("Mat_Item_PillLabel", (0.95, 0.95, 0.92), roughness=0.8)
    body = create_cylinder("PillBottle", (0, 0, 0.045), 0.025, 0.09, vertices=16, material=amber)
    band = create_cylinder("PillLabel", (0, 0, 0.042), 0.0255, 0.045, vertices=16, material=label)
    lid = create_cylinder("PillCap", (0, 0, 0.098), 0.027, 0.018, vertices=16, material=cap)
    return join_objects([body, band, lid], "Loot_Antibiotics")


def build_painkillers(ctx):
    card = _mat("Mat_Item_BlisterCard", (0.90, 0.90, 0.92), roughness=0.5)
    stripe = _mat("Mat_Item_BlisterStripe", (0.16, 0.36, 0.72), roughness=0.5)
    foil = _mat("Mat_Item_BlisterFoil", (0.78, 0.80, 0.82), metallic=0.9, roughness=0.25)
    box = create_box("PainkillerBox", (0, 0, 0.02), (0.1, 0.05, 0.04), card)
    band = create_box("PainkillerStripe", (0, 0.0255, 0.02), (0.1, 0.002, 0.012), stripe)
    sheet = create_box("PainkillerSheet", (0.02, 0.0, 0.043), (0.08, 0.045, 0.004), foil, rotation=(0, 0, math.radians(12)))
    return join_objects([box, band, sheet], "Loot_Painkillers")


def build_canned_food(ctx):
    tin = _mat("Mat_Item_Tin", (0.72, 0.73, 0.75), metallic=0.9, roughness=0.3)
    label = _mat("Mat_Item_TinLabel", (0.72, 0.16, 0.10), roughness=0.7)
    body = create_cylinder("Can", (0, 0, 0.055), 0.038, 0.11, vertices=18, material=tin)
    band = create_cylinder("CanLabel", (0, 0, 0.055), 0.0385, 0.075, vertices=18, material=label)
    lid = create_cylinder("CanRim", (0, 0, 0.111), 0.036, 0.004, vertices=18, material=tin)
    return join_objects([body, band, lid], "Loot_CannedFood")


def build_raw_food(ctx):
    paper = _mat("Mat_Item_ButcherPaper", (0.78, 0.68, 0.52), roughness=0.95)
    meat = _mat("Mat_Item_Meat", (0.62, 0.18, 0.16), roughness=0.6)
    string = _mat("Mat_Item_Twine", (0.55, 0.45, 0.30), roughness=0.9)
    wrap = create_box("MeatWrap", (0, 0, 0.025), (0.16, 0.11, 0.05), paper, bevel_radius=0.01)
    cut = create_box("MeatCut", (0.03, 0.0, 0.055), (0.08, 0.07, 0.02), meat, bevel_radius=0.006, rotation=(0, 0, math.radians(-10)))
    tie = create_box("MeatTwine", (-0.03, 0, 0.026), (0.006, 0.112, 0.052), string)
    return join_objects([wrap, cut, tie], "Loot_RawFood")


def build_water_bottle(ctx):
    plastic = _mat("Mat_Item_BottlePlastic", (0.40, 0.66, 0.82), roughness=0.15)
    cap = _mat("Mat_Item_BottleCap", (0.10, 0.30, 0.70), roughness=0.5)
    label = _mat("Mat_Item_BottleLabel", (0.92, 0.94, 0.96), roughness=0.7)
    body = create_cylinder("Bottle", (0, 0, 0.09), 0.033, 0.18, vertices=16, material=plastic)
    band = create_cylinder("BottleLabel", (0, 0, 0.09), 0.0335, 0.06, vertices=16, material=label)
    neck = create_cone("BottleNeck", (0, 0, 0.2), 0.033, 0.014, 0.04, vertices=16, material=plastic)
    lid = create_cylinder("BottleCap", (0, 0, 0.228), 0.015, 0.018, vertices=12, material=cap)
    return join_objects([body, band, neck, lid], "Loot_WaterBottle")


def build_ammo_box_rifle(ctx):
    steel = _mat("Mat_Item_RifleCan", (0.26, 0.30, 0.18), metallic=0.5, roughness=0.55)
    latch = _mat("Mat_Item_RifleLatch", (0.20, 0.20, 0.20), metallic=0.8, roughness=0.4)
    stencil = _mat("Mat_Item_RifleStencil", (0.85, 0.78, 0.35), roughness=0.6)
    can = create_box("RifleCan", (0, 0, 0.07), (0.26, 0.1, 0.14), steel, bevel_radius=0.006)
    lid = create_box("RifleLid", (0, 0, 0.145), (0.27, 0.105, 0.012), latch)
    handle = create_box("RifleHandle", (0, 0, 0.16), (0.1, 0.02, 0.012), latch)
    mark = create_box("RifleStencil", (0, 0.051, 0.07), (0.12, 0.002, 0.03), stencil)
    return join_objects([can, lid, handle, mark], "Loot_AmmoBox_Rifle")


def build_ammo_box_smg(ctx):
    card = _mat("Mat_Item_SmgBox", (0.22, 0.22, 0.24), roughness=0.6)
    stripe = _mat("Mat_Item_SmgStripe", (0.92, 0.74, 0.12), roughness=0.5)
    brass = _mat("Mat_Brass_Bullet", (0.85, 0.72, 0.28), metallic=0.9, roughness=0.25)
    box = create_box("SmgBox", (0, 0, 0.04), (0.14, 0.09, 0.08), card, bevel_radius=0.004)
    band = create_box("SmgStripe", (0, 0.0455, 0.05), (0.14, 0.002, 0.018), stripe)
    rounds = [create_cylinder("SmgRound{0}".format(i), (-0.045 + i * 0.03, 0.0, 0.092), 0.006, 0.024, vertices=8, material=brass) for i in range(4)]
    return join_objects([box, band] + rounds, "Loot_AmmoBox_SMG")


def build_cloth(ctx):
    colours = ((0.36, 0.30, 0.44), (0.62, 0.56, 0.44), (0.30, 0.42, 0.34))
    parts = []
    for i, rgb in enumerate(colours):
        mat = _mat("Mat_Item_Cloth{0}".format(i), rgb, roughness=0.95)
        parts.append(create_box("ClothFold{0}".format(i), (0.004 * i, -0.003 * i, 0.012 + i * 0.022), (0.18, 0.13, 0.022), mat, bevel_radius=0.008, rotation=(0, 0, math.radians(6 * i - 6))))
    return join_objects(parts, "Loot_Cloth")


def build_chemicals(ctx):
    jug = _mat("Mat_Item_ChemJug", (0.24, 0.52, 0.28), roughness=0.4)
    cap = _mat("Mat_Item_ChemCap", (0.86, 0.72, 0.10), roughness=0.5)
    hazard = _mat("Mat_Item_ChemHazard", (0.95, 0.60, 0.10), roughness=0.6)
    body = create_box("ChemJug", (0, 0, 0.09), (0.12, 0.08, 0.18), jug, bevel_radius=0.012)
    grip = create_box("ChemHandle", (-0.03, 0, 0.19), (0.05, 0.02, 0.03), jug)
    spout = create_cylinder("ChemCap", (0.035, 0, 0.19), 0.016, 0.025, vertices=12, material=cap)
    sign = create_box("ChemHazard", (0, 0.041, 0.09), (0.05, 0.002, 0.05), hazard, rotation=(0, math.radians(45), 0))
    return join_objects([body, grip, spout, sign], "Loot_Chemicals")


def build_duct_tape(ctx):
    tape = _mat("Mat_Item_Tape", (0.58, 0.60, 0.62), metallic=0.3, roughness=0.5)
    core = _mat("Mat_Item_TapeCore", (0.50, 0.38, 0.22), roughness=0.9)
    hole = _mat("Mat_Item_TapeHole", (0.05, 0.05, 0.05), roughness=1.0)
    roll = create_cylinder("TapeRoll", (0, 0, 0.025), 0.05, 0.05, vertices=20, material=tape)
    ring = create_cylinder("TapeCore", (0, 0, 0.026), 0.03, 0.052, vertices=20, material=core)
    gap = create_cylinder("TapeHole", (0, 0, 0.027), 0.024, 0.054, vertices=16, material=hole)
    strip = create_box("TapeStrip", (0.06, -0.02, 0.003), (0.05, 0.048, 0.004), tape, rotation=(0, 0, math.radians(20)))
    return join_objects([roll, ring, gap, strip], "Loot_DuctTape")


def build_noise_lure(ctx):
    case = _mat("Mat_Item_ClockCase", (0.72, 0.14, 0.12), metallic=0.4, roughness=0.4)
    face = _mat("Mat_Item_ClockFace", (0.94, 0.92, 0.86), roughness=0.6)
    bell = _mat("Mat_Item_ClockBell", (0.82, 0.70, 0.30), metallic=0.9, roughness=0.25)
    body = create_cylinder("ClockBody", (0, 0, 0.06), 0.045, 0.035, vertices=18, material=case, rotation=(math.radians(90), 0, 0))
    dial = create_cylinder("ClockFace", (0, 0.018, 0.06), 0.038, 0.004, vertices=18, material=face, rotation=(math.radians(90), 0, 0))
    left = create_sphere("BellL", (-0.03, 0, 0.108), 0.018, segments=10, rings=6, material=bell)
    right = create_sphere("BellR", (0.03, 0, 0.108), 0.018, segments=10, rings=6, material=bell)
    leg_l = create_box("LegL", (-0.03, 0, 0.01), (0.008, 0.008, 0.02), case)
    leg_r = create_box("LegR", (0.03, 0, 0.01), (0.008, 0.008, 0.02), case)
    return join_objects([body, dial, left, right, leg_l, leg_r], "Loot_NoiseLure")


def _bottle(prefix, glass):
    body = create_cylinder(prefix + "Body", (0, 0, 0.08), 0.034, 0.16, vertices=16, material=glass)
    shoulder = create_cone(prefix + "Shoulder", (0, 0, 0.18), 0.034, 0.014, 0.04, vertices=16, material=glass)
    neck = create_cylinder(prefix + "Neck", (0, 0, 0.225), 0.013, 0.05, vertices=12, material=glass)
    return [body, shoulder, neck]


def build_bottle(ctx):
    glass = _mat("Mat_Item_BrownGlass", (0.36, 0.20, 0.06), roughness=0.1)
    label = _mat("Mat_Item_BeerLabel", (0.86, 0.78, 0.56), roughness=0.7)
    parts = _bottle("Bottle", glass)
    parts.append(create_cylinder("BottleLabel", (0, 0, 0.08), 0.0345, 0.05, vertices=16, material=label))
    return join_objects(parts, "Loot_Bottle")


def build_molotov(ctx):
    glass = _mat("Mat_Item_MolotovGlass", (0.24, 0.40, 0.18), roughness=0.1)
    fuel = _mat("Mat_Item_MolotovFuel", (0.80, 0.46, 0.10), roughness=0.2)
    rag = _mat("Mat_Item_MolotovRag", (0.74, 0.70, 0.60), roughness=0.95)
    parts = _bottle("Molotov", glass)
    parts.append(create_cylinder("MolotovFuel", (0, 0, 0.055), 0.0345, 0.1, vertices=16, material=fuel))
    parts.append(create_box("MolotovRag", (0.01, 0, 0.27), (0.03, 0.02, 0.06), rag, rotation=(0, math.radians(18), 0)))
    return join_objects(parts, "Loot_Molotov")


def build_flare(ctx):
    tube = _mat("Mat_Item_FlareTube", (0.82, 0.12, 0.10), roughness=0.5)
    cap = _mat("Mat_Item_FlareCap", (0.12, 0.12, 0.12), roughness=0.6)
    tip = _mat("Mat_Item_FlareTip", (1.0, 0.55, 0.30), roughness=0.4, emission=2.0)
    stick = create_cylinder("FlareStick", (0, 0, 0.016), 0.016, 0.2, vertices=14, material=tube, rotation=LYING)
    end = create_cylinder("FlareCap", (-0.105, 0, 0.016), 0.018, 0.03, vertices=14, material=cap, rotation=LYING)
    head = create_cylinder("FlareTip", (0.105, 0, 0.016), 0.012, 0.012, vertices=12, material=tip, rotation=LYING)
    return join_objects([stick, end, head], "Loot_Flare")


def build_pipe_bomb(ctx):
    pipe = _mat("Mat_Item_BombPipe", (0.40, 0.40, 0.42), metallic=0.85, roughness=0.45)
    cap = _mat("Mat_Item_BombCap", (0.30, 0.30, 0.32), metallic=0.85, roughness=0.5)
    wire = _mat("Mat_Item_BombWire", (0.80, 0.12, 0.10), roughness=0.5)
    tape = _mat("Mat_Item_BombTape", (0.10, 0.10, 0.10), roughness=0.8)
    body = create_cylinder("BombPipe", (0, 0, 0.028), 0.026, 0.18, vertices=14, material=pipe, rotation=LYING)
    left = create_cylinder("BombCapL", (-0.095, 0, 0.028), 0.03, 0.022, vertices=6, material=cap, rotation=LYING)
    right = create_cylinder("BombCapR", (0.095, 0, 0.028), 0.03, 0.022, vertices=6, material=cap, rotation=LYING)
    wrap = create_cylinder("BombTape", (0.02, 0, 0.028), 0.027, 0.04, vertices=14, material=tape, rotation=LYING)
    fuse = create_box("BombWire", (0.04, 0, 0.062), (0.1, 0.006, 0.006), wire, rotation=(0, math.radians(-12), 0))
    return join_objects([body, left, right, wrap, fuse], "Loot_PipeBomb")


def build_blueprint(ctx):
    paper = _mat("Mat_Item_Blueprint", (0.14, 0.30, 0.62), roughness=0.9)
    ink = _mat("Mat_Item_BlueprintInk", (0.86, 0.90, 0.96), roughness=0.9)
    band = _mat("Mat_Item_BlueprintBand", (0.70, 0.18, 0.12), roughness=0.7)
    sheet = create_box("BlueprintSheet", (0, 0, 0.002), (0.2, 0.14, 0.004), paper)
    lines = create_box("BlueprintLines", (0, 0, 0.0045), (0.14, 0.09, 0.001), ink)
    roll = create_cylinder("BlueprintRoll", (0, 0.06, 0.02), 0.018, 0.22, vertices=14, material=paper, rotation=LYING)
    tie = create_cylinder("BlueprintBand", (0, 0.06, 0.02), 0.0185, 0.015, vertices=14, material=band, rotation=LYING)
    return join_objects([sheet, lines, roll, tie], "Loot_Blueprint")


def build_lamp_cell(ctx):
    shell = _mat("Mat_Item_CellShell", (0.14, 0.14, 0.16), metallic=0.4, roughness=0.5)
    band = _mat("Mat_Item_CellBand", (0.30, 0.78, 0.36), roughness=0.5, emission=0.6)
    contact = _mat("Mat_Item_CellContact", (0.80, 0.52, 0.28), metallic=0.95, roughness=0.2)
    body = create_cylinder("CellBody", (0, 0, 0.04), 0.022, 0.08, vertices=16, material=shell)
    ring = create_cylinder("CellBand", (0, 0, 0.055), 0.0225, 0.014, vertices=16, material=band)
    nub = create_cylinder("CellContact", (0, 0, 0.084), 0.008, 0.008, vertices=10, material=contact)
    return join_objects([body, ring, nub], "Loot_LampCell")


def build_generator_part(ctx):
    casing = _mat("Mat_Item_AlternatorSteel", (0.52, 0.54, 0.56), metallic=0.8, roughness=0.4)
    rust = _mat("Mat_Item_AlternatorRust", (0.46, 0.24, 0.12), metallic=0.5, roughness=0.8)
    copper = _mat("Mat_Item_AlternatorCopper", (0.78, 0.44, 0.22), metallic=0.95, roughness=0.3)
    strap = _mat("Mat_Item_AlternatorStrap", (0.12, 0.12, 0.12), roughness=0.8)
    parts = [create_cylinder("AlternatorBody", (0, 0, 0.07), 0.07, 0.14, vertices=16, material=casing, rotation=LYING)]
    for i in range(5):
        x = -0.05 + i * 0.025
        parts.append(create_cylinder(f"AlternatorFin_{i}", (x, 0, 0.07), 0.076, 0.008, vertices=16, material=rust, rotation=LYING))
    parts.append(create_cylinder("AlternatorShaft", (0.09, 0, 0.07), 0.012, 0.05, vertices=10, material=casing, rotation=LYING))
    parts.append(create_cylinder("AlternatorPulley", (0.11, 0, 0.07), 0.035, 0.018, vertices=16, material=strap, rotation=LYING))
    for y in (-0.02, 0.02):
        parts.append(create_cylinder(f"AlternatorTerminal_{y}", (-0.03, y, 0.145), 0.008, 0.02, vertices=8, material=copper))
    parts.append(create_box("AlternatorBracket", (-0.02, 0.075, 0.07), (0.06, 0.012, 0.05), rust))
    parts.append(create_box("AlternatorTag", (0.0, -0.071, 0.07), (0.05, 0.004, 0.03), copper))
    return join_objects(parts, "Loot_GeneratorPart")


if __name__ == "__main__":
    import pipeline
    sys.exit(pipeline.main(["--only", ",".join(ITEM_IDS)]))

