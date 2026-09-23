"""Per-vertex ambient occlusion baked into a colour layer, the fallback AO the Outpost shaders read.

Each vertex casts a fixed fan of rays over its hemisphere against its own mesh and the floor (z = 0),
weighted by the cosine to the normal. The fan and the maths are plain Python so the bake is the same
on every machine; only the ray cast itself comes from Blender's BVH tree.
"""

import math

RAYS = 32
"""Directions in the fixed sphere fan; about half face any given normal."""
REACH = 0.6
"""Metres a ray travels before it counts as open sky."""
LIFT = 0.004
"""Metres a ray starts above its vertex, so it doesn't hit the faces it sits on."""
FLOOR = 0.0
"""World height of the ground every model stands on."""
DARKEST = 0.35
"""Visibility floor, so a fully enclosed vertex still takes some light."""
LAYER = "AO"


def fan(count=RAYS):
    """Unit directions spread evenly over the sphere (golden-angle spiral)."""
    golden = math.pi * (3.0 - math.sqrt(5.0))
    points = []
    for index in range(count):
        z = 1.0 - (index + 0.5) * 2.0 / count
        radius = math.sqrt(max(0.0, 1.0 - z * z))
        angle = golden * index
        points.append((math.cos(angle) * radius, math.sin(angle) * radius, z))
    return points


def floor_hit(origin, direction, reach=REACH, floor=FLOOR):
    """True when a ray from origin reaches the ground plane within reach."""
    if direction[2] >= -1e-9 or origin[2] < floor - 1e-6:
        return False
    distance = (origin[2] - floor) / -direction[2]
    return distance <= reach


def visibility(origin, normal, hits, directions=None, reach=REACH, floor=FLOOR):
    """0..1 share of the cosine-weighted hemisphere that reaches open air.

    hits(start, direction, reach) answers whether the mesh blocks that ray.
    """
    directions = directions if directions is not None else fan()
    length = math.sqrt(normal[0] ** 2 + normal[1] ** 2 + normal[2] ** 2)
    if length < 1e-9:
        return 1.0
    n = (normal[0] / length, normal[1] / length, normal[2] / length)
    start = (origin[0] + n[0] * LIFT, origin[1] + n[1] * LIFT, origin[2] + n[2] * LIFT)
    total = 0.0
    open_air = 0.0
    for d in directions:
        weight = d[0] * n[0] + d[1] * n[1] + d[2] * n[2]
        if weight <= 0.0:
            continue
        total += weight
        if floor_hit(start, d, reach, floor) or hits(start, d, reach):
            continue
        open_air += weight
    return open_air / total if total > 0.0 else 1.0


def shade(seen):
    """Visibility to the stored AO value, lifted off black."""
    seen = 0.0 if seen < 0.0 else 1.0 if seen > 1.0 else seen
    return DARKEST + (1.0 - DARKEST) * seen


def to_byte(value):
    value = 0.0 if value < 0.0 else 1.0 if value > 1.0 else value
    return int(round(value * 255.0))


def bake(obj):
    """Write the AO layer onto a mesh object in Blender, in world space, as the render colour."""
    import bmesh
    from mathutils.bvhtree import BVHTree

    mesh = obj.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.transform(obj.matrix_world)
    bm.normal_update()
    tree = BVHTree.FromBMesh(bm)

    def hits(start, direction, reach):
        location, _normal, _index, _distance = tree.ray_cast(start, direction, reach)
        return location is not None

    directions = fan()
    values = []
    bm.verts.ensure_lookup_table()
    for vert in bm.verts:
        co = vert.co
        normal = vert.normal
        seen = visibility((co.x, co.y, co.z), (normal.x, normal.y, normal.z), hits, directions)
        values.append(to_byte(shade(seen)) / 255.0)
    bm.free()

    existing = mesh.color_attributes.get(LAYER)
    if existing is not None:
        mesh.color_attributes.remove(existing)
    layer = mesh.color_attributes.new(name=LAYER, type="BYTE_COLOR", domain="POINT")
    for index, value in enumerate(values):
        layer.data[index].color_srgb = (value, value, value, 1.0)
    mesh.color_attributes.active_color = layer
    mesh.color_attributes.render_color_index = mesh.color_attributes.find(LAYER)
    return values
