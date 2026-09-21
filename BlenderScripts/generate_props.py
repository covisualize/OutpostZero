import bpy
import os
import sys
import math

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.append(script_dir)

from blender_utils import (
    reset_scene, get_or_create_material, create_box, create_cylinder, 
    create_sphere, create_cone, join_objects, set_origin_to_bottom, export_fbx
)

MODELS_DIR = r"C:\Users\User\.gemini\antigravity\scratch\OutpostZero\Assets\Models\Props"

def generate_jersey_barrier():
    reset_scene()
    parts = []

    mat_concrete = get_or_create_material("Mat_Concrete_Weathered", (0.55, 0.54, 0.52, 1.0), roughness=0.9)
    mat_rebar = get_or_create_material("Mat_Rebar_Rusted", (0.42, 0.22, 0.14, 1.0), metallic=0.7, roughness=0.8)

    # Base wedge (3.0m long, 0.8m wide, 0.35m high)
    base = create_box("BarrierBase", (0, 0, 0.175), (3.0, 0.80, 0.35), mat_concrete, bevel_radius=0.02)
    # Upper stem (3.0m long, 0.35m wide, 0.65m high)
    stem = create_box("BarrierStem", (0, 0, 0.675), (3.0, 0.35, 0.65), mat_concrete, bevel_radius=0.02)
    parts.extend([base, stem])

    # Exposed bent rebar on chipped corner
    rebar1 = create_cylinder("Rebar1", (-1.25, 0.05, 1.05), 0.012, 0.22, vertices=8, material=mat_rebar, rotation=(math.radians(25), math.radians(-15), 0))
    rebar2 = create_cylinder("Rebar2", (-1.18, -0.04, 1.08), 0.012, 0.26, vertices=8, material=mat_rebar, rotation=(math.radians(-35), math.radians(20), 0))
    parts.extend([rebar1, rebar2])

    final_mesh = join_objects(parts, "Barricade_Concrete_Jersey")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Barricade_Concrete_Jersey.fbx"))

def generate_wood_wire_barricade():
    reset_scene()
    parts = []

    mat_wood = get_or_create_material("Mat_Pallet_Wood", (0.45, 0.34, 0.22, 1.0), roughness=0.9)
    mat_wire = get_or_create_material("Mat_Barbed_Wire", (0.58, 0.60, 0.62, 1.0), metallic=0.85, roughness=0.4)

    # 3 wooden pallets standing upright at slight angles (Total width ~2.8m, height ~1.2m)
    for i, px in enumerate([-0.9, 0.0, 0.9]):
        rot_y = math.radians(6 if i % 2 == 0 else -6)
        pallet = create_box(f"Pallet_{i}", (px, 0, 0.6), (0.85, 0.14, 1.2), mat_wood, rotation=(0, 0, rot_y))
        parts.append(pallet)

    # Angled support stakes behind
    stake1 = create_cylinder("Stake1", (-0.7, -0.45, 0.5), 0.04, 1.1, vertices=8, material=mat_wood, rotation=(math.radians(-45), 0, 0))
    stake2 = create_cylinder("Stake2", (0.7, -0.45, 0.5), 0.04, 1.1, vertices=8, material=mat_wood, rotation=(math.radians(-45), 0, 0))
    parts.extend([stake1, stake2])

    # Barbed wire coil loops across the front
    for cx in [-0.8, -0.3, 0.3, 0.8]:
        coil = create_cylinder(f"WireCoil_{cx}", (cx, 0.12, 0.65), 0.22, 0.04, vertices=12, material=mat_wire, rotation=(0, math.radians(90), 0))
        parts.append(coil)

    final_mesh = join_objects(parts, "Barricade_Wood_Wire")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Barricade_Wood_Wire.fbx"))

def generate_sandbags():
    reset_scene()
    parts = []

    mat_canvas = get_or_create_material("Mat_Sandbag_Canvas", (0.64, 0.58, 0.44, 1.0), roughness=0.95)

    # 3 layers of stacked sandbags (curved bunker wall, 2.6m wide, 0.9m high)
    layer_configs = [
        (0.12, [-1.0, -0.6, -0.2, 0.2, 0.6, 1.0]),
        (0.36, [-0.8, -0.4, 0.0, 0.4, 0.8]),
        (0.60, [-0.6, -0.2, 0.2, 0.6]),
        (0.84, [-0.4, 0.0, 0.4])
    ]

    for z_pos, x_positions in layer_configs:
        for idx, x_pos in enumerate(x_positions):
            y_curv = 0.18 * (1.0 - (abs(x_pos) / 1.2)**2)
            bag = create_box(f"Bag_{z_pos}_{idx}", (x_pos, y_curv, z_pos), (0.42, 0.26, 0.22), mat_canvas, bevel_radius=0.04)
            parts.append(bag)

    final_mesh = join_objects(parts, "Barricade_Sandbags")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Barricade_Sandbags.fbx"))

def generate_wrecked_sedan():
    reset_scene()
    parts = []

    mat_car_paint = get_or_create_material("Mat_Sedan_RustedPaint", (0.32, 0.18, 0.14, 1.0), metallic=0.5, roughness=0.75) # Rusted maroon
    mat_tire = get_or_create_material("Mat_Car_TireRubber", (0.10, 0.10, 0.10, 1.0), roughness=0.9)
    mat_rim = get_or_create_material("Mat_Car_RimSteel", (0.35, 0.36, 0.38, 1.0), metallic=0.9, roughness=0.5)
    mat_glass = get_or_create_material("Mat_Car_BrokenGlass", (0.20, 0.25, 0.30, 1.0), metallic=0.3, roughness=0.3)
    mat_bumper = get_or_create_material("Mat_Car_BumperChrome", (0.65, 0.65, 0.68, 1.0), metallic=0.8, roughness=0.4)

    # 1. Main Chassis & Hood (4.4m long, 1.85m wide, 1.35m high)
    lower_body = create_box("LowerBody", (0, 0, 0.48), (1.85, 4.2, 0.48), mat_car_paint, bevel_radius=0.04)
    cabin_roof = create_box("CabinRoof", (0, -0.25, 1.02), (1.55, 2.1, 0.58), mat_car_paint, bevel_radius=0.03)
    hood = create_box("Hood", (0, 1.25, 0.68), (1.75, 1.45, 0.12), mat_car_paint)
    trunk = create_box("Trunk", (0, -1.55, 0.66), (1.75, 1.1, 0.14), mat_car_paint)
    parts.extend([lower_body, cabin_roof, hood, trunk])

    # 2. Windshield & Windows
    windshield = create_box("Windshield", (0, 0.72, 0.98), (1.50, 0.35, 0.48), mat_glass, rotation=(math.radians(35), 0, 0))
    rear_window = create_box("RearWindow", (0, -1.22, 0.98), (1.50, 0.35, 0.48), mat_glass, rotation=(math.radians(-35), 0, 0))
    parts.extend([windshield, rear_window])

    # 3. Bumpers
    front_bumper = create_box("FrontBumper", (0, 2.15, 0.38), (1.92, 0.20, 0.22), mat_bumper)
    rear_bumper = create_box("RearBumper", (0, -2.15, 0.38), (1.92, 0.20, 0.22), mat_bumper)
    parts.extend([front_bumper, rear_bumper])

    # 4. Wheels (flat / askew)
    wheel_positions = [
        (-0.95, 1.35),
        (0.95, 1.35),
        (-0.95, -1.35),
        (0.95, -1.35)
    ]
    for idx, (wx, wy) in enumerate(wheel_positions):
        tire = create_cylinder(f"Tire_{idx}", (wx, wy, 0.28), 0.32, 0.22, vertices=14, material=mat_tire, rotation=(0, math.radians(90), 0))
        rim = create_cylinder(f"Rim_{idx}", (wx * 1.02, wy, 0.28), 0.20, 0.24, vertices=12, material=mat_rim, rotation=(0, math.radians(90), 0))
        parts.extend([tire, rim])

    final_mesh = join_objects(parts, "Vehicle_Wrecked_Sedan")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Vehicle_Wrecked_Sedan.fbx"))

def generate_apocalypse_truck():
    reset_scene()
    parts = []

    mat_truck_cab = get_or_create_material("Mat_Truck_ArmoredSteel", (0.22, 0.25, 0.20, 1.0), metallic=0.7, roughness=0.6) # Olive green armor
    mat_armor_plate = get_or_create_material("Mat_Armor_Plates", (0.16, 0.16, 0.18, 1.0), metallic=0.85, roughness=0.5)
    mat_tire = get_or_create_material("Mat_Truck_Tire", (0.08, 0.08, 0.08, 1.0), roughness=0.9)
    mat_bullbar = get_or_create_material("Mat_Truck_BullBar", (0.12, 0.12, 0.12, 1.0), metallic=0.9, roughness=0.4)

    # 1. Cab & Bed (5.2m long, 2.1m wide, 1.9m high)
    cab = create_box("TruckCab", (0, 0.6, 1.15), (2.1, 2.2, 1.2), mat_truck_cab)
    bed = create_box("TruckBed", (0, -1.4, 0.75), (2.05, 2.4, 0.65), mat_truck_cab)
    parts.extend([cab, bed])

    # 2. Armored Window Slits
    visor = create_box("ArmoredVisor", (0, 1.72, 1.35), (1.8, 0.25, 0.28), mat_armor_plate)
    parts.append(visor)

    # 3. Heavy Front Bull-Bar Ram Grill
    bull_grill = create_box("BullBarMain", (0, 2.45, 0.75), (2.2, 0.25, 0.8), mat_bullbar)
    spikes1 = create_cone("RamSpikeL", (-0.6, 2.65, 0.75), 0.08, 0.01, 0.35, vertices=8, material=mat_bullbar, rotation=(math.radians(90), 0, 0))
    spikes2 = create_cone("RamSpikeR", (0.6, 2.65, 0.75), 0.08, 0.01, 0.35, vertices=8, material=mat_bullbar, rotation=(math.radians(90), 0, 0))
    parts.extend([bull_grill, spikes1, spikes2])

    # 4. Large Heavy Wheels
    for idx, (wx, wy) in enumerate([(-1.1, 1.5), (1.1, 1.5), (-1.1, -1.4), (1.1, -1.4)]):
        wheel = create_cylinder(f"HeavyWheel_{idx}", (wx, wy, 0.45), 0.46, 0.32, vertices=14, material=mat_tire, rotation=(0, math.radians(90), 0))
        parts.append(wheel)

    final_mesh = join_objects(parts, "Vehicle_Apocalypse_Truck")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Vehicle_Apocalypse_Truck.fbx"))

def generate_dumpster():
    reset_scene()
    parts = []

    mat_dumpster = get_or_create_material("Mat_Dumpster_Green", (0.18, 0.35, 0.24, 1.0), metallic=0.6, roughness=0.7)
    mat_lid = get_or_create_material("Mat_Dumpster_BlackLid", (0.10, 0.10, 0.10, 1.0), roughness=0.8)
    mat_rust = get_or_create_material("Mat_Dumpster_Rust", (0.42, 0.24, 0.15, 1.0), roughness=0.9)

    # Main container body (2.2m wide, 1.3m deep, 1.35m high)
    body = create_box("DumpsterBody", (0, 0, 0.72), (2.2, 1.3, 1.1), mat_dumpster)
    parts.append(body)

    # Side forklift pockets
    pocket_l = create_box("ForkPocketL", (-1.15, 0, 0.70), (0.12, 1.1, 0.18), mat_rust)
    pocket_r = create_box("ForkPocketR", (1.15, 0, 0.70), (0.12, 1.1, 0.18), mat_rust)
    parts.extend([pocket_l, pocket_r])

    # Plastic lids (one closed, one open/askew)
    lid_closed = create_box("LidClosed", (-0.56, 0, 1.32), (1.08, 1.34, 0.08), mat_lid)
    lid_open = create_box("LidOpen", (0.56, 0.22, 1.62), (1.08, 1.34, 0.08), mat_lid, rotation=(math.radians(35), 0, 0))
    parts.extend([lid_closed, lid_open])

    final_mesh = join_objects(parts, "Prop_Dumpster")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Prop_Dumpster.fbx"))

def generate_barrels():
    # 1. Red Explosive Fuel Drum
    reset_scene()
    mat_red = get_or_create_material("Mat_Barrel_ExplosiveRed", (0.78, 0.14, 0.12, 1.0), metallic=0.7, roughness=0.45)
    mat_hazard = get_or_create_material("Mat_Hazard_Stripe", (0.85, 0.75, 0.12, 1.0), roughness=0.6)
    
    parts_r = []
    drum = create_cylinder("DrumBody", (0, 0, 0.48), 0.30, 0.94, vertices=16, material=mat_red)
    ring1 = create_cylinder("Ring1", (0, 0, 0.32), 0.315, 0.035, vertices=16, material=mat_hazard)
    ring2 = create_cylinder("Ring2", (0, 0, 0.64), 0.315, 0.035, vertices=16, material=mat_hazard)
    parts_r.extend([drum, ring1, ring2])
    
    final_r = join_objects(parts_r, "Prop_Barrel_Red_Explosive")
    set_origin_to_bottom(final_r)
    export_fbx(os.path.join(MODELS_DIR, "Prop_Barrel_Red_Explosive.fbx"))

    # 2. Toxic Biohazard Drum
    reset_scene()
    mat_toxic = get_or_create_material("Mat_Barrel_ToxicYellow", (0.65, 0.72, 0.18, 1.0), metallic=0.65, roughness=0.5)
    mat_bio_symbol = get_or_create_material("Mat_Biohazard_Black", (0.12, 0.12, 0.12, 1.0), roughness=0.7)
    
    parts_t = []
    drum_t = create_cylinder("ToxicDrum", (0, 0, 0.48), 0.30, 0.94, vertices=16, material=mat_toxic)
    ring_t = create_cylinder("RingT", (0, 0, 0.48), 0.315, 0.06, vertices=16, material=mat_bio_symbol)
    parts_t.extend([drum_t, ring_t])
    
    final_t = join_objects(parts_t, "Prop_Barrel_Toxic")
    set_origin_to_bottom(final_t)
    export_fbx(os.path.join(MODELS_DIR, "Prop_Barrel_Toxic.fbx"))

    # 3. Rusted Oil Drum
    reset_scene()
    mat_oil = get_or_create_material("Mat_Barrel_OilBlue", (0.18, 0.26, 0.36, 1.0), metallic=0.7, roughness=0.6)
    drum_oil = create_cylinder("OilDrum", (0, 0, 0.48), 0.30, 0.94, vertices=16, material=mat_oil)
    set_origin_to_bottom(drum_oil)
    export_fbx(os.path.join(MODELS_DIR, "Prop_Barrel_Oil.fbx"))

def generate_crates():
    # 1. Wooden Cargo Crate (1.2m x 1.2m x 1.2m)
    reset_scene()
    parts_w = []
    mat_wood = get_or_create_material("Mat_Crate_Lumber", (0.52, 0.38, 0.24, 1.0), roughness=0.88)
    mat_trim = get_or_create_material("Mat_Crate_Bracing", (0.38, 0.28, 0.18, 1.0), roughness=0.9)

    core = create_box("CrateCore", (0, 0, 0.6), (1.18, 1.18, 1.18), mat_wood)
    parts_w.append(core)

    # Diagonal corner braces
    brace1 = create_box("Brace1", (0, 0.60, 0.6), (1.20, 0.04, 0.14), mat_trim, rotation=(0, math.radians(45), 0))
    brace2 = create_box("Brace2", (0, -0.60, 0.6), (1.20, 0.04, 0.14), mat_trim, rotation=(0, math.radians(-45), 0))
    parts_w.extend([brace1, brace2])

    final_w = join_objects(parts_w, "Prop_Crate_Wood")
    set_origin_to_bottom(final_w)
    export_fbx(os.path.join(MODELS_DIR, "Prop_Crate_Wood.fbx"))

    # 2. Military Ammo Crate
    reset_scene()
    parts_m = []
    mat_mil_green = get_or_create_material("Mat_Mil_OliveDrab", (0.24, 0.28, 0.18, 1.0), roughness=0.75)
    mat_latches = get_or_create_material("Mat_Mil_Latches", (0.12, 0.12, 0.14, 1.0), metallic=0.9, roughness=0.3)

    box_mil = create_box("MilCrateBody", (0, 0, 0.26), (0.95, 0.55, 0.50), mat_mil_green, bevel_radius=0.015)
    latch_l = create_box("LatchL", (-0.28, 0.28, 0.32), (0.08, 0.02, 0.12), mat_latches)
    latch_r = create_box("LatchR", (0.28, 0.28, 0.32), (0.08, 0.02, 0.12), mat_latches)
    parts_m.extend([box_mil, latch_l, latch_r])

    final_m = join_objects(parts_m, "Prop_Crate_Military")
    set_origin_to_bottom(final_m)
    export_fbx(os.path.join(MODELS_DIR, "Prop_Crate_Military.fbx"))

def generate_street_furniture():
    # 1. Street Lamp
    reset_scene()
    parts_l = []
    mat_lamp_post = get_or_create_material("Mat_Lamp_Metal", (0.20, 0.22, 0.24, 1.0), metallic=0.85, roughness=0.4)
    mat_light_head = get_or_create_material("Mat_Lamp_Bulb", (0.95, 0.95, 0.85, 1.0), roughness=0.2)

    base_plate = create_cylinder("LampBase", (0, 0, 0.15), 0.24, 0.30, vertices=12, material=mat_lamp_post)
    pole = create_cylinder("LampPole", (0, 0, 2.6), 0.08, 4.8, vertices=12, material=mat_lamp_post)
    arm = create_cylinder("LampArm", (0.45, 0, 4.9), 0.06, 1.1, vertices=10, material=mat_lamp_post, rotation=(0, math.radians(75), 0))
    fixture = create_box("LampFixture", (0.95, 0, 4.8), (0.55, 0.28, 0.18), mat_lamp_post)
    bulb = create_box("BulbCover", (0.95, 0, 4.70), (0.45, 0.22, 0.04), mat_light_head)
    parts_l.extend([base_plate, pole, arm, fixture, bulb])

    final_l = join_objects(parts_l, "Prop_StreetLamp")
    set_origin_to_bottom(final_l)
    export_fbx(os.path.join(MODELS_DIR, "Prop_StreetLamp.fbx"))

    # 2. Park Bench
    reset_scene()
    parts_b = []
    mat_cast_iron = get_or_create_material("Mat_Bench_CastIron", (0.12, 0.13, 0.14, 1.0), metallic=0.9, roughness=0.5)
    mat_slats = get_or_create_material("Mat_Bench_Wood", (0.48, 0.32, 0.20, 1.0), roughness=0.8)

    leg_l = create_box("BenchLegL", (-0.85, 0, 0.35), (0.08, 0.65, 0.70), mat_cast_iron)
    leg_r = create_box("BenchLegR", (0.85, 0, 0.35), (0.08, 0.65, 0.70), mat_cast_iron)
    seat = create_box("BenchSeat", (0, -0.05, 0.44), (1.95, 0.48, 0.06), mat_slats)
    back = create_box("BenchBack", (0, -0.26, 0.72), (1.95, 0.06, 0.44), mat_slats, rotation=(math.radians(-12), 0, 0))
    parts_b.extend([leg_l, leg_r, seat, back])

    final_b = join_objects(parts_b, "Prop_StreetBench")
    set_origin_to_bottom(final_b)
    export_fbx(os.path.join(MODELS_DIR, "Prop_StreetBench.fbx"))

if __name__ == "__main__":
    print("[PropsGenerator] Generating urban props, barricades & vehicles...")
    generate_jersey_barrier()
    generate_wood_wire_barricade()
    generate_sandbags()
    generate_wrecked_sedan()
    generate_apocalypse_truck()
    generate_dumpster()
    generate_barrels()
    generate_crates()
    generate_street_furniture()
    print("[PropsGenerator] Props generated successfully!")
