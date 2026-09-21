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

MODELS_DIR = models_dir("Environment")

def generate_road_straight():
    reset_scene()
    parts = []

    mat_asphalt = get_or_create_material("Mat_Road_Asphalt", (0.16, 0.17, 0.18, 1.0), roughness=0.9)
    mat_curb = get_or_create_material("Mat_Concrete_Curb", (0.52, 0.53, 0.55, 1.0), roughness=0.8)
    mat_sidewalk = get_or_create_material("Mat_Sidewalk_Tile", (0.62, 0.60, 0.58, 1.0), roughness=0.85)
    mat_stripes = get_or_create_material("Mat_Road_Stripes_Yellow", (0.85, 0.72, 0.15, 1.0), roughness=0.6)

    # Road base (6m wide roadway in center)
    roadway = create_box("Roadway", (0, 0, 0.02), (6.0, 10.0, 0.04), mat_asphalt)
    parts.append(roadway)

    # Dashed center line markings
    for y_pos in [-3.5, -1.2, 1.2, 3.5]:
        stripe = create_box(f"Stripe_{y_pos}", (0, y_pos, 0.042), (0.18, 1.4, 0.005), mat_stripes)
        parts.append(stripe)

    # Left Sidewalk & Curb (2m wide)
    curb_l = create_box("CurbL", (-3.10, 0, 0.08), (0.20, 10.0, 0.16), mat_curb)
    walk_l = create_box("SidewalkL", (-4.10, 0, 0.08), (1.80, 10.0, 0.16), mat_sidewalk)
    parts.extend([curb_l, walk_l])

    # Right Sidewalk & Curb (2m wide)
    curb_r = create_box("CurbR", (3.10, 0, 0.08), (0.20, 10.0, 0.16), mat_curb)
    walk_r = create_box("SidewalkR", (4.10, 0, 0.08), (1.80, 10.0, 0.16), mat_sidewalk)
    parts.extend([curb_r, walk_r])

    final_mesh = join_objects(parts, "Road_Tile_Straight")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Road_Tile_Straight.fbx"))

def generate_road_intersection():
    reset_scene()
    parts_x = []

    mat_asphalt = get_or_create_material("Mat_Road_Asphalt", (0.16, 0.17, 0.18, 1.0), roughness=0.9)
    mat_sidewalk = get_or_create_material("Mat_Sidewalk_Tile", (0.62, 0.60, 0.58, 1.0), roughness=0.85)
    mat_white_line = get_or_create_material("Mat_Road_Stripes_White", (0.85, 0.85, 0.88, 1.0), roughness=0.6)
    
    inter_base = create_box("IntersectionBase", (0, 0, 0.02), (10.0, 10.0, 0.04), mat_asphalt)
    parts_x.append(inter_base)

    # 4 Corner Sidewalk quadrants (2m x 2m corners)
    for cx in [-4.0, 4.0]:
        for cy in [-4.0, 4.0]:
            corner = create_box(f"CornerWalk_{cx}_{cy}", (cx, cy, 0.08), (2.0, 2.0, 0.16), mat_sidewalk)
            parts_x.append(corner)

    # Crosswalk zebra strips
    for offset in [-1.5, -0.5, 0.5, 1.5]:
        # North zebra
        z_n = create_box(f"ZebraN_{offset}", (offset, 2.6, 0.042), (0.45, 1.0, 0.005), mat_white_line)
        # South zebra
        z_s = create_box(f"ZebraS_{offset}", (offset, -2.6, 0.042), (0.45, 1.0, 0.005), mat_white_line)
        # West zebra
        z_w = create_box(f"ZebraW_{offset}", (-2.6, offset, 0.042), (1.0, 0.45, 0.005), mat_white_line)
        # East zebra
        z_e = create_box(f"ZebraE_{offset}", (2.6, offset, 0.042), (1.0, 0.45, 0.005), mat_white_line)
        parts_x.extend([z_n, z_s, z_w, z_e])

    final_mesh_x = join_objects(parts_x, "Road_Tile_Intersection")
    set_origin_to_bottom(final_mesh_x)
    export_fbx(os.path.join(MODELS_DIR, "Road_Tile_Intersection.fbx"))

def generate_storefront_building():
    reset_scene()
    parts = []

    mat_brick = get_or_create_material("Mat_Bldg_Brick", (0.54, 0.26, 0.20, 1.0), roughness=0.9)
    mat_concrete = get_or_create_material("Mat_Bldg_ConcreteTrim", (0.60, 0.58, 0.55, 1.0), roughness=0.8)
    mat_glass_broken = get_or_create_material("Mat_Bldg_GlassBroken", (0.18, 0.28, 0.32, 1.0), metallic=0.4, roughness=0.2)
    mat_sign = get_or_create_material("Mat_Bldg_Signboard", (0.15, 0.18, 0.22, 1.0), roughness=0.7)
    mat_metal = get_or_create_material("Mat_Bldg_Ironwork", (0.20, 0.22, 0.24, 1.0), metallic=0.85, roughness=0.4)

    # 1. Main structure (10m wide, 8m deep, 7.2m high)
    main_body = create_box("StorefrontBody", (0, 0, 3.6), (10.0, 8.0, 7.2), mat_brick)
    parts.append(main_body)

    # 2. Storefront Base Pillars & Lintel
    pillar_l = create_box("PillarL", (-4.7, 4.05, 1.8), (0.6, 0.3, 3.6), mat_concrete)
    pillar_r = create_box("PillarR", (4.7, 4.05, 1.8), (0.6, 0.3, 3.6), mat_concrete)
    pillar_c = create_box("PillarC", (0.0, 4.05, 1.8), (0.6, 0.3, 3.6), mat_concrete)
    lintel = create_box("StorefrontLintel", (0, 4.05, 3.6), (10.2, 0.35, 0.5), mat_concrete)
    parts.extend([pillar_l, pillar_r, pillar_c, lintel])

    # 3. Storefront Windows (Display frames)
    window_l = create_box("DisplayWindowL", (-2.35, 4.02, 1.8), (4.0, 0.08, 2.6), mat_glass_broken)
    window_r = create_box("DisplayWindowR", (2.35, 4.02, 1.8), (4.0, 0.08, 2.6), mat_glass_broken)
    parts.extend([window_l, window_r])

    # 4. Weathered Signboard
    sign = create_box("StoreSignboard", (0, 4.15, 4.15), (7.5, 0.18, 0.95), mat_sign)
    parts.append(sign)

    # 5. Upper Windows (2nd floor)
    for wx in [-3.2, -1.1, 1.1, 3.2]:
        win_frame = create_box(f"WinFrame_{wx}", (wx, 4.04, 5.5), (1.4, 0.12, 1.8), mat_concrete)
        win_glass = create_box(f"WinGlass_{wx}", (wx, 4.02, 5.5), (1.2, 0.06, 1.6), mat_glass_broken)
        parts.extend([win_frame, win_glass])

    # 6. Rooftop Parapet & AC Condenser
    roof_parapet = create_box("Parapet", (0, 0, 7.35), (10.2, 8.2, 0.4), mat_concrete)
    ac_unit = create_box("ACUnit", (2.5, -1.5, 7.75), (1.8, 1.4, 1.1), mat_metal)
    ac_fan = create_cylinder("ACFanCover", (2.5, -1.5, 8.32), 0.5, 0.06, vertices=12, material=mat_metal)
    parts.extend([roof_parapet, ac_unit, ac_fan])

    final_mesh = join_objects(parts, "Building_Storefront_2Story")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Building_Storefront_2Story.fbx"))

def generate_industrial_warehouse():
    reset_scene()
    parts = []

    mat_corrugated = get_or_create_material("Mat_Corrugated_Steel", (0.40, 0.44, 0.46, 1.0), metallic=0.75, roughness=0.55)
    mat_steel_frame = get_or_create_material("Mat_Warehouse_IBeam", (0.18, 0.18, 0.20, 1.0), metallic=0.9, roughness=0.4)
    mat_rolldoor = get_or_create_material("Mat_Rollup_Door", (0.55, 0.42, 0.24, 1.0), metallic=0.5, roughness=0.8) # Rusted door
    mat_concrete = get_or_create_material("Mat_Warehouse_Foundation", (0.48, 0.48, 0.50, 1.0), roughness=0.9)

    # 1. Main structure (14m wide, 10m deep, 6.5m high)
    foundation = create_box("Foundation", (0, 0, 0.4), (14.2, 10.2, 0.8), mat_concrete)
    walls = create_box("CorrugatedWalls", (0, 0, 3.8), (14.0, 10.0, 6.0), mat_corrugated)
    parts.extend([foundation, walls])

    # 2. Structural I-Beam Columns on corners & front
    for bx in [-7.0, -2.5, 2.5, 7.0]:
        beam = create_box(f"IBeam_{bx}", (bx, 5.08, 3.8), (0.45, 0.35, 6.2), mat_steel_frame)
        parts.append(beam)

    # 3. Roll-Up Industrial Loading Bay Door
    rollup_door = create_box("RollupDoor", (0, 5.04, 2.4), (4.4, 0.12, 4.0), mat_rolldoor)
    door_hood = create_box("DoorHood", (0, 5.15, 4.5), (4.8, 0.45, 0.35), mat_steel_frame)
    parts.extend([rollup_door, door_hood])

    # 4. Rooftop Industrial Exhaust Vents
    roof_trim = create_box("RoofPeak", (0, 0, 6.9), (14.3, 10.3, 0.3), mat_steel_frame)
    parts.append(roof_trim)

    vent1 = create_cylinder("ExhaustVent1", (-3.5, 1.5, 7.5), 0.55, 1.2, vertices=12, material=mat_steel_frame)
    vent_cap1 = create_cone("VentCap1", (-3.5, 1.5, 8.2), 0.75, 0.1, 0.35, vertices=12, material=mat_steel_frame)
    parts.extend([vent1, vent_cap1])

    final_mesh = join_objects(parts, "Building_Warehouse_Depot")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Building_Warehouse_Depot.fbx"))

def generate_ruin_wall_corner():
    reset_scene()
    parts = []

    mat_brick = get_or_create_material("Mat_Ruin_Brick", (0.50, 0.22, 0.18, 1.0), roughness=0.95)
    mat_plaster = get_or_create_material("Mat_Ruin_Plaster", (0.72, 0.70, 0.65, 1.0), roughness=0.88)
    mat_rubble = get_or_create_material("Mat_Ruin_Rubble", (0.45, 0.44, 0.42, 1.0), roughness=0.9)

    # Wall segment 1 (North-South: 4m long, 3m high, 0.5m thick)
    wall1 = create_box("RuinWall1", (0, 2.0, 1.5), (0.5, 4.0, 3.0), mat_brick)
    plaster1 = create_box("Plaster1", (0.27, 2.0, 1.5), (0.05, 3.6, 2.7), mat_plaster)
    parts.extend([wall1, plaster1])

    # Wall segment 2 (East-West: 4m long, 3m high, 0.5m thick)
    wall2 = create_box("RuinWall2", (2.0, 0, 1.5), (4.0, 0.5, 3.0), mat_brick)
    plaster2 = create_box("Plaster2", (2.0, 0.27, 1.5), (3.6, 0.05, 2.7), mat_plaster)
    parts.extend([wall2, plaster2])

    # Rubble piles at the base
    rubble1 = create_box("Rubble1", (0.8, 0.8, 0.25), (1.4, 1.4, 0.5), mat_rubble, rotation=(0, 0, math.radians(25)))
    rubble2 = create_box("Rubble2", (-0.4, 1.2, 0.18), (0.8, 0.9, 0.35), mat_rubble, rotation=(0, 0, math.radians(-15)))
    parts.extend([rubble1, rubble2])

    final_mesh = join_objects(parts, "Ruin_Wall_Corner")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Ruin_Wall_Corner.fbx"))

if __name__ == "__main__":
    print("[ArchitectureGenerator] Generating modular architecture & buildings...")
    generate_road_straight()
    generate_road_intersection()
    generate_storefront_building()
    generate_industrial_warehouse()
    generate_ruin_wall_corner()
    print("[ArchitectureGenerator] Architecture models generated successfully!")
