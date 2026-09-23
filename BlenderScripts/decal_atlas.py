"""Procedural decal atlas for the URP decal projectors (PRO-38).

One 1024 x 512 RGBA sheet of 128 px cells, drawn from value noise and Voronoi cells with no
Blender or GPU, so the PNG is byte-identical on every machine. The cell order is mirrored by
Assets/Scripts/Graphics/DecalAtlas.cs; a test keeps the two tables matched.

    python3 BlenderScripts/decal_atlas.py           # write the atlas and its .meta
    python3 BlenderScripts/decal_atlas.py --check   # exit 1 if the committed atlas is stale
"""

import hashlib
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from texture_set import encode_png, meta_text as _texture_meta  # noqa: E402

CELL = 128
COLUMNS = 8
ROWS = 4
PADDING = 3
OUTPUT = "Assets/Textures/Decals/DecalAtlas.png"

LAYOUT = (
    ("blood", 8),
    ("drip", 4),
    ("hole_concrete", 3),
    ("hole_metal", 3),
    ("hole_wood", 3),
    ("scorch", 2),
    ("oil", 2),
    ("footprint", 2),
)


def cells():
    """(kind, variant, index) for every cell, in atlas order."""
    out = []
    index = 0
    for kind, count in LAYOUT:
        for variant in range(count):
            out.append((kind, variant, index))
            index += 1
    return out


def first(kind):
    index = 0
    for name, count in LAYOUT:
        if name == kind:
            return index
        index += count
    raise KeyError(kind)


def cell_origin(index):
    """Top-left pixel of a cell; row 0 is the top of the PNG."""
    return (index % COLUMNS) * CELL, (index // COLUMNS) * CELL


def _hash(ix, iy, seed):
    h = (ix * 374761393 + iy * 668265263 + seed * 2246822519) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    h ^= h >> 16
    return (h & 0xFFFFFF) / float(0x1000000)


def _smooth(t):
    return t * t * (3.0 - 2.0 * t)


def value_noise(x, y, seed):
    ix = math.floor(x)
    iy = math.floor(y)
    fx = _smooth(x - ix)
    fy = _smooth(y - iy)
    a = _hash(ix, iy, seed)
    b = _hash(ix + 1, iy, seed)
    c = _hash(ix, iy + 1, seed)
    d = _hash(ix + 1, iy + 1, seed)
    top = a + (b - a) * fx
    bottom = c + (d - c) * fx
    return top + (bottom - top) * fy


def fbm(x, y, seed, octaves=3):
    total = 0.0
    amplitude = 0.5
    norm = 0.0
    for octave in range(octaves):
        total += value_noise(x, y, seed + octave * 31) * amplitude
        norm += amplitude
        x *= 2.03
        y *= 2.03
        amplitude *= 0.5
    return total / norm


def voronoi(x, y, seed):
    """Distance to the nearest feature point and that cell's own random value."""
    ix = math.floor(x)
    iy = math.floor(y)
    best = 9.0
    owner = 0.0
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            cx = ix + ox
            cy = iy + oy
            px = cx + _hash(cx, cy, seed)
            py = cy + _hash(cx, cy, seed + 7)
            d = (px - x) * (px - x) + (py - y) * (py - y)
            if d < best:
                best = d
                owner = _hash(cx, cy, seed + 13)
    return math.sqrt(best), owner


def _step(edge0, edge1, x):
    if edge0 == edge1:
        return 1.0 if x >= edge1 else 0.0
    t = (x - edge0) / (edge1 - edge0)
    t = 0.0 if t < 0.0 else 1.0 if t > 1.0 else t
    return _smooth(t)


def _mix(a, b, t):
    return (a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t)


BLOOD = (0.36, 0.03, 0.025)
BLOOD_WET = (0.2, 0.01, 0.01)


def blood(u, v, seed):
    r = math.hypot(u, v)
    angle = math.atan2(v, u)
    edge = 0.55 + 0.3 * (fbm(math.cos(angle) * 1.6 + seed, math.sin(angle) * 1.6, seed) - 0.5) * 2.0
    body = 1.0 - _step(edge - 0.05, edge, r)
    d, owner = voronoi(u * 5.5 + seed * 0.37, v * 5.5, seed + 3)
    drop = 0.0
    if owner < 0.4 and r < 0.94 and r > edge * 0.8:
        drop = 1.0 - _step(0.1 + owner * 0.12, 0.14 + owner * 0.12, d)
    alpha = max(body, drop)
    grain = fbm(u * 6.0, v * 6.0, seed + 5)
    colour = _mix(BLOOD, BLOOD_WET, _step(0.1, 0.6, 1.0 - r) * 0.8 + (grain - 0.5) * 0.4)
    return colour, alpha * (0.82 + 0.18 * grain)


def drip(u, v, seed):
    top = 1.0 - _step(0.2, 0.26, math.hypot(u * 0.7, (v + 0.72) * 1.4) - 0.12 * fbm(u * 3.0, v * 3.0, seed))
    alpha = top
    for streak in range(5):
        x = (_hash(streak, seed, 41) - 0.5) * 1.1
        length = 0.55 + _hash(streak, seed, 43) * 1.1
        width = 0.035 + _hash(streak, seed, 47) * 0.04
        end = -0.72 + length
        if v < -0.75 or v > end + width * 2.0:
            continue
        t = (v + 0.75) / max(length, 0.01)
        w = width * (1.0 - 0.45 * min(t, 1.0))
        wobble = (value_noise(v * 4.0, streak, seed) - 0.5) * 0.05
        line = 1.0 - _step(w * 0.7, w, abs(u - x - wobble)) if v <= end else 0.0
        bulb = 1.0 - _step(width * 1.1, width * 1.5, math.hypot(u - x - wobble, v - end))
        alpha = max(alpha, line, bulb)
    colour = _mix(BLOOD, BLOOD_WET, 0.3 + 0.4 * fbm(u * 5.0, v * 5.0, seed + 9))
    return colour, alpha * 0.9


def hole(u, v, seed, material):
    r = math.hypot(u, v)
    angle = math.atan2(v, u)
    jag = (fbm(math.cos(angle) * 2.5 + seed, math.sin(angle) * 2.5, seed) - 0.5) * 0.08
    core = 1.0 - _step(0.1 + jag, 0.13 + jag, r)
    black = (0.04, 0.035, 0.03)
    if material == "metal":
        rim = (1.0 - _step(0.17, 0.2, r)) * _step(0.1, 0.13, r)
        scuff = (1.0 - _step(0.22, 0.42 + jag * 2.0, r)) * 0.45
        colour = black if core > 0.5 else (0.78, 0.74, 0.68) if rim > 0.3 else (0.16, 0.12, 0.1)
        return colour, max(core, rim, scuff)
    if material == "wood":
        splinter = 0.0
        for k in range(4):
            lean = (_hash(k, seed, 61) - 0.5) * 0.5
            reach = 0.35 + _hash(k, seed, 67) * 0.4
            side = 1.0 if k % 2 == 0 else -1.0
            along = u * side
            if 0.0 < along < reach:
                width = 0.1 * (1.0 - along / reach)
                splinter = max(splinter, 1.0 - _step(width * 0.6, width, abs(v - along * lean)))
        colour = black if core > 0.5 else (0.66, 0.5, 0.3)
        return colour, max(core, splinter * 0.9)
    crater = 1.0 - _step(0.26 + jag * 2.0, 0.32 + jag * 2.0, r)
    d, owner = voronoi(u * 7.0, v * 7.0, seed + 17)
    chips = crater * (0.55 + 0.45 * _step(0.05, 0.25, d))
    crack = 0.0
    for k in range(5):
        a = _hash(k, seed, 71) * math.tau
        reach = 0.45 + _hash(k, seed, 73) * 0.4
        along = u * math.cos(a) + v * math.sin(a)
        across = -u * math.sin(a) + v * math.cos(a)
        if 0.1 < along < reach:
            wobble = (value_noise(along * 6.0, k, seed) - 0.5) * 0.06
            crack = max(crack, (1.0 - _step(0.006, 0.016, abs(across - wobble))) * (1.0 - along / reach))
    grey = 0.36 + 0.12 * owner
    colour = black if core > 0.5 else (grey, grey * 0.96, grey * 0.9) if chips > crack else (0.12, 0.11, 0.1)
    return colour, max(core, chips * 0.85, crack * 0.8)


def scorch(u, v, seed):
    warp = (fbm(u * 2.2 + seed, v * 2.2, seed) - 0.5) * 0.5
    r = math.hypot(u, v) + warp
    angle = math.atan2(v, u)
    rays = value_noise(angle * 4.0 + seed, 0.0, seed + 3)
    soot = 1.0 - _step(0.35 + rays * 0.3, 0.92, r)
    grain = fbm(u * 8.0, v * 8.0, seed + 11)
    char = 1.0 - _step(0.05, 0.4, r)
    colour = _mix((0.13, 0.1, 0.08), (0.03, 0.025, 0.02), char * 0.8 + grain * 0.2)
    return colour, soot * (0.6 + 0.35 * grain)


def oil(u, v, seed):
    angle = math.atan2(v, u)
    edge = 0.62 + 0.3 * (fbm(math.cos(angle) * 1.2 + seed, math.sin(angle) * 1.2, seed) - 0.5) * 2.0
    r = math.hypot(u * (1.0 + 0.2 * (seed % 2)), v)
    body = 1.0 - _step(edge - 0.08, edge, r)
    sheen = fbm(u * 3.0, v * 3.0, seed + 19)
    tint = (0.05 + 0.02 * math.sin(sheen * 9.0), 0.045 + 0.015 * math.sin(sheen * 9.0 + 2.1), 0.05 + 0.025 * math.sin(sheen * 9.0 + 4.2))
    return tint, body * 0.88


def footprint(u, v, seed):
    if seed % 2 == 1:
        u = -u
    u += 0.04 * v
    sole = math.hypot((u - 0.02) / 0.3, (v + 0.28) / 0.5)
    heel = math.hypot(u / 0.25, (v - 0.58) / 0.26)
    shape = max(1.0 - _step(0.9, 1.0, sole), 1.0 - _step(0.9, 1.0, heel))
    tread = 0.72 + 0.28 * _step(-0.2, 0.2, math.sin(v * 38.0 + math.sin(u * 12.0)))
    patchy = _step(0.25, 0.55, fbm(u * 4.0, v * 4.0, seed + 23))
    colour = _mix(BLOOD, (0.22, 0.05, 0.03), 0.5)
    return colour, shape * tread * patchy * 0.85


def shade(kind, variant, u, v):
    seed = variant + 1 + first(kind) * 97
    if kind == "blood":
        return blood(u, v, seed)
    if kind == "drip":
        return drip(u, v, seed)
    if kind.startswith("hole_"):
        return hole(u, v, seed, kind[5:])
    if kind == "scorch":
        return scorch(u, v, seed)
    if kind == "oil":
        return oil(u, v, seed)
    if kind == "footprint":
        return footprint(u, v, variant)
    raise KeyError(kind)


def _byte(value):
    value = 0.0 if value < 0.0 else 1.0 if value > 1.0 else value
    return int(value * 255.0 + 0.5)


def render_cell(kind, variant):
    """CELL x CELL RGBA bytes for one cell, transparent at its border."""
    pixels = bytearray(CELL * CELL * 4)
    inner = CELL - 2 * PADDING
    for y in range(CELL):
        for x in range(CELL):
            if x < PADDING or y < PADDING or x >= CELL - PADDING or y >= CELL - PADDING:
                continue
            u = ((x - PADDING + 0.5) / inner) * 2.0 - 1.0
            v = ((y - PADDING + 0.5) / inner) * 2.0 - 1.0
            colour, alpha = shade(kind, variant, u, v)
            edge = max(abs(u), abs(v))
            alpha *= 1.0 - _step(0.9, 1.0, edge)
            if alpha <= 0.002:
                continue
            at = (y * CELL + x) * 4
            pixels[at] = _byte(colour[0])
            pixels[at + 1] = _byte(colour[1])
            pixels[at + 2] = _byte(colour[2])
            pixels[at + 3] = _byte(alpha)
    return pixels


def render():
    width = CELL * COLUMNS
    height = CELL * ROWS
    atlas = bytearray(width * height * 4)
    for kind, variant, index in cells():
        pixels = render_cell(kind, variant)
        ox, oy = cell_origin(index)
        for y in range(CELL):
            start = ((oy + y) * width + ox) * 4
            atlas[start:start + CELL * 4] = pixels[y * CELL * 4:(y + 1) * CELL * 4]
    return width, height, atlas


def png_bytes():
    width, height, atlas = render()
    return encode_png(width, height, atlas)


def guid():
    return hashlib.md5(OUTPUT.encode("utf-8")).hexdigest()


def meta_text():
    """Colour texture with straight alpha, clamped so neighbouring cells never bleed in."""
    text = _texture_meta(OUTPUT, "Albedo")
    lines = []
    for line in text.split("\n"):
        if line.startswith("guid: "):
            line = "guid: " + guid()
        elif line.strip() == "alphaIsTransparency: 0":
            line = line.replace("0", "1")
        elif line.strip() == "maxTextureSize: 256":
            line = line.replace("256", "1024")
        elif line.strip() in ("wrapU: 0", "wrapV: 0", "wrapW: 0"):
            line = line.replace("0", "1")
        lines.append(line)
    return "\n".join(lines)


def repo_root():
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def write(root=None):
    root = root or repo_root()
    path = os.path.join(root, OUTPUT)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, "wb") as handle:
        handle.write(png_bytes())
    with open(path + ".meta", "w", newline="\n") as handle:
        handle.write(meta_text())
    return path


def stale(root=None):
    root = root or repo_root()
    path = os.path.join(root, OUTPUT)
    if not os.path.exists(path) or not os.path.exists(path + ".meta"):
        return True
    with open(path, "rb") as handle:
        if handle.read() != png_bytes():
            return True
    with open(path + ".meta", "r") as handle:
        return handle.read() != meta_text()


if __name__ == "__main__":
    if "--check" in sys.argv:
        if stale():
            print("decal atlas is stale: run python3 BlenderScripts/decal_atlas.py")
            sys.exit(1)
        print("decal atlas is current")
        sys.exit(0)
    print("wrote " + write())
