import os
import sys

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.append(script_dir)

import generate_characters
import generate_weapons
import generate_architecture
import generate_props
import generate_base_building

def build_all():
    print("==================================================")
    print("      OUTPOST ZERO — 3D ASSET BUILD PIPELINE      ")
    print("==================================================")
    
    print("\n>>> Phase 1: Generating Characters...")
    generate_characters.generate_player_leader()
    generate_characters.generate_zombie_walker()
    generate_characters.generate_zombie_runner()
    generate_characters.generate_zombie_brute()
    generate_characters.generate_npc_merchant()
    generate_characters.generate_colonist()

    print("\n>>> Phase 2: Generating Weapons & Survival Loot...")
    generate_weapons.generate_pistol()
    generate_weapons.generate_shotgun()
    generate_weapons.generate_machete()
    generate_weapons.generate_assault_rifle()
    generate_weapons.generate_loot_ammo_boxes()
    generate_weapons.generate_medkit()
    generate_weapons.generate_scrap_pile()

    print("\n>>> Phase 3: Generating Architecture & Buildings...")
    generate_architecture.generate_road_straight()
    generate_architecture.generate_road_intersection()
    generate_architecture.generate_storefront_building()
    generate_architecture.generate_industrial_warehouse()
    generate_architecture.generate_ruin_wall_corner()

    print("\n>>> Phase 4: Generating Urban Clutter, Props & Vehicles...")
    generate_props.generate_jersey_barrier()
    generate_props.generate_wood_wire_barricade()
    generate_props.generate_sandbags()
    generate_props.generate_wrecked_sedan()
    generate_props.generate_apocalypse_truck()
    generate_props.generate_dumpster()
    generate_props.generate_barrels()
    generate_props.generate_crates()
    generate_props.generate_street_furniture()

    print("\n>>> Phase 5: Generating Sanctuary Base Building Modules...")
    generate_base_building.generate_workbench()
    generate_base_building.generate_campfire_cooker()
    generate_base_building.generate_generator()
    generate_base_building.generate_medical_cot()
    generate_base_building.generate_watchtower()
    generate_base_building.generate_water_collector()

    print("\n==================================================")
    print("  ALL 32 OUTPOST ZERO 3D ASSETS BUILT & EXPORTED  ")
    print("==================================================")

if __name__ == "__main__":
    build_all()
