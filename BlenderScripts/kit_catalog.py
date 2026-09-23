"""Snap-grid kit. Pieces, door openings, and building recipes. No Blender required."""

import json
import os

GRID = 2.0
DOOR_CLEAR = 1.2
SURFACES = ("concrete", "metal", "wood", "glass")


def repo_root():
    return os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))


def catalog_path(root=None):
    return os.path.join(root or repo_root(), "Assets", "Resources", "KitCatalog.json")


def box(x, y, z, w, h, d):
    return {
        "x": round(x, 3),
        "y": round(y, 3),
        "z": round(z, 3),
        "w": round(w, 3),
        "h": round(h, 3),
        "d": round(d, 3),
    }


def piece(piece_id, category, w, h, d, surface, colliders=None, door=0, lod=24, variants=None):
    if colliders is None:
        colliders = [box(0, 0, 0, w, h, d)]
    if variants is None:
        variants = ["brick", "concrete", "plaster"] if piece_id.startswith("wall") or piece_id in ("corner", "parapet") else [surface]
    return {
        "id": piece_id,
        "category": category,
        "w": w,
        "h": h,
        "d": d,
        "surface": surface,
        "door": door,
        "lod": lod,
        "variants": variants,
        "colliders": colliders,
    }


def framed_opening(piece_id, category, w, h, d, surface, clear, head, lod=24):
    jamb = (w - clear) / 2.0
    colliders = [
        box(0, 0, 0, jamb, h, d),
        box(w - jamb, 0, 0, jamb, h, d),
        box(jamb, head, 0, clear, h - head, d),
    ]
    return piece(piece_id, category, w, h, d, surface, colliders, door=clear, lod=lod)


def window_wall(piece_id, broken):
    width, height, depth = 2.0, 3.0, 0.2
    hole_w, hole_h, sill = 1.0, 1.2, 0.9
    left = (width - hole_w) / 2.0
    colliders = [
        box(0, 0, 0, left, height, depth),
        box(width - left, 0, 0, left, height, depth),
        box(left, 0, 0, hole_w, sill, depth),
        box(left, sill + hole_h, 0, hole_w, height - (sill + hole_h), depth),
    ]
    if not broken:
        colliders.append(box(left, sill, 0, hole_w, hole_h, depth))
    return piece(piece_id, "building", width, height, depth, "concrete", colliders, door=0)


def stair_boxes():
    colliders = []
    for step in range(12):
        colliders.append(box(0, step * 0.25, step * 0.34, 2.0, 0.25, 0.34))
    return colliders


def all_pieces():
    wall = (2.0, 3.0, 0.2)
    pieces = [
        piece("wall_plain", "building", *wall, "concrete"),
        window_wall("wall_window", broken=False),
        window_wall("wall_window_broken", broken=True),
        framed_opening("wall_door", "building", 2, 3, 0.2, "concrete", 1.2, 2.1),
        framed_opening("wall_double_door", "building", 2, 3, 0.2, "concrete", 1.6, 2.15),
        framed_opening("wall_garage", "building", 4, 3, 0.2, "concrete", 2.4, 2.4),
        piece("corner", "building", 0.2, 3, 0.2, "concrete"),
        piece("wall_half", "building", 2, 1.5, 0.2, "concrete"),
        piece("floor", "building", 2, 0.1, 2, "concrete", lod=32),
        piece("ceiling", "building", 2, 0.1, 2, "concrete", lod=32),
        piece("roof", "building", 2, 0.16, 2, "concrete", lod=40),
        piece("parapet", "building", 2, 0.4, 0.2, "concrete"),
        piece("stairs", "building", 2, 3, 4.08, "concrete", stair_boxes(), lod=20),
        piece("balcony", "building", 2, 1.1, 1.2, "metal", [
            box(0, 0, 0, 2, 0.1, 1.2),
            box(0, 0.1, 1.05, 2, 1.0, 0.08),
        ]),
        piece("fire_escape", "building", 1.2, 3, 2.2, "metal", [
            box(0, 0, 0, 1.2, 0.08, 2.2),
            box(0, 1.4, 0, 1.2, 0.08, 2.2),
            box(0, 0, 2.05, 1.2, 3, 0.08),
        ]),
        piece("wall_boarded", "building", *wall, "wood"),
        piece("road_straight", "road", 2, 0.08, 2, "concrete", lod=36),
        piece("road_curve", "road", 2, 0.08, 2, "concrete", lod=36),
        piece("road_t", "road", 2, 0.08, 2, "concrete", lod=36),
        piece("road_cross", "road", 2, 0.08, 2, "concrete", lod=36),
        piece("road_deadend", "road", 2, 0.08, 2, "concrete", lod=36),
        piece("sidewalk_straight", "road", 2, 0.15, 2, "concrete", lod=30),
        piece("sidewalk_corner", "road", 2, 0.15, 2, "concrete", lod=30),
        piece("curb_ramp", "road", 2, 0.15, 1, "concrete", [box(0, 0, 0, 2, 0.08, 1)]),
        piece("crosswalk", "road", 2, 0.02, 2, "concrete", lod=28),
        piece("parking", "road", 2, 0.08, 2, "concrete", lod=36),
        piece("alley", "road", 2, 0.08, 2, "concrete", lod=30),
        piece("manhole", "road", 1, 0.04, 1, "metal"),
        piece("drain", "road", 0.4, 0.08, 2, "metal"),
        piece("shelf", "interior", 0.9, 1.8, 0.4, "wood"),
        piece("counter", "interior", 1.6, 0.9, 0.6, "wood"),
        piece("fridge", "interior", 0.7, 1.8, 0.7, "metal"),
        piece("pallet", "interior", 1.2, 0.8, 1.0, "wood"),
        piece("desk", "interior", 1.4, 0.75, 0.7, "wood"),
        piece("chair", "interior", 0.5, 0.9, 0.5, "wood"),
        piece("filing", "interior", 0.5, 1.3, 0.6, "metal"),
        piece("vending", "interior", 0.9, 1.8, 0.7, "metal"),
        piece("hospital_bed", "interior", 2.0, 0.7, 0.9, "metal"),
        piece("lockers", "interior", 1.5, 1.9, 0.5, "metal"),
        piece("toilet", "interior", 0.8, 1.2, 1.2, "concrete", [
            box(0, 0, 0, 0.08, 1.2, 1.2),
            box(0.72, 0, 0, 0.08, 1.2, 1.2),
            box(0, 0, 1.12, 0.8, 1.2, 0.08),
            box(0.15, 0, 0.3, 0.5, 0.4, 0.6),
        ]),
        piece("workbench", "interior", 1.6, 0.9, 0.7, "wood"),
        piece("generator", "interior", 1.1, 0.8, 0.7, "metal"),
        piece("pipes", "interior", 2, 0.2, 0.2, "metal"),
        piece("ceiling_light", "interior", 0.4, 0.15, 0.8, "metal", lod=16),
        piece("sandbag_straight", "fort", 2, 0.6, 0.5, "concrete"),
        piece("sandbag_corner", "fort", 0.6, 0.6, 0.6, "concrete"),
        piece("wood_barricade", "fort", 2, 1.2, 0.2, "wood"),
        piece("wire", "fort", 2, 1.1, 0.4, "metal"),
        piece("jersey", "fort", 2, 0.8, 0.6, "concrete"),
        piece("fence", "fort", 2, 2, 0.08, "metal"),
        framed_opening("fence_gate", "fort", 2, 2, 0.08, "metal", 1.2, 1.9),
        piece("tower_segment", "fort", 2, 2, 2, "wood", [
            box(0, 0, 0, 0.15, 2, 0.15),
            box(1.85, 0, 0, 0.15, 2, 0.15),
            box(0, 0, 1.85, 0.15, 2, 0.15),
            box(1.85, 0, 1.85, 0.15, 2, 0.15),
            box(0, 1.85, 0, 2, 0.15, 2),
        ]),
        piece("spike", "fort", 2, 0.25, 0.4, "metal"),
        framed_opening("plywood_door", "fort", 2, 2.2, 0.08, "wood", 1.2, 2.0),
    ]
    return pieces


def at(piece_id, x, y, z, yaw=0):
    return {"id": piece_id, "x": round(x, 3), "y": round(y, 3), "z": round(z, 3), "yaw": int(yaw)}


def _slab(placements, piece_id, y, xs, zs, skip=()):
    for x in xs:
        for z in zs:
            if (x, z) in skip:
                continue
            placements.append(at(piece_id, x, y, z))


def _front_walk(placements, xs):
    for x in xs:
        placements.append(at("sidewalk_straight", x, 0, -2))


def storefront():
    placed = []
    _slab(placed, "floor", 0, (0, 2), (0, 2))
    placed.append(at("wall_plain", 0, 0, 0))
    placed.append(at("wall_door", 2, 0, 0))
    placed.append(at("wall_window", 0, 0, 3.8))
    placed.append(at("wall_window_broken", 2, 0, 3.8))
    placed.append(at("wall_plain", 0, 0, 0, 270))
    placed.append(at("wall_boarded", 0, 0, 2, 270))
    placed.append(at("wall_window", 4.2, 0, 2, 90))
    placed.append(at("wall_window", 4.2, 0, 4, 90))
    _slab(placed, "roof", 3, (0, 2), (0, 2))
    placed.append(at("shelf", 0.35, 0, 1.4))
    placed.append(at("counter", 2.1, 0, 2.8))
    placed.append(at("vending", 0.3, 0, 2.9))
    placed.append(at("ceiling_light", 1.6, 2.7, 1.6))
    _front_walk(placed, (0, 2))
    placed.append(at("crosswalk", 2, 0, -4))
    return placed


def warehouse():
    placed = []
    xs = (0, 2, 4, 6)
    zs = (0, 2, 4)
    _slab(placed, "floor", 0, xs, zs)
    placed.append(at("wall_plain", 0, 0, 0))
    placed.append(at("wall_garage", 2, 0, 0))
    placed.append(at("wall_plain", 6, 0, 0))
    for x in xs:
        placed.append(at("wall_plain", x, 0, 5.8))
    for z in zs:
        placed.append(at("wall_plain", 0, 0, z, 270))
        placed.append(at("wall_window", 8.2, 0, z + 2, 90))
    _slab(placed, "roof", 3, xs, zs)
    placed.append(at("pallet", 0.8, 0, 2.0))
    placed.append(at("pallet", 0.8, 0, 3.3))
    placed.append(at("generator", 5.4, 0, 4.0))
    placed.append(at("pipes", 4.2, 2.2, 4.6))
    placed.append(at("workbench", 5.0, 0, 1.2))
    _front_walk(placed, (2, 4))
    return placed


def hospital():
    placed = []
    _slab(placed, "floor", 0, (0, 2, 4), (0, 2, 4))
    placed.append(at("wall_door", 0, 0, 0))
    placed.append(at("wall_window", 2, 0, 0))
    placed.append(at("wall_plain", 4, 0, 0))
    for x in (0, 2, 4):
        placed.append(at("wall_window", x, 0, 5.8))
        placed.append(at("wall_plain", 0, 0, x, 270))
        placed.append(at("wall_plain", 6.2, 0, x + 2, 90))
    _slab(placed, "roof", 3, (0, 2, 4), (0, 2, 4))
    placed.append(at("hospital_bed", 0.4, 0, 2.2))
    placed.append(at("hospital_bed", 0.4, 0, 3.6))
    placed.append(at("lockers", 3.6, 0, 4.4))
    placed.append(at("filing", 4.4, 0, 1.2))
    placed.append(at("toilet", 4.2, 0, 2.6))
    placed.append(at("ceiling_light", 2.2, 2.7, 2.2))
    _front_walk(placed, (0, 2))
    return placed


def apartment():
    placed = []
    xs = (0, 2, 4)
    zs = (0, 2, 4)
    # The flight climbs from z 0.8 to 4.88 in the middle column; the slab above it opens from z 2 so
    # there is head room, and the top step lands sideways on the tiles either side.
    well = ((2, 2), (2, 4))
    for floor in range(3):
        y = floor * 3
        _slab(placed, "floor", y, xs, zs, well if floor > 0 else ())
        south = ("wall_door", "wall_plain", "wall_window") if floor == 0 else ("wall_plain", "wall_window", "wall_boarded")
        for index, x in enumerate(xs):
            placed.append(at(south[index], x, y, 0))
            placed.append(at("wall_window", x, y, 5.8))
            placed.append(at("wall_plain", 0, y, zs[index], 270))
            placed.append(at("wall_plain", 6.2, y, zs[index] + 2, 90))
        if floor < 2:
            placed.append(at("stairs", 2.0, y, 0.8))
        placed.append(at("fire_escape", -1.3, y, 2.0))
    _slab(placed, "roof", 9, xs, zs)
    for x in xs:
        placed.append(at("parapet", x, 9.16, 0))
    placed.append(at("shelf", 0.4, 0, 4.4))
    placed.append(at("desk", 0.4, 3, 4.2))
    placed.append(at("chair", 1.6, 3, 4.3))
    placed.append(at("hospital_bed", 4.0, 6, 4.0))
    placed.append(at("ceiling_light", 1.2, 2.7, 2.2))
    _front_walk(placed, (0, 2))
    return placed


def edge():
    placed = []
    x = -8.0
    while x <= 16.0:
        piece_id = "fence_gate" if abs(x) < 0.1 else "fence"
        placed.append(at(piece_id, x, 0, 22))
        x += GRID
    return placed


def recipe_for(district_id):
    if district_id == "rail_yard":
        return "warehouse"
    if district_id == "old_hospital":
        return "hospital"
    if district_id == "north_gate":
        return "apartment"
    return "storefront"


def catalog():
    return {
        "grid": GRID,
        "doorClear": DOOR_CLEAR,
        "anchor": {"x": 12, "y": 0, "z": 5},
        "pieces": all_pieces(),
        "storefront": storefront(),
        "warehouse": warehouse(),
        "hospital": hospital(),
        "apartment": apartment(),
        "edge": edge(),
    }


def by_id(pieces):
    return {item["id"]: item for item in pieces}


def clear_at(item, x, y, z):
    for collider in item["colliders"]:
        if collider["x"] <= x <= collider["x"] + collider["w"] and collider["y"] <= y <= collider["y"] + collider["h"] and collider["z"] <= z <= collider["z"] + collider["d"]:
            return False
    return True


def opening_width(item, y=1.0):
    samples = 80
    width = item["w"]
    best = run = 0
    for index in range(samples):
        x = (index + 0.5) * width / samples
        if clear_at(item, x, y, item["d"] * 0.5):
            run += 1
            if run > best:
                best = run
        else:
            run = 0
    return best * width / samples


def unknown_ids(pieces, placements):
    known = set(by_id(pieces))
    return sorted({item["id"] for item in placements if item["id"] not in known})


def storeys(placements):
    return sorted({item["y"] for item in placements if item["id"] == "floor"})


def write_catalog(root=None):
    path = catalog_path(root)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        json.dump(catalog(), handle, indent=2)
        handle.write("\n")
    return path


def fbx_relative(piece_id):
    return "Assets/Models/Kit/Kit_{0}.fbx".format(piece_id)


if __name__ == "__main__":
    destination = write_catalog()
    print("Wrote {0}".format(destination))
