import bpy
import os
import sys
import math

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.append(script_dir)

from blender_paths import models_dir
from blender_utils import (
    reset_scene, get_or_create_material, create_box, create_cylinder, 
    create_sphere, create_cone, join_objects, set_origin_to_bottom, export_fbx, prepare_character
)

MODELS_DIR = models_dir("Characters")

def attach_readability(parts, head, hands, role):
    from character_detail import eye_color, eye_strength, face_parts, thumb_box
    label = role[:1].upper() + role[1:]
    color = eye_color(role) + (1.0,)
    eye_mat = get_or_create_material(
        "Mat_Eye_" + label,
        color,
        roughness=0.25,
        emission=eye_strength(role),
        emission_color=color,
    )
    face_mat = get_or_create_material("Mat_Face_" + label, (0.82, 0.66, 0.54, 1.0), roughness=0.7)
    thumb_mat = get_or_create_material("Mat_Thumb_" + label, (0.74, 0.58, 0.48, 1.0), roughness=0.65)
    for name, loc, size, kind in face_parts(head):
        if kind == "sphere":
            parts.append(create_sphere(name, loc, size, segments=8, rings=6, material=eye_mat))
        else:
            parts.append(create_box(name, loc, size, face_mat))
    for hand, side in hands:
        loc, size = thumb_box(hand, side)
        parts.append(create_box("Thumb" + side, loc, size, thumb_mat, bevel_radius=0.004))

def generate_player_leader():
    reset_scene()
    parts = []
    
    # Materials
    mat_skin = get_or_create_material("Mat_Skin_Survivor", (0.86, 0.70, 0.58, 1.0), roughness=0.7)
    mat_jacket = get_or_create_material("Mat_Jacket_Olive", (0.24, 0.32, 0.20, 1.0), roughness=0.8)
    mat_vest = get_or_create_material("Mat_Vest_Tactical", (0.12, 0.12, 0.14, 1.0), roughness=0.6, metallic=0.2)
    mat_pants = get_or_create_material("Mat_Pants_Camo", (0.28, 0.26, 0.22, 1.0), roughness=0.85)
    mat_boots = get_or_create_material("Mat_Boots_Leather", (0.10, 0.08, 0.06, 1.0), roughness=0.5, metallic=0.1)
    mat_pack = get_or_create_material("Mat_Backpack_Tan", (0.42, 0.35, 0.24, 1.0), roughness=0.8)
    mat_cap = get_or_create_material("Mat_Cap_Dark", (0.15, 0.16, 0.18, 1.0), roughness=0.7)
    mat_metal = get_or_create_material("Mat_Metal_Buckles", (0.7, 0.7, 0.75, 1.0), roughness=0.3, metallic=0.8)

    # 1. Torso & Tactical Vest
    torso = create_box("Torso", (0, 0, 1.15), (0.46, 0.28, 0.55), mat_jacket)
    parts.append(torso)
    
    vest_plate = create_box("VestFront", (0, 0.04, 1.18), (0.42, 0.25, 0.44), mat_vest, bevel_radius=0.012)
    parts.append(vest_plate)
    
    # Pouches on chest
    pouch1 = create_box("PouchL", (-0.12, 0.16, 1.12), (0.10, 0.08, 0.14), mat_vest)
    pouch2 = create_box("PouchR", (0.12, 0.16, 1.12), (0.10, 0.08, 0.14), mat_vest)
    pouch3 = create_box("PouchC", (0.0, 0.16, 1.25), (0.12, 0.07, 0.09), mat_vest)
    parts.extend([pouch1, pouch2, pouch3])

    # 2. Head & Cap
    neck = create_cylinder("Neck", (0, 0, 1.45), 0.10, 0.12, vertices=12, material=mat_skin)
    head = create_sphere("Head", (0, 0.02, 1.62), 0.16, segments=14, rings=10, material=mat_skin)
    parts.extend([neck, head])

    # Cap & Visor
    cap_dome = create_sphere("CapDome", (0, 0.01, 1.68), 0.17, segments=14, rings=8, material=mat_cap)
    cap_visor = create_box("CapVisor", (0, 0.16, 1.66), (0.18, 0.14, 0.025), mat_cap, rotation=(math.radians(-8), 0, 0))
    parts.extend([cap_dome, cap_visor])

    # 3. Arms & Tactical Gloves
    # Left Arm
    arm_l = create_cylinder("ArmL", (-0.31, 0.02, 1.20), 0.08, 0.48, vertices=10, material=mat_jacket, rotation=(0, math.radians(12), 0))
    hand_l = create_box("HandL", (-0.38, 0.08, 0.90), (0.09, 0.11, 0.10), mat_vest)
    parts.extend([arm_l, hand_l])

    # Right Arm (forward weapon grip stance)
    arm_r = create_cylinder("ArmR", (0.31, 0.08, 1.22), 0.08, 0.46, vertices=10, material=mat_jacket, rotation=(math.radians(25), math.radians(-10), 0))
    hand_r = create_box("HandR", (0.36, 0.28, 1.02), (0.09, 0.11, 0.10), mat_vest)
    parts.extend([arm_r, hand_r])

    # 4. Legs & Kneepads
    pelvis = create_box("Pelvis", (0, 0, 0.85), (0.38, 0.26, 0.16), mat_pants)
    leg_l = create_cylinder("LegL", (-0.15, 0, 0.52), 0.10, 0.56, vertices=12, material=mat_pants)
    leg_r = create_cylinder("LegR", (0.15, 0, 0.52), 0.10, 0.56, vertices=12, material=mat_pants)
    knee_l = create_box("KneeL", (-0.15, 0.10, 0.48), (0.11, 0.06, 0.12), mat_vest)
    knee_r = create_box("KneeR", (0.15, 0.10, 0.48), (0.11, 0.06, 0.12), mat_vest)
    parts.extend([pelvis, leg_l, leg_r, knee_l, knee_r])

    # 5. Boots
    boot_l = create_box("BootL", (-0.15, 0.03, 0.12), (0.14, 0.26, 0.24), mat_boots)
    boot_r = create_box("BootR", (0.15, 0.03, 0.12), (0.14, 0.26, 0.24), mat_boots)
    parts.extend([boot_l, boot_r])

    # 6. Backpack & Radio Antenna
    pack_body = create_box("Backpack", (0, -0.22, 1.18), (0.36, 0.22, 0.46), mat_pack)
    bedroll = create_cylinder("Bedroll", (0, -0.22, 1.44), 0.10, 0.42, vertices=12, material=mat_jacket, rotation=(0, math.radians(90), 0))
    antenna = create_cylinder("Antenna", (0.14, -0.26, 1.62), 0.012, 0.48, vertices=8, material=mat_metal)
    parts.extend([pack_body, bedroll, antenna])
    attach_readability(parts, (0, 0.02, 1.62), [((-0.38, 0.08, 0.90), "L"), ((0.36, 0.28, 1.02), "R")], "survivor")

    final_mesh = join_objects(parts, "Survivor_Leader")
    set_origin_to_bottom(final_mesh)
    prepare_character(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Survivor_Leader.fbx"), animated=True)

def generate_zombie_walker():
    reset_scene()
    parts = []

    mat_rot_flesh = get_or_create_material("Mat_Zombie_RotFlesh", (0.42, 0.48, 0.38, 1.0), roughness=0.9)
    mat_blood = get_or_create_material("Mat_Zombie_Blood", (0.45, 0.05, 0.05, 1.0), roughness=0.4)
    mat_torn_shirt = get_or_create_material("Mat_Torn_Shirt", (0.55, 0.48, 0.42, 1.0), roughness=0.95)
    mat_torn_pants = get_or_create_material("Mat_Torn_Pants", (0.18, 0.22, 0.28, 1.0), roughness=0.9)

    # Shambling slouch: torso leaned forward and tilted
    torso = create_box("ZombieTorso", (0.02, 0.06, 1.12), (0.42, 0.26, 0.52), mat_torn_shirt, rotation=(math.radians(15), math.radians(-6), math.radians(4)))
    parts.append(torso)

    # Exposed ribs / gore on chest
    ribs = create_box("ExposedGore", (0.04, 0.18, 1.16), (0.24, 0.08, 0.22), mat_blood, rotation=(math.radians(15), 0, 0))
    parts.append(ribs)

    # Head tilted sideways
    head = create_sphere("ZombieHead", (0.05, 0.16, 1.54), 0.16, segments=12, rings=10, material=mat_rot_flesh)
    parts.append(head)

    # Jaw / gaping mouth
    jaw = create_box("ZombieJaw", (0.05, 0.28, 1.45), (0.12, 0.12, 0.08), mat_blood)
    parts.append(jaw)

    # Left arm: reaching forward
    arm_l = create_cylinder("ZombieArmL", (-0.28, 0.22, 1.22), 0.07, 0.56, vertices=10, material=mat_rot_flesh, rotation=(math.radians(70), math.radians(12), 0))
    hand_l = create_box("ZombieClawL", (-0.30, 0.48, 1.35), (0.10, 0.14, 0.06), mat_rot_flesh)
    parts.extend([arm_l, hand_l])

    # Right arm: hanging broken/twisted
    arm_r = create_cylinder("ZombieArmR", (0.28, -0.02, 1.08), 0.07, 0.52, vertices=10, material=mat_rot_flesh, rotation=(math.radians(-10), math.radians(-15), math.radians(10)))
    hand_r = create_box("ZombieClawR", (0.32, -0.04, 0.78), (0.09, 0.12, 0.07), mat_rot_flesh)
    parts.extend([arm_r, hand_r])

    # Legs (one dragging slightly behind)
    leg_l = create_cylinder("ZombieLegL", (-0.14, 0.08, 0.52), 0.09, 0.58, vertices=10, material=mat_torn_pants, rotation=(math.radians(8), 0, 0))
    leg_r = create_cylinder("ZombieLegR", (0.14, -0.10, 0.50), 0.09, 0.58, vertices=10, material=mat_torn_pants, rotation=(math.radians(-14), math.radians(6), 0))
    foot_l = create_box("ZombieFootL", (-0.14, 0.14, 0.10), (0.12, 0.24, 0.16), mat_torn_pants)
    foot_r = create_box("ZombieFootR", (0.14, -0.12, 0.10), (0.12, 0.22, 0.16), mat_rot_flesh) # Bare ruined foot
    parts.extend([leg_l, leg_r, foot_l, foot_r])
    attach_readability(parts, (0.05, 0.16, 1.54), [((-0.30, 0.48, 1.35), "L"), ((0.32, -0.04, 0.78), "R")], "walker")

    final_mesh = join_objects(parts, "Zombie_Walker")
    set_origin_to_bottom(final_mesh)
    prepare_character(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Zombie_Walker.fbx"), animated=True)

def generate_zombie_runner():
    reset_scene()
    parts = []

    mat_pale_flesh = get_or_create_material("Mat_Zombie_RunnerSkin", (0.50, 0.52, 0.48, 1.0), roughness=0.6)
    mat_blood = get_or_create_material("Mat_Zombie_Blood", (0.52, 0.04, 0.04, 1.0), roughness=0.3)
    mat_hoodie = get_or_create_material("Mat_Runner_Hoodie", (0.35, 0.12, 0.12, 1.0), roughness=0.85)
    mat_trackpants = get_or_create_material("Mat_Runner_Pants", (0.12, 0.12, 0.14, 1.0), roughness=0.8)

    # Low predatory crouch (torso pitched forward 35 degrees)
    torso = create_box("RunnerTorso", (0, 0.22, 0.88), (0.36, 0.22, 0.50), mat_hoodie, rotation=(math.radians(35), 0, 0))
    parts.append(torso)

    # Snarling head thrust forward
    head = create_sphere("RunnerHead", (0, 0.48, 1.10), 0.14, segments=12, rings=10, material=mat_pale_flesh)
    jaw = create_box("RunnerJaw", (0, 0.58, 1.02), (0.10, 0.12, 0.08), mat_blood)
    parts.extend([head, jaw])

    # Arms in sprinting / pouncing spread
    arm_l = create_cylinder("RunnerArmL", (-0.26, 0.38, 0.82), 0.06, 0.56, vertices=10, material=mat_pale_flesh, rotation=(math.radians(45), math.radians(30), 0))
    claw_l = create_box("RunnerClawL", (-0.42, 0.56, 0.62), (0.08, 0.14, 0.05), mat_blood)
    arm_r = create_cylinder("RunnerArmR", (0.26, 0.12, 0.78), 0.06, 0.56, vertices=10, material=mat_pale_flesh, rotation=(math.radians(-25), math.radians(-25), 0))
    claw_r = create_box("RunnerClawR", (0.38, -0.05, 0.60), (0.08, 0.14, 0.05), mat_blood)
    parts.extend([arm_l, claw_l, arm_r, claw_r])

    # Bent spring legs
    leg_l = create_cylinder("RunnerLegL", (-0.15, 0.12, 0.44), 0.08, 0.50, vertices=10, material=mat_trackpants, rotation=(math.radians(-30), 0, 0))
    leg_r = create_cylinder("RunnerLegR", (0.15, -0.16, 0.38), 0.08, 0.52, vertices=10, material=mat_trackpants, rotation=(math.radians(35), 0, 0))
    foot_l = create_box("RunnerFootL", (-0.15, 0.28, 0.10), (0.10, 0.22, 0.14), mat_trackpants)
    foot_r = create_box("RunnerFootR", (0.15, -0.28, 0.10), (0.10, 0.22, 0.14), mat_trackpants)
    parts.extend([leg_l, leg_r, foot_l, foot_r])
    attach_readability(parts, (0, 0.48, 1.10), [((-0.42, 0.56, 0.62), "L"), ((0.38, -0.05, 0.60), "R")], "runner")

    final_mesh = join_objects(parts, "Zombie_Runner")
    set_origin_to_bottom(final_mesh)
    prepare_character(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Zombie_Runner.fbx"), animated=True)

def generate_zombie_brute():
    reset_scene()
    parts = []

    mat_mutant_flesh = get_or_create_material("Mat_Zombie_BruteFlesh", (0.36, 0.32, 0.30, 1.0), roughness=0.85)
    mat_bone_spike = get_or_create_material("Mat_Bone_Calcified", (0.82, 0.78, 0.68, 1.0), roughness=0.5, metallic=0.1)
    mat_ripped_overalls = get_or_create_material("Mat_Brute_Overalls", (0.15, 0.18, 0.22, 1.0), roughness=0.9)
    mat_veins = get_or_create_material("Mat_Mutant_Veins", (0.28, 0.08, 0.12, 1.0), roughness=0.4)

    # Massive, wide chest (2.2m tall, 1.1m wide shoulder frame)
    torso_upper = create_box("BruteChest", (0, 0, 1.55), (0.86, 0.55, 0.65), mat_mutant_flesh)
    torso_lower = create_box("BruteBelly", (0, 0.04, 1.15), (0.72, 0.48, 0.50), mat_ripped_overalls)
    parts.extend([torso_upper, torso_lower])

    # Thick sunken head between traps
    neck_collar = create_cylinder("BruteNeck", (0, 0.05, 1.82), 0.24, 0.20, vertices=12, material=mat_mutant_flesh)
    head = create_sphere("BruteHead", (0, 0.10, 1.95), 0.22, segments=12, rings=10, material=mat_mutant_flesh)
    jaw = create_box("BruteJaw", (0, 0.24, 1.82), (0.20, 0.18, 0.14), mat_veins)
    parts.extend([neck_collar, head, jaw])

    # Giant mutated right arm with bone spikes
    bicep_r = create_sphere("BruteBicepR", (0.58, 0.02, 1.55), 0.28, segments=12, rings=10, material=mat_mutant_flesh)
    forearm_r = create_cylinder("BruteForearmR", (0.64, 0.14, 1.08), 0.22, 0.62, vertices=10, material=mat_mutant_flesh, rotation=(math.radians(20), 0, 0))
    fist_r = create_box("BruteFistR", (0.66, 0.26, 0.72), (0.34, 0.36, 0.32), mat_mutant_flesh)
    # Bone club spikes
    spike1 = create_cone("BoneSpike1", (0.78, 0.04, 1.62), 0.06, 0.01, 0.32, vertices=8, material=mat_bone_spike, rotation=(0, math.radians(65), 0))
    spike2 = create_cone("BoneSpike2", (0.72, 0.20, 1.15), 0.05, 0.01, 0.28, vertices=8, material=mat_bone_spike, rotation=(math.radians(45), math.radians(45), 0))
    parts.extend([bicep_r, forearm_r, fist_r, spike1, spike2])

    # Regular left arm
    bicep_l = create_cylinder("BruteArmL", (-0.52, 0.02, 1.35), 0.16, 0.52, vertices=10, material=mat_mutant_flesh, rotation=(0, math.radians(15), 0))
    fist_l = create_box("BruteFistL", (-0.58, 0.06, 0.96), (0.22, 0.24, 0.22), mat_mutant_flesh)
    parts.extend([bicep_l, fist_l])

    # Heavy pillar legs
    leg_l = create_cylinder("BruteLegL", (-0.26, 0, 0.55), 0.18, 0.70, vertices=12, material=mat_ripped_overalls)
    leg_r = create_cylinder("BruteLegR", (0.26, 0, 0.55), 0.18, 0.70, vertices=12, material=mat_ripped_overalls)
    foot_l = create_box("BruteFootL", (-0.26, 0.08, 0.14), (0.24, 0.38, 0.24), mat_ripped_overalls)
    foot_r = create_box("BruteFootR", (0.26, 0.08, 0.14), (0.24, 0.38, 0.24), mat_ripped_overalls)
    parts.extend([leg_l, leg_r, foot_l, foot_r])
    attach_readability(parts, (0, 0.10, 1.95), [((-0.58, 0.06, 0.96), "L"), ((0.66, 0.26, 0.72), "R")], "brute")

    final_mesh = join_objects(parts, "Zombie_Brute")
    set_origin_to_bottom(final_mesh)
    prepare_character(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Zombie_Brute.fbx"), animated=True)

def generate_npc_merchant():
    reset_scene()
    parts = []

    mat_skin = get_or_create_material("Mat_Skin_Merchant", (0.76, 0.62, 0.50, 1.0), roughness=0.7)
    mat_coat = get_or_create_material("Mat_Coat_Duster", (0.32, 0.24, 0.18, 1.0), roughness=0.85)
    mat_hat = get_or_create_material("Mat_Hat_Leather", (0.16, 0.12, 0.08, 1.0), roughness=0.6)
    mat_pack = get_or_create_material("Mat_Vendor_Pack", (0.45, 0.38, 0.26, 1.0), roughness=0.8)
    mat_metal = get_or_create_material("Mat_Metal_Goods", (0.65, 0.65, 0.70, 1.0), roughness=0.35, metallic=0.7)
    mat_cloth = get_or_create_material("Mat_Cloth_Scarf", (0.55, 0.22, 0.16, 1.0), roughness=0.9)

    # Body & Long Trenchcoat
    torso = create_box("MerchantTorso", (0, 0, 1.15), (0.44, 0.30, 0.55), mat_coat)
    coat_skirt = create_cone("CoatSkirt", (0, 0, 0.60), 0.32, 0.24, 0.65, vertices=12, material=mat_coat)
    scarf = create_cylinder("Scarf", (0, 0.02, 1.44), 0.16, 0.10, vertices=12, material=mat_cloth)
    parts.extend([torso, coat_skirt, scarf])

    # Head & Wide Brim Hat
    head = create_sphere("MerchantHead", (0, 0.02, 1.58), 0.15, segments=12, rings=10, material=mat_skin)
    hat_brim = create_cylinder("HatBrim", (0, 0.02, 1.68), 0.34, 0.03, vertices=16, material=mat_hat)
    hat_crown = create_cylinder("HatCrown", (0, 0.02, 1.78), 0.18, 0.18, vertices=14, material=mat_hat)
    parts.extend([head, hat_brim, hat_crown])

    # Arms (resting comfortably on hip/straps)
    arm_l = create_cylinder("ArmL", (-0.30, 0.04, 1.15), 0.08, 0.46, vertices=10, material=mat_coat, rotation=(0, math.radians(16), 0))
    arm_r = create_cylinder("ArmR", (0.30, 0.04, 1.15), 0.08, 0.46, vertices=10, material=mat_coat, rotation=(0, math.radians(-16), 0))
    parts.extend([arm_l, arm_r])

    # Massive Vendor Traveling Pack-Frame
    frame = create_box("PackFrame", (0, -0.28, 1.25), (0.52, 0.28, 0.70), mat_pack)
    lantern = create_cylinder("HangingLantern", (0.28, -0.26, 1.45), 0.06, 0.14, vertices=8, material=mat_metal)
    pan = create_cylinder("CookingPan", (-0.28, -0.26, 1.15), 0.12, 0.04, vertices=10, material=mat_metal, rotation=(math.radians(90), 0, 0))
    bundle = create_cylinder("SleepingRoll", (0, -0.28, 1.68), 0.12, 0.58, vertices=12, material=mat_cloth, rotation=(0, math.radians(90), 0))
    parts.extend([frame, lantern, pan, bundle])

    # Boots under coat
    boot_l = create_box("BootL", (-0.14, 0.04, 0.12), (0.13, 0.25, 0.22), mat_hat)
    boot_r = create_box("BootR", (0.14, 0.04, 0.12), (0.13, 0.25, 0.22), mat_hat)
    parts.extend([boot_l, boot_r])
    attach_readability(parts, (0, 0.02, 1.58), [((-0.30, 0.04, 1.15), "L"), ((0.30, 0.04, 1.15), "R")], "merchant")

    final_mesh = join_objects(parts, "NPC_Merchant")
    set_origin_to_bottom(final_mesh)
    prepare_character(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "NPC_Merchant.fbx"), animated=True)

def generate_colonist():
    reset_scene()
    parts = []

    mat_skin = get_or_create_material("Mat_Skin_Colonist", (0.82, 0.68, 0.55, 1.0), roughness=0.7)
    mat_shirt = get_or_create_material("Mat_Shirt_Flannel", (0.58, 0.22, 0.18, 1.0), roughness=0.85)
    mat_dungarees = get_or_create_material("Mat_Dungarees_Denim", (0.18, 0.25, 0.38, 1.0), roughness=0.8)
    mat_boots = get_or_create_material("Mat_Boots_Work", (0.22, 0.16, 0.10, 1.0), roughness=0.6)
    mat_toolbelt = get_or_create_material("Mat_Toolbelt", (0.18, 0.14, 0.09, 1.0), roughness=0.5)

    torso = create_box("ColonistTorso", (0, 0, 1.15), (0.42, 0.26, 0.52), mat_dungarees)
    belt = create_box("Toolbelt", (0, 0, 0.88), (0.46, 0.30, 0.10), mat_toolbelt)
    head = create_sphere("ColonistHead", (0, 0.02, 1.58), 0.15, segments=12, rings=10, material=mat_skin)
    parts.extend([torso, belt, head])

    arm_l = create_cylinder("ArmL", (-0.28, 0, 1.15), 0.07, 0.48, vertices=10, material=mat_shirt)
    arm_r = create_cylinder("ArmR", (0.28, 0, 1.15), 0.07, 0.48, vertices=10, material=mat_shirt)
    leg_l = create_cylinder("LegL", (-0.14, 0, 0.52), 0.09, 0.58, vertices=10, material=mat_dungarees)
    leg_r = create_cylinder("LegR", (0.14, 0, 0.52), 0.09, 0.58, vertices=10, material=mat_dungarees)
    boot_l = create_box("BootL", (-0.14, 0.03, 0.12), (0.13, 0.24, 0.22), mat_boots)
    boot_r = create_box("BootR", (0.14, 0.03, 0.12), (0.13, 0.24, 0.22), mat_boots)
    parts.extend([arm_l, arm_r, leg_l, leg_r, boot_l, boot_r])
    attach_readability(parts, (0, 0.02, 1.58), [((-0.28, 0, 1.15), "L"), ((0.28, 0, 1.15), "R")], "colonist")

    final_mesh = join_objects(parts, "Colonist_Survivor")
    set_origin_to_bottom(final_mesh)
    prepare_character(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Colonist_Survivor.fbx"), animated=True)

if __name__ == "__main__":
    print("[CharacterGenerator] Generating characters...")
    generate_player_leader()
    generate_zombie_walker()
    generate_zombie_runner()
    generate_zombie_brute()
    generate_npc_merchant()
    generate_colonist()
    print("[CharacterGenerator] All characters generated successfully!")
