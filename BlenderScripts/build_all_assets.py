import os
import sys
import traceback

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.append(script_dir)

from blender_paths import apply_cli_overrides


REQUIRED_BLENDER = (4, 2)


def require_blender_version():
    import bpy

    version = bpy.app.version[:2]
    if version < REQUIRED_BLENDER:
        raise SystemExit(
            "Outpost Zero assets require Blender {0}.{1} LTS or newer. Found {2}.".format(
                REQUIRED_BLENDER[0], REQUIRED_BLENDER[1], bpy.app.version_string
            )
        )
    print("[Blender] Using {0}".format(bpy.app.version_string))


def build_all(only=None):
    require_blender_version()

    import generate_architecture
    import generate_base_building
    import generate_characters
    import generate_kit
    import generate_props
    import generate_weapons

    phases = {
        "characters": [
            ("Survivor_Leader", generate_characters.generate_player_leader),
            ("Zombie_Walker", generate_characters.generate_zombie_walker),
            ("Zombie_Runner", generate_characters.generate_zombie_runner),
            ("Zombie_Brute", generate_characters.generate_zombie_brute),
            ("NPC_Merchant", generate_characters.generate_npc_merchant),
            ("Colonist_Survivor", generate_characters.generate_colonist),
        ],
        "weapons": [
            ("Weapon_Pistol_9mm", generate_weapons.generate_pistol),
            ("Weapon_Shotgun_Pump", generate_weapons.generate_shotgun),
            ("Weapon_Machete", generate_weapons.generate_machete),
            ("Weapon_AssaultRifle", generate_weapons.generate_assault_rifle),
            ("Loot_AmmoBoxes", generate_weapons.generate_loot_ammo_boxes),
            ("Loot_Medkit", generate_weapons.generate_medkit),
            ("Loot_ScrapPile", generate_weapons.generate_scrap_pile),
        ],
        "architecture": [
            ("Road_Tile_Straight", generate_architecture.generate_road_straight),
            ("Road_Tile_Intersection", generate_architecture.generate_road_intersection),
            ("Building_Storefront_2Story", generate_architecture.generate_storefront_building),
            ("Building_Warehouse_Depot", generate_architecture.generate_industrial_warehouse),
            ("Ruin_Wall_Corner", generate_architecture.generate_ruin_wall_corner),
        ],
        "props": [
            ("Barricade_Concrete_Jersey", generate_props.generate_jersey_barrier),
            ("Barricade_Wood_Wire", generate_props.generate_wood_wire_barricade),
            ("Barricade_Sandbags", generate_props.generate_sandbags),
            ("Vehicle_Wrecked_Sedan", generate_props.generate_wrecked_sedan),
            ("Vehicle_Apocalypse_Truck", generate_props.generate_apocalypse_truck),
            ("Prop_Dumpster", generate_props.generate_dumpster),
            ("Prop_Barrels", generate_props.generate_barrels),
            ("Prop_Crates", generate_props.generate_crates),
            ("Prop_StreetFurniture", generate_props.generate_street_furniture),
        ],
        "kit": [
            ("SnapKit", generate_kit.generate_all),
        ],
        "base": [
            ("Base_CraftingWorkbench", generate_base_building.generate_workbench),
            ("Base_Campfire_Cooker", generate_base_building.generate_campfire_cooker),
            ("Base_Generator_Diesel", generate_base_building.generate_generator),
            ("Base_MedicalCot", generate_base_building.generate_medical_cot),
            ("Base_Watchtower", generate_base_building.generate_watchtower),
            ("Base_WaterCollector", generate_base_building.generate_water_collector),
        ],
    }

    selected = only or list(phases.keys())
    unknown = [name for name in selected if name not in phases]
    if unknown:
        raise SystemExit("Unknown --only categories: {0}. Choose from {1}.".format(
            ", ".join(unknown), ", ".join(phases.keys())
        ))

    print("==================================================")
    print("      OUTPOST ZERO — 3D ASSET BUILD PIPELINE      ")
    print("==================================================")

    built = []
    failures = []
    for category in selected:
        print("\n>>> {0}".format(category))
        for name, builder in phases[category]:
            try:
                builder()
                built.append("{0}/{1}".format(category, name))
                print("  ok  {0}".format(name))
            except Exception:
                failures.append(name)
                print("  FAIL {0}".format(name))
                traceback.print_exc()

    print("\n==================================================")
    print("  Built {0} step(s). Failed {1}.".format(len(built), len(failures)))
    print("==================================================")
    if failures:
        raise SystemExit(1)


if __name__ == "__main__":
    categories = apply_cli_overrides()
    build_all(categories)
