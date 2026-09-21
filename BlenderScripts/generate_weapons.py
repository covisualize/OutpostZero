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
    create_sphere, create_cone, join_objects, set_origin_to_bottom, export_fbx
)

MODELS_DIR = models_dir("Weapons")

def generate_pistol():
    reset_scene()
    parts = []

    mat_metal_dark = get_or_create_material("Mat_Gun_Steel", (0.16, 0.17, 0.19, 1.0), metallic=0.9, roughness=0.3)
    mat_metal_silver = get_or_create_material("Mat_Gun_Silver", (0.75, 0.77, 0.80, 1.0), metallic=0.95, roughness=0.2)
    mat_grip_polymer = get_or_create_material("Mat_Gun_Polymer", (0.08, 0.08, 0.09, 1.0), metallic=0.1, roughness=0.7)

    # Scale: ~0.22m long pistol
    # 1. Slide
    slide = create_box("PistolSlide", (0, 0.04, 0.06), (0.038, 0.22, 0.042), mat_metal_dark, bevel_radius=0.003)
    ejection_port = create_box("EjectionPort", (0.016, 0.02, 0.07), (0.012, 0.048, 0.018), mat_metal_silver)
    parts.extend([slide, ejection_port])

    # Sights
    front_sight = create_box("FrontSight", (0, 0.13, 0.084), (0.008, 0.014, 0.012), mat_metal_silver)
    rear_sight = create_box("RearSight", (0, -0.055, 0.084), (0.018, 0.014, 0.012), mat_metal_dark)
    barrel_tip = create_cylinder("BarrelTip", (0, 0.145, 0.06), 0.012, 0.025, vertices=12, material=mat_metal_silver, rotation=(math.radians(90), 0, 0))
    parts.extend([front_sight, rear_sight, barrel_tip])

    # 2. Lower Frame & Grip
    frame = create_box("PistolFrame", (0, 0.03, 0.035), (0.034, 0.20, 0.024), mat_grip_polymer)
    rail = create_box("TacticalRail", (0, 0.09, 0.018), (0.028, 0.07, 0.012), mat_metal_dark)
    grip = create_box("PistolGrip", (0, -0.045, -0.045), (0.032, 0.065, 0.14), mat_grip_polymer, rotation=(math.radians(16), 0, 0))
    trigger_guard = create_box("TriggerGuard", (0, 0.015, -0.015), (0.016, 0.055, 0.042), mat_grip_polymer)
    trigger = create_box("Trigger", (0, 0.012, -0.012), (0.008, 0.012, 0.024), mat_metal_silver, rotation=(math.radians(-15), 0, 0))
    mag_base = create_box("MagBase", (0, -0.065, -0.115), (0.036, 0.068, 0.014), mat_metal_dark)
    parts.extend([frame, rail, grip, trigger_guard, trigger, mag_base])

    final_mesh = join_objects(parts, "Weapon_Pistol_9mm")
    export_fbx(os.path.join(MODELS_DIR, "Weapon_Pistol_9mm.fbx"))

def generate_shotgun():
    reset_scene()
    parts = []

    mat_metal = get_or_create_material("Mat_Shotgun_Metal", (0.15, 0.15, 0.17, 1.0), metallic=0.9, roughness=0.35)
    mat_wood_poly = get_or_create_material("Mat_Shotgun_Stock", (0.34, 0.20, 0.12, 1.0), metallic=0.0, roughness=0.6)
    mat_rubber = get_or_create_material("Mat_Shotgun_RecoilPad", (0.06, 0.06, 0.06, 1.0), metallic=0.0, roughness=0.9)

    # Scale: ~0.95m total length
    # 1. Receiver
    receiver = create_box("Receiver", (0, 0, 0.04), (0.045, 0.24, 0.075), mat_metal, bevel_radius=0.004)
    parts.append(receiver)

    # 2. Barrel & Magazine Tube
    barrel = create_cylinder("Barrel", (0, 0.38, 0.065), 0.016, 0.58, vertices=14, material=mat_metal, rotation=(math.radians(90), 0, 0))
    mag_tube = create_cylinder("MagTube", (0, 0.34, 0.032), 0.015, 0.50, vertices=14, material=mat_metal, rotation=(math.radians(90), 0, 0))
    barrel_clamp = create_box("BarrelClamp", (0, 0.54, 0.048), (0.038, 0.024, 0.052), mat_metal)
    bead_sight = create_sphere("BeadSight", (0, 0.65, 0.082), 0.006, segments=8, rings=6, material=mat_metal)
    parts.extend([barrel, mag_tube, barrel_clamp, bead_sight])

    # 3. Pump Slide / Fore-end
    pump = create_cylinder("PumpSlide", (0, 0.26, 0.032), 0.024, 0.18, vertices=12, material=mat_wood_poly, rotation=(math.radians(90), 0, 0))
    parts.append(pump)

    # 4. Stock & Grip
    grip_neck = create_box("GripNeck", (0, -0.16, 0.01), (0.038, 0.12, 0.06), mat_wood_poly, rotation=(math.radians(18), 0, 0))
    stock_body = create_box("StockBody", (0, -0.32, -0.01), (0.042, 0.26, 0.12), mat_wood_poly, rotation=(math.radians(8), 0, 0))
    recoil_pad = create_box("RecoilPad", (0, -0.45, -0.02), (0.045, 0.028, 0.13), mat_rubber)
    trigger_guard = create_box("TriggerGuard", (0, -0.04, -0.018), (0.018, 0.06, 0.045), mat_metal)
    parts.extend([grip_neck, stock_body, recoil_pad, trigger_guard])

    final_mesh = join_objects(parts, "Weapon_Shotgun_Pump")
    export_fbx(os.path.join(MODELS_DIR, "Weapon_Shotgun_Pump.fbx"))

def generate_machete():
    reset_scene()
    parts = []

    mat_blade = get_or_create_material("Mat_Blade_Steel", (0.80, 0.82, 0.86, 1.0), metallic=0.98, roughness=0.18)
    mat_handle = get_or_create_material("Mat_Handle_CordWrap", (0.16, 0.12, 0.09, 1.0), metallic=0.0, roughness=0.85)
    mat_guard = get_or_create_material("Mat_Machete_Guard", (0.22, 0.22, 0.24, 1.0), metallic=0.85, roughness=0.4)

    # Scale: ~0.55m total length
    # 1. Blade (angled bolo tip)
    blade_main = create_box("BladeMain", (0, 0.18, 0.02), (0.008, 0.32, 0.065), mat_blade)
    blade_tip = create_box("BladeTip", (0, 0.36, 0.03), (0.007, 0.12, 0.085), mat_blade, rotation=(math.radians(12), 0, 0))
    parts.extend([blade_main, blade_tip])

    # 2. Guard
    crossguard = create_box("Crossguard", (0, 0.015, 0.02), (0.026, 0.024, 0.09), mat_guard)
    parts.append(crossguard)

    # 3. Handle & Pommel
    handle = create_cylinder("Handle", (0, -0.08, 0.018), 0.018, 0.16, vertices=12, material=mat_handle, rotation=(math.radians(90), 0, 0))
    pommel = create_sphere("PommelRing", (0, -0.165, 0.018), 0.024, segments=10, rings=8, material=mat_guard)
    parts.extend([handle, pommel])

    final_mesh = join_objects(parts, "Weapon_Machete")
    export_fbx(os.path.join(MODELS_DIR, "Weapon_Machete.fbx"))

def generate_assault_rifle():
    reset_scene()
    parts = []

    mat_metal = get_or_create_material("Mat_Rifle_Gunmetal", (0.14, 0.15, 0.16, 1.0), metallic=0.92, roughness=0.3)
    mat_wood = get_or_create_material("Mat_Rifle_Wood", (0.42, 0.22, 0.12, 1.0), metallic=0.0, roughness=0.65)
    mat_mag = get_or_create_material("Mat_Rifle_Mag", (0.20, 0.20, 0.22, 1.0), metallic=0.8, roughness=0.4)

    # Receiver
    receiver = create_box("Receiver", (0, 0, 0.05), (0.046, 0.28, 0.08), mat_metal)
    dust_cover = create_cylinder("DustCover", (0, 0, 0.09), 0.022, 0.26, vertices=12, material=mat_metal, rotation=(math.radians(90), 0, 0))
    parts.extend([receiver, dust_cover])

    # Curved Banana Magazine
    mag = create_box("BananaMag", (0, 0.04, -0.06), (0.034, 0.07, 0.18), mat_mag, rotation=(math.radians(-18), 0, 0))
    parts.append(mag)

    # Barrel & Gas Block
    barrel = create_cylinder("Barrel", (0, 0.38, 0.05), 0.014, 0.52, vertices=12, material=mat_metal, rotation=(math.radians(90), 0, 0))
    gas_tube = create_cylinder("GasTube", (0, 0.26, 0.08), 0.012, 0.28, vertices=10, material=mat_metal, rotation=(math.radians(90), 0, 0))
    front_sight = create_box("FrontSightPost", (0, 0.52, 0.085), (0.018, 0.025, 0.065), mat_metal)
    flash_hider = create_cylinder("FlashHider", (0, 0.64, 0.05), 0.018, 0.05, vertices=10, material=mat_metal, rotation=(math.radians(90), 0, 0))
    handguard = create_cylinder("Handguard", (0, 0.24, 0.058), 0.028, 0.22, vertices=12, material=mat_wood, rotation=(math.radians(90), 0, 0))
    parts.extend([barrel, gas_tube, front_sight, flash_hider, handguard])

    # Stock & Pistol Grip
    stock = create_box("Stock", (0, -0.28, 0.02), (0.042, 0.30, 0.11), mat_wood, rotation=(math.radians(6), 0, 0))
    pistol_grip = create_box("PistolGrip", (0, -0.08, -0.04), (0.034, 0.055, 0.12), mat_wood, rotation=(math.radians(22), 0, 0))
    parts.extend([stock, pistol_grip])

    final_mesh = join_objects(parts, "Weapon_AssaultRifle")
    export_fbx(os.path.join(MODELS_DIR, "Weapon_AssaultRifle.fbx"))

def generate_loot_ammo_boxes():
    # 1. 9mm Ammo Box
    reset_scene()
    mat_box9 = get_or_create_material("Mat_Ammo_9mm_Box", (0.28, 0.35, 0.22, 1.0), roughness=0.6) # Military olive
    mat_brass = get_or_create_material("Mat_Brass_Bullet", (0.85, 0.72, 0.28, 1.0), metallic=0.9, roughness=0.25)
    box = create_box("AmmoBox9mm", (0, 0, 0.08), (0.18, 0.12, 0.16), mat_box9, bevel_radius=0.008)
    set_origin_to_bottom(box)
    export_fbx(os.path.join(MODELS_DIR, "Loot_AmmoBox_9mm.fbx"))

    # 2. Shotgun Shell Box
    reset_scene()
    mat_box12 = get_or_create_material("Mat_Ammo_Shotgun_Box", (0.65, 0.18, 0.14, 1.0), roughness=0.7) # Red box
    box_sg = create_box("AmmoBoxShotgun", (0, 0, 0.09), (0.20, 0.14, 0.18), mat_box12, bevel_radius=0.008)
    set_origin_to_bottom(box_sg)
    export_fbx(os.path.join(MODELS_DIR, "Loot_AmmoBox_Shotgun.fbx"))

def generate_medkit():
    reset_scene()
    parts = []
    mat_case = get_or_create_material("Mat_Medkit_White", (0.88, 0.88, 0.90, 1.0), roughness=0.4)
    mat_red = get_or_create_material("Mat_Medkit_Cross", (0.85, 0.08, 0.08, 1.0), roughness=0.3)
    mat_metal = get_or_create_material("Mat_Medkit_Clasps", (0.75, 0.75, 0.78, 1.0), metallic=0.9, roughness=0.2)

    # Briefcase body
    body = create_box("MedkitBody", (0, 0, 0.12), (0.34, 0.14, 0.24), mat_case, bevel_radius=0.012)
    parts.append(body)

    # Red Cross (horizontal and vertical bars)
    cross_h = create_box("CrossH", (0, 0.072, 0.12), (0.16, 0.01, 0.05), mat_red)
    cross_v = create_box("CrossV", (0, 0.072, 0.12), (0.05, 0.01, 0.16), mat_red)
    parts.extend([cross_h, cross_v])

    # Handle & Clasps
    handle = create_cylinder("Handle", (0, 0, 0.26), 0.014, 0.14, vertices=12, material=mat_case, rotation=(0, math.radians(90), 0))
    clasp_l = create_box("ClaspL", (-0.11, 0.072, 0.21), (0.03, 0.014, 0.04), mat_metal)
    clasp_r = create_box("ClaspR", (0.11, 0.072, 0.21), (0.03, 0.014, 0.04), mat_metal)
    parts.extend([handle, clasp_l, clasp_r])

    final_mesh = join_objects(parts, "Loot_Medkit")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Loot_Medkit.fbx"))

def generate_scrap_pile():
    reset_scene()
    parts = []
    mat_rusty = get_or_create_material("Mat_Rust_Metal", (0.42, 0.24, 0.16, 1.0), metallic=0.6, roughness=0.85)
    mat_steel = get_or_create_material("Mat_Scrap_Steel", (0.55, 0.58, 0.62, 1.0), metallic=0.9, roughness=0.45)

    # Pile of industrial gears, pipes, and sheet metal plates
    pipe1 = create_cylinder("Pipe1", (0.05, -0.05, 0.08), 0.06, 0.55, vertices=10, material=mat_steel, rotation=(math.radians(15), math.radians(40), 0))
    pipe2 = create_cylinder("Pipe2", (-0.08, 0.08, 0.06), 0.045, 0.48, vertices=10, material=mat_rusty, rotation=(math.radians(-25), math.radians(65), 0))
    plate1 = create_box("Plate1", (0, 0, 0.03), (0.42, 0.38, 0.03), mat_rusty, rotation=(0, 0, math.radians(12)))
    gear1 = create_cylinder("Gear1", (-0.12, -0.08, 0.12), 0.14, 0.04, vertices=14, material=mat_steel, rotation=(math.radians(35), math.radians(-20), 0))
    parts.extend([pipe1, pipe2, plate1, gear1])

    final_mesh = join_objects(parts, "Loot_ScrapPile")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Loot_ScrapPile.fbx"))

if __name__ == "__main__":
    print("[WeaponGenerator] Generating weapons and loot items...")
    generate_pistol()
    generate_shotgun()
    generate_machete()
    generate_assault_rifle()
    generate_loot_ammo_boxes()
    generate_medkit()
    generate_scrap_pile()
    print("[WeaponGenerator] Weapons and loot items generated successfully!")
