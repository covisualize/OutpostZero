"""Inventory icons rendered from an item's own triangles. Pure Python, so no GPU is needed.

The camera is orthographic at a fixed three-quarter view. Faces are flat-shaded with one key
light plus ambient in each material's base colour, supersampled, and outlined so the item reads
on a dark pack slot. Same triangles in, same bytes out, on any machine.
"""

import math

ICON_RENDER_SIZE = 64
SUPERSAMPLE = 4
YAW_DEGREES = 125.0
PITCH_DEGREES = 28.0
MARGIN = 0.1
AMBIENT = 0.38
LIGHT = (-0.45, -0.7, 0.55)
OUTLINE = (18, 18, 20)


def _normalize(v):
    length = math.sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2])
    if length <= 1e-12:
        return (0.0, 0.0, 0.0)
    return (v[0] / length, v[1] / length, v[2] / length)


def _cross(a, b):
    return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])


def _dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]


def to_srgb(linear):
    linear = 0.0 if linear < 0.0 else (1.0 if linear > 1.0 else linear)
    if linear <= 0.0031308:
        value = linear * 12.92
    else:
        value = 1.055 * (linear ** (1.0 / 2.4)) - 0.055
    return int(round(value * 255.0))


def view(point):
    """Blender space (Z up) to screen x, screen y (up), and depth (away), seen from above the +Y front."""
    yaw = math.radians(YAW_DEGREES)
    pitch = math.radians(PITCH_DEGREES)
    x, y, z = point
    rx = x * math.cos(yaw) - y * math.sin(yaw)
    ry = x * math.sin(yaw) + y * math.cos(yaw)
    sy = z * math.cos(pitch) + ry * math.sin(pitch)
    depth = ry * math.cos(pitch) - z * math.sin(pitch)
    return (rx, sy, depth)


def _quantize(point):
    return (round(point[0], 6), round(point[1], 6), round(point[2], 6))


def render(triangles, size=ICON_RENDER_SIZE, supersample=SUPERSAMPLE):
    """triangles: iterable of (v0, v1, v2, (r, g, b)) in Blender space with linear 0..1 colour.
    Returns size*size RGBA bytes, transparent where nothing was drawn."""
    light = _normalize(LIGHT)
    prepared = []
    for v0, v1, v2, colour in triangles:
        a, b, c = _quantize(v0), _quantize(v1), _quantize(v2)
        normal = _normalize(_cross((b[0] - a[0], b[1] - a[1], b[2] - a[2]), (c[0] - a[0], c[1] - a[1], c[2] - a[2])))
        if normal == (0.0, 0.0, 0.0):
            continue
        lambert = abs(_dot(normal, light))
        shade = AMBIENT + (1.0 - AMBIENT) * lambert
        rgb = tuple(to_srgb(channel * shade) for channel in colour[:3])
        prepared.append((view(a), view(b), view(c), rgb))

    out = bytearray(size * size * 4)
    if not prepared:
        return out

    xs = [p[0] for tri in prepared for p in tri[:3]]
    ys = [p[1] for tri in prepared for p in tri[:3]]
    span = max(max(xs) - min(xs), max(ys) - min(ys), 1e-6)
    cx = (max(xs) + min(xs)) * 0.5
    cy = (max(ys) + min(ys)) * 0.5
    big = size * supersample
    scale = big * (1.0 - 2.0 * MARGIN) / span

    depth = [float("inf")] * (big * big)
    colour = [None] * (big * big)
    for tri in prepared:
        pts = [((p[0] - cx) * scale + big * 0.5, big * 0.5 - (p[1] - cy) * scale, p[2]) for p in tri[:3]]
        (x0, y0, z0), (x1, y1, z1), (x2, y2, z2) = pts
        area = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0)
        if abs(area) < 1e-9:
            continue
        min_x = max(0, int(math.floor(min(x0, x1, x2))))
        max_x = min(big - 1, int(math.ceil(max(x0, x1, x2))))
        min_y = max(0, int(math.floor(min(y0, y1, y2))))
        max_y = min(big - 1, int(math.ceil(max(y0, y1, y2))))
        inv = 1.0 / area
        rgb = tri[3]
        for py in range(min_y, max_y + 1):
            fy = py + 0.5
            row = py * big
            for px in range(min_x, max_x + 1):
                fx = px + 0.5
                w0 = ((x1 - fx) * (y2 - fy) - (x2 - fx) * (y1 - fy)) * inv
                w1 = ((x2 - fx) * (y0 - fy) - (x0 - fx) * (y2 - fy)) * inv
                w2 = 1.0 - w0 - w1
                if w0 < 0.0 or w1 < 0.0 or w2 < 0.0:
                    continue
                z = w0 * z0 + w1 * z1 + w2 * z2
                index = row + px
                if z < depth[index]:
                    depth[index] = z
                    colour[index] = rgb

    for y in range(size):
        for x in range(size):
            r = g = b = hits = 0
            for sy in range(supersample):
                row = (y * supersample + sy) * big
                for sx in range(supersample):
                    sample = colour[row + x * supersample + sx]
                    if sample is None:
                        continue
                    r += sample[0]
                    g += sample[1]
                    b += sample[2]
                    hits += 1
            if hits == 0:
                continue
            index = (y * size + x) * 4
            out[index] = (r + hits // 2) // hits
            out[index + 1] = (g + hits // 2) // hits
            out[index + 2] = (b + hits // 2) // hits
            out[index + 3] = (hits * 255 + (supersample * supersample) // 2) // (supersample * supersample)
    return outline(out, size)


def outline(rgba, size):
    result = bytearray(rgba)
    for y in range(size):
        for x in range(size):
            index = (y * size + x) * 4
            if rgba[index + 3] != 0:
                continue
            near = 0
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < size and 0 <= ny < size:
                    near = max(near, rgba[(ny * size + nx) * 4 + 3])
            if near > 0:
                result[index:index + 4] = bytes((OUTLINE[0], OUTLINE[1], OUTLINE[2], min(255, near)))
    return result


def coverage(rgba):
    """Share of pixels with any alpha, for the audit's is-it-actually-a-picture check."""
    pixels = len(rgba) // 4
    if pixels == 0:
        return 0.0
    return sum(1 for i in range(pixels) if rgba[i * 4 + 3]) / float(pixels)


def mesh_triangles(objects):
    """Collects world-space triangles and base colours from Blender mesh objects (LOD0 only)."""
    triangles = []
    for obj in sorted(objects, key=lambda o: o.name):
        if obj.type != "MESH" or ("_LOD" in obj.name and not obj.name.endswith("_LOD0")):
            continue
        mesh = obj.data
        mesh.calc_loop_triangles()
        matrix = obj.matrix_world
        colours = []
        for slot in obj.material_slots:
            colours.append(base_colour(slot.material))
        for tri in mesh.loop_triangles:
            corners = [tuple(matrix @ mesh.vertices[i].co) for i in tri.vertices]
            tint = colours[tri.material_index] if tri.material_index < len(colours) else (0.6, 0.6, 0.6)
            triangles.append((corners[0], corners[1], corners[2], tint))
    return triangles


def base_colour(material):
    if material is None:
        return (0.6, 0.6, 0.6)
    if material.use_nodes and material.node_tree is not None:
        bsdf = material.node_tree.nodes.get("Principled BSDF")
        if bsdf is not None and "Base Color" in bsdf.inputs:
            value = bsdf.inputs["Base Color"].default_value
            return (float(value[0]), float(value[1]), float(value[2]))
    value = material.diffuse_color
    return (float(value[0]), float(value[1]), float(value[2]))
