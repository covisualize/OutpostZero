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

MODELS_DIR = models_dir("BaseBuilding")

def generate_workbench():
    reset_scene()
    parts = []

    mat_wood = get_or_create_material("Mat_Bench_Timber", (0.46, 0.32, 0.20, 1.0), roughness=0.88)
    mat_metal = get_or_create_material("Mat_Bench_ViseMetal", (0.35, 0.38, 0.42, 1.0), metallic=0.9, roughness=0.35)
    mat_pegboard = get_or_create_material("Mat_Pegboard_Brown", (0.32, 0.24, 0.16, 1.0), roughness=0.9)

    # 1. Main Table Top (1.8m wide, 0.9m deep, 0.9m high)
    table_top = create_box("TableTop", (0, 0, 0.88), (1.8, 0.9, 0.08), mat_wood, bevel_radius=0.01)
    parts.append(table_top)

    # 4 Heavy legs
    for lx in [-0.80, 0.80]:
        for ly in [-0.35, 0.35]:
            leg = create_box(f"Leg_{lx}_{ly}", (lx, ly, 0.42), (0.12, 0.12, 0.84), mat_wood)
            parts.append(leg)

    # Lower shelf
    shelf = create_box("LowerShelf", (0, 0, 0.22), (1.6, 0.75, 0.04), mat_wood)
    parts.append(shelf)

    # Back Tool Pegboard
    pegboard = create_box("BackPegboard", (0, 0.42, 1.40), (1.8, 0.04, 0.95), mat_pegboard)
    parts.append(pegboard)

    # Mounted Corner Vise
    vise_base = create_box("ViseBase", (0.75, -0.32, 0.96), (0.16, 0.16, 0.08), mat_metal)
    vise_jaws = create_box("ViseJaws", (0.75, -0.32, 1.05), (0.14, 0.22, 0.10), mat_metal)
    parts.extend([vise_base, vise_jaws])

    final_mesh = join_objects(parts, "Base_CraftingWorkbench")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Base_CraftingWorkbench.fbx"))

def generate_campfire_cooker():
    reset_scene()
    parts = []

    mat_stone = get_or_create_material("Mat_Fire_Stones", (0.42, 0.40, 0.38, 1.0), roughness=0.95)
    mat_log = get_or_create_material("Mat_Fire_Logs", (0.22, 0.14, 0.08, 1.0), roughness=0.9)
    mat_embers = get_or_create_material("Mat_Fire_Embers", (0.95, 0.35, 0.05, 1.0), roughness=0.5)
    mat_pot = get_or_create_material("Mat_Cook_CastIron", (0.12, 0.12, 0.14, 1.0), metallic=0.85, roughness=0.45)
    mat_stakes = get_or_create_material("Mat_Cook_Stakes", (0.35, 0.25, 0.15, 1.0), roughness=0.9)

    # Stone fire ring (radius 0.65m)
    num_stones = 10
    for i in range(num_stones):
        angle = (2 * math.pi * i) / num_stones
        sx = 0.65 * math.cos(angle)
        sy = 0.65 * math.sin(angle)
        stone = create_sphere(f"Stone_{i}", (sx, sy, 0.10), 0.13, segments=8, rings=6, material=mat_stone)
        parts.append(stone)

    # Central glowing embers bed
    embers = create_cylinder("Embers", (0, 0, 0.04), 0.45, 0.08, vertices=12, material=mat_embers)
    parts.append(embers)

    # Crossed charred logs
    log1 = create_cylinder("Log1", (0, 0, 0.12), 0.065, 0.70, vertices=8, material=mat_log, rotation=(0, math.radians(70), math.radians(30)))
    log2 = create_cylinder("Log2", (0, 0, 0.12), 0.065, 0.70, vertices=8, material=mat_log, rotation=(0, math.radians(70), math.radians(-50)))
    parts.extend([log1, log2])

    # Cooking spit stakes (L and R) and crossbeam
    stake_l = create_cylinder("SpitStakeL", (-0.60, 0, 0.65), 0.035, 1.30, vertices=8, material=mat_stakes)
    stake_r = create_cylinder("SpitStakeR", (0.60, 0, 0.65), 0.035, 1.30, vertices=8, material=mat_stakes)
    crossbeam = create_cylinder("Crossbeam", (0, 0, 1.15), 0.025, 1.35, vertices=8, material=mat_stakes, rotation=(0, math.radians(90), 0))
    parts.extend([stake_l, stake_r, crossbeam])

    # Hanging Stewpot
    pot = create_cylinder("CookingPot", (0, 0, 0.72), 0.20, 0.25, vertices=14, material=mat_pot)
    pot_lid = create_sphere("PotLid", (0, 0, 0.85), 0.18, segments=12, rings=6, material=mat_pot)
    parts.extend([pot, pot_lid])

    final_mesh = join_objects(parts, "Base_Campfire_Cooker")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Base_Campfire_Cooker.fbx"))

def generate_generator():
    reset_scene()
    parts = []

    mat_frame = get_or_create_material("Mat_Gen_FrameRed", (0.68, 0.18, 0.15, 1.0), metallic=0.7, roughness=0.5)
    mat_engine = get_or_create_material("Mat_Gen_EngineMetal", (0.20, 0.22, 0.24, 1.0), metallic=0.9, roughness=0.35)
    mat_panel = get_or_create_material("Mat_Gen_ControlPanel", (0.75, 0.75, 0.72, 1.0), roughness=0.4)

    # Roll cage frame (1.1m x 0.75m x 0.85m)
    cage_base = create_box("CageBase", (0, 0, 0.05), (1.1, 0.75, 0.08), mat_frame)
    parts.append(cage_base)

    # Engine Block in center
    engine = create_box("EngineBlock", (0, -0.05, 0.42), (0.75, 0.55, 0.62), mat_engine)
    parts.append(engine)

    # Fuel Tank on top
    fuel_tank = create_box("FuelTank", (0, -0.05, 0.78), (0.70, 0.50, 0.18), mat_frame, bevel_radius=0.03)
    fuel_cap = create_cylinder("FuelCap", (0.22, -0.05, 0.89), 0.045, 0.04, vertices=12, material=mat_engine)
    parts.extend([fuel_tank, fuel_cap])

    # Exhaust pipe with rain cap
    exhaust = create_cylinder("ExhaustPipe", (-0.32, -0.28, 0.65), 0.035, 0.45, vertices=10, material=mat_engine)
    parts.append(exhaust)

    # Control Panel on front
    panel = create_box("ControlPanel", (0, 0.32, 0.46), (0.55, 0.06, 0.35), mat_panel)
    gauge = create_cylinder("VoltageGauge", (-0.12, 0.35, 0.48), 0.05, 0.02, vertices=12, material=mat_engine, rotation=(math.radians(90), 0, 0))
    parts.extend([panel, gauge])

    final_mesh = join_objects(parts, "Base_Generator_Diesel")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Base_Generator_Diesel.fbx"))

def generate_medical_cot():
    reset_scene()
    parts = []

    mat_tubing = get_or_create_material("Mat_Cot_MetalTube", (0.55, 0.55, 0.58, 1.0), metallic=0.9, roughness=0.3)
    mat_canvas = get_or_create_material("Mat_Cot_Canvas", (0.28, 0.32, 0.24, 1.0), roughness=0.9)
    mat_pillow = get_or_create_material("Mat_Cot_Pillow", (0.85, 0.85, 0.88, 1.0), roughness=0.8)
    mat_blanket = get_or_create_material("Mat_Cot_Blanket", (0.42, 0.20, 0.18, 1.0), roughness=0.85)

    # Dimensions: 2.0m long, 0.85m wide, 0.45m high
    frame_l = create_cylinder("RailL", (-0.40, 0, 0.42), 0.022, 2.05, vertices=10, material=mat_tubing, rotation=(math.radians(90), 0, 0))
    frame_r = create_cylinder("RailR", (0.40, 0, 0.42), 0.022, 2.05, vertices=10, material=mat_tubing, rotation=(math.radians(90), 0, 0))
    parts.extend([frame_l, frame_r])

    # X-Legs (3 pairs)
    for ly in [-0.8, 0.0, 0.8]:
        leg1 = create_cylinder(f"XLeg1_{ly}", (0, ly, 0.22), 0.018, 0.55, vertices=8, material=mat_tubing, rotation=(0, math.radians(35), 0))
        leg2 = create_cylinder(f"XLeg2_{ly}", (0, ly, 0.22), 0.018, 0.55, vertices=8, material=mat_tubing, rotation=(0, math.radians(-35), 0))
        parts.extend([leg1, leg2])

    # Canvas stretched surface
    canvas = create_box("CotCanvas", (0, 0, 0.42), (0.78, 1.95, 0.03), mat_canvas)
    pillow = create_box("CotPillow", (0, 0.72, 0.47), (0.55, 0.35, 0.08), mat_pillow, bevel_radius=0.02)
    blanket = create_box("CotBlanket", (0, -0.22, 0.46), (0.75, 1.15, 0.05), mat_blanket)
    parts.extend([canvas, pillow, blanket])

    final_mesh = join_objects(parts, "Base_MedicalCot")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Base_MedicalCot.fbx"))

def generate_watchtower():
    reset_scene()
    parts = []

    mat_timbers = get_or_create_material("Mat_Tower_Timber", (0.36, 0.24, 0.15, 1.0), roughness=0.9)
    mat_roof = get_or_create_material("Mat_Tower_TinRoof", (0.48, 0.42, 0.35, 1.0), metallic=0.6, roughness=0.7)

    # 4 Upright heavy stilt posts (4.8m tall, 3m x 3m footprint)
    for px in [-1.4, 1.4]:
        for py in [-1.4, 1.4]:
            post = create_cylinder(f"Post_{px}_{py}", (px, py, 2.4), 0.10, 4.8, vertices=8, material=mat_timbers)
            parts.append(post)

    # Platform Deck at 3.5m height (3.2m x 3.2m)
    deck = create_box("DeckPlanks", (0, 0, 3.5), (3.2, 3.2, 0.14), mat_timbers)
    parts.append(deck)

    # Railing around platform
    for rx, ry, rdx, rdy in [
        (0, 1.55, 3.2, 0.1),
        (0, -1.55, 3.2, 0.1),
        (-1.55, 0, 0.1, 3.2),
        (1.55, 0, 0.1, 3.2)
    ]:
        rail = create_box(f"Railing_{rx}_{ry}", (rx, ry, 4.0), (rdx, rdy, 0.9), mat_timbers)
        parts.append(rail)

    # Slanted Tin Roof
    roof = create_box("TinRoof", (0, 0, 5.2), (3.6, 3.6, 0.06), mat_roof, rotation=(math.radians(8), 0, 0))
    parts.append(roof)

    final_mesh = join_objects(parts, "Base_Watchtower")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Base_Watchtower.fbx"))

def generate_water_collector():
    reset_scene()
    parts = []

    mat_barrel = get_or_create_material("Mat_Water_BarrelBlue", (0.16, 0.35, 0.58, 1.0), metallic=0.4, roughness=0.6)
    mat_pedestal = get_or_create_material("Mat_Water_PedestalWood", (0.42, 0.30, 0.18, 1.0), roughness=0.9)
    mat_funnel = get_or_create_material("Mat_Water_FunnelTin", (0.65, 0.68, 0.70, 1.0), metallic=0.85, roughness=0.35)
    mat_spigot = get_or_create_material("Mat_Water_BrassSpigot", (0.80, 0.65, 0.22, 1.0), metallic=0.9, roughness=0.3)

    # Wooden pedestal (0.45m high)
    pedestal = create_box("Pedestal", (0, 0, 0.225), (0.75, 0.75, 0.45), mat_pedestal)
    parts.append(pedestal)

    # 55-gallon Collector Drum
    drum = create_cylinder("WaterDrum", (0, 0, 0.95), 0.32, 0.95, vertices=16, material=mat_barrel)
    parts.append(drum)

    # Large top funnel collector
    funnel = create_cone("CollectorFunnel", (0, 0, 1.55), 0.48, 0.22, 0.28, vertices=16, material=mat_funnel)
    parts.append(funnel)

    # Brass spigot near bottom
    spigot = create_cylinder("Spigot", (0, 0.36, 0.55), 0.02, 0.12, vertices=8, material=mat_spigot, rotation=(math.radians(90), 0, 0))
    parts.append(spigot)

    final_mesh = join_objects(parts, "Base_WaterCollector")
    set_origin_to_bottom(final_mesh)
    export_fbx(os.path.join(MODELS_DIR, "Base_WaterCollector.fbx"))

if __name__ == "__main__":
    print("[BaseBuildingGenerator] Generating sanctuary & base building props...")
    generate_workbench()
    generate_campfire_cooker()
    generate_generator()
    generate_medical_cot()
    generate_watchtower()
    generate_water_collector()
    print("[BaseBuildingGenerator] Base building props generated successfully!")
