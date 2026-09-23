"""Procedural PBR material library: tiling albedo, normal and mask maps plus URP materials.

Each family is drawn from periodic integer-hash noise, so every map tiles seamlessly and the
pixels come out the same on any machine (only zlib's byte stream can differ, so the staleness
check compares decoded pixels). Run with plain Python 3 and numpy:

    python3 BlenderScripts/material_library.py          # write everything
    python3 BlenderScripts/material_library.py --check  # exit 1 if the committed set is stale

Outputs, per family ``<Family>``:

- ``Assets/Materials/Library/Textures/<Family>_Albedo.png`` (sRGB; RGBA for glass)
- ``Assets/Materials/Library/Textures/<Family>_Normal.png`` (OpenGL, tangent space)
- ``Assets/Materials/Library/Textures/<Family>_Mask.png``   (R metallic, G occlusion, B 0,
  A smoothness: URP/Lit reads R and A from ``_MetallicGlossMap`` and G from ``_OcclusionMap``)
- ``Assets/Materials/Library/ML_<Family>.mat``  URP/Lit (UV mapped; imports and props)
- ``Assets/Materials/Library/MT_<Family>.mat``  OutpostZero/EnvironmentTriplanar (world UVs;
  the primitive-built architecture), except glass
- ``Assets/Resources/MaterialLibrary.asset`` listing both sets for runtime lookup
"""

import hashlib
import os
import struct
import sys
import zlib

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from texture_set import meta_text as _texture_meta  # noqa: E402

SIZE = 1024
TEXTURE_DIR = "Assets/Materials/Library/Textures"
MATERIAL_DIR = "Assets/Materials/Library"
LIBRARY_ASSET = "Assets/Resources/MaterialLibrary.asset"
LIT_SHADER_GUID = "933532a4fcc9baf4fa0491de14d08ed7"
TRIPLANAR_SHADER = "Assets/Shaders/OutpostEnvironment.shader"
LIBRARY_SCRIPT = "Assets/Scripts/Graphics/MaterialLibrary.cs"

# Order is the SurfaceFamily enum order in MaterialLibrary.cs (index + 1).
FAMILIES = (
    "Asphalt",
    "ConcreteCracked",
    "BrickRed",
    "BrickGrey",
    "MetalRusted",
    "MetalPainted",
    "Plywood",
    "TarpFabric",
    "Glass",
    "Rubber",
    "RotFlesh",
    "Cloth",
)

# Metres one texture tile covers on the triplanar material.
TILE_METRES = {
    "Asphalt": 4.0,
    "ConcreteCracked": 3.0,
    "BrickRed": 2.0,
    "BrickGrey": 2.0,
    "MetalRusted": 2.0,
    "MetalPainted": 2.0,
    "Plywood": 2.4,
    "TarpFabric": 1.5,
    "Glass": 2.0,
    "Rubber": 1.0,
    "RotFlesh": 1.0,
    "Cloth": 0.8,
}

MASK32 = np.uint64(0xFFFFFFFF)


# ---------------------------------------------------------------- noise

def _hash(ix, iy, seed):
    """Integer hash of lattice points to [0, 1). Pure uint arithmetic, so it is exact everywhere."""
    h = (ix.astype(np.uint64) * np.uint64(374761393)
         + iy.astype(np.uint64) * np.uint64(668265263)
         + np.uint64(seed) * np.uint64(2246822519)) & MASK32
    h = ((h ^ (h >> np.uint64(13))) * np.uint64(1274126177)) & MASK32
    h = h ^ (h >> np.uint64(16))
    return h.astype(np.float64) / 4294967296.0


def grid(size=SIZE):
    """Pixel-centre UVs in [0, 1): u across, v down the image."""
    axis = (np.arange(size, dtype=np.float64) + 0.5) / size
    return np.meshgrid(axis, axis)


def value_noise(u, v, freq, seed, freq_y=None):
    """Periodic over [0, 1); freq_y stretches the lattice along v and keeps the tile seamless."""
    freq_y = freq if freq_y is None else freq_y
    x = u * freq
    y = v * freq_y
    ix = np.floor(x).astype(np.int64)
    iy = np.floor(y).astype(np.int64)
    fx = x - ix
    fy = y - iy
    fx = fx * fx * (3.0 - 2.0 * fx)
    fy = fy * fy * (3.0 - 2.0 * fy)
    x0 = ix % freq
    y0 = iy % freq_y
    x1 = (ix + 1) % freq
    y1 = (iy + 1) % freq_y
    a = _hash(x0, y0, seed)
    b = _hash(x1, y0, seed)
    c = _hash(x0, y1, seed)
    d = _hash(x1, y1, seed)
    return (a + (b - a) * fx) + ((c + (d - c) * fx) - (a + (b - a) * fx)) * fy


def fbm(u, v, freq, seed, octaves=4, gain=0.5, freq_y=None):
    freq_y = freq if freq_y is None else freq_y
    total = np.zeros_like(u)
    amp = 1.0
    norm = 0.0
    for octave in range(octaves):
        total += value_noise(u, v, freq << octave, seed + octave * 101, freq_y << octave) * amp
        norm += amp
        amp *= gain
    return total / norm


def voronoi(u, v, freq, seed):
    """Periodic Worley noise: distance to nearest and second-nearest point, and the nearest cell's id hash."""
    x = u * freq
    y = v * freq
    ix = np.floor(x).astype(np.int64)
    iy = np.floor(y).astype(np.int64)
    f1 = np.full(u.shape, 9.0)
    f2 = np.full(u.shape, 9.0)
    cell = np.zeros(u.shape)
    for oy in (-1, 0, 1):
        for ox in (-1, 0, 1):
            cx = ix + ox
            cy = iy + oy
            wx = cx % freq
            wy = cy % freq
            px = cx + _hash(wx, wy, seed)
            py = cy + _hash(wx, wy, seed + 17)
            d = np.sqrt((px - x) ** 2 + (py - y) ** 2)
            closer = d < f1
            f2 = np.where(closer, f1, np.minimum(f2, d))
            cell = np.where(closer, _hash(wx, wy, seed + 29), cell)
            f1 = np.where(closer, d, f1)
    return f1, f2, cell


def _step(edge0, edge1, x):
    t = np.clip((x - edge0) / (edge1 - edge0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def _mix(a, b, t):
    t = t[..., None] if np.ndim(t) == 2 else t
    return np.asarray(a) * (1.0 - t) + np.asarray(b) * t


def _rgb(r, g, b, shape):
    out = np.empty(shape + (3,))
    out[..., 0] = r
    out[..., 1] = g
    out[..., 2] = b
    return out


def _blur(field, radius):
    out = np.zeros_like(field)
    count = 0
    for dy in range(-radius, radius + 1, max(1, radius // 2)):
        for dx in range(-radius, radius + 1, max(1, radius // 2)):
            out += np.roll(np.roll(field, dy, axis=0), dx, axis=1)
            count += 1
    return out / count


def crack_lines(u, v, freq, seed, width):
    f1, f2, _cell = voronoi(u, v, freq, seed)
    wobble = fbm(u, v, freq * 4, seed + 3, 3) * 0.08
    edge = f2 - f1 + wobble
    return 1.0 - _step(width * 0.4, width, edge)


# ---------------------------------------------------------------- families

def asphalt(u, v):
    grain = value_noise(u, v, 128, 11) * 0.7 + value_noise(u, v, 256, 12) * 0.3
    patches = fbm(u, v, 4, 13, 4)
    speck = (value_noise(u, v, 192, 14) > 0.86).astype(np.float64)
    cracks = crack_lines(u, v, 3, 15, 0.05) * _step(0.45, 0.7, fbm(u, v, 2, 16, 3))
    tone = 0.13 + patches * 0.07 + grain * 0.04 + speck * 0.1 - cracks * 0.08
    albedo = _rgb(tone, tone * 0.99, tone * 1.02, u.shape)
    height = grain * 0.35 + speck * 0.2 - cracks * 0.8 + patches * 0.2
    return albedo, height, 0.0, 0.18 + patches * 0.14, None


def concrete_cracked(u, v):
    mottle = fbm(u, v, 4, 21, 5)
    pores = (value_noise(u, v, 160, 22) > 0.9).astype(np.float64)
    cracks = crack_lines(u, v, 2, 23, 0.035) * _step(0.35, 0.6, fbm(u, v, 3, 24, 3))
    stain = _step(0.55, 0.8, fbm(u, v, 3, 25, 4))
    tone = 0.46 + mottle * 0.16 - pores * 0.12 - cracks * 0.28 - stain * 0.1
    albedo = _rgb(tone, tone * 0.98, tone * 0.94, u.shape)
    height = mottle * 0.3 - pores * 0.35 - cracks
    return albedo, height, 0.0, 0.22 + mottle * 0.1, None


def _bricks(u, v, seed, base, spread):
    rows = 16
    cols = 4
    y = v * rows
    row = np.floor(y).astype(np.int64)
    x = u * cols + (row % 2) * 0.5
    col = np.floor(x).astype(np.int64) % cols
    fx = x - np.floor(x)
    fy = y - row
    mortar_w = 0.045
    edge = np.minimum(np.minimum(fx, 1.0 - fx) * 2.0, np.minimum(fy, 1.0 - fy) * 0.5 * 2.0)
    mortar = 1.0 - _step(mortar_w * 0.5, mortar_w, edge)
    tint = _hash(col, row % rows, seed)
    grit = fbm(u, v, 64, seed + 1, 3)
    chip = _step(0.72, 0.8, fbm(u, v, 16, seed + 2, 4)) * (1.0 - mortar)
    brick = np.asarray(base) + (tint[..., None] - 0.5) * np.asarray(spread) + (grit[..., None] - 0.5) * 0.08
    mortar_col = _rgb(0.52, 0.5, 0.46, u.shape) + (grit[..., None] - 0.5) * 0.05
    albedo = _mix(brick, mortar_col, mortar)
    albedo = albedo * (1.0 - chip[..., None] * 0.25)
    height = (1.0 - mortar) * (0.7 + grit * 0.3) - chip * 0.3
    return albedo, height, 0.0, 0.2 + grit * 0.08, None


def brick_red(u, v):
    return _bricks(u, v, 31, (0.46, 0.18, 0.12), (0.14, 0.06, 0.04))


def brick_grey(u, v):
    return _bricks(u, v, 37, (0.38, 0.37, 0.36), (0.1, 0.1, 0.1))


def metal_rusted(u, v):
    rust_field = fbm(u, v, 4, 41, 5)
    rust = _step(0.42, 0.58, rust_field)
    pits = (value_noise(u, v, 128, 42) > 0.8).astype(np.float64) * rust
    streak = fbm(u, v, 4, 43, 3, freq_y=16)
    steel = _rgb(0.52, 0.53, 0.55, u.shape) + (streak[..., None] - 0.5) * 0.1
    rust_col = _mix(_rgb(0.42, 0.2, 0.08, u.shape), _rgb(0.28, 0.12, 0.05, u.shape), fbm(u, v, 32, 44, 3))
    albedo = _mix(steel, rust_col, rust)
    height = rust * 0.4 + rust_field * 0.3 - pits * 0.3
    metallic = 1.0 - rust * 0.95
    smooth = 0.72 * (1.0 - rust) + 0.12 * rust + (streak - 0.5) * 0.1
    return albedo, height, metallic, smooth, None


def metal_painted(u, v):
    panel_v = v * 2.0
    seam = 1.0 - _step(0.004, 0.012, np.abs(panel_v - np.round(panel_v)) / 2.0)
    chips = _step(0.66, 0.7, fbm(u, v, 8, 51, 5))
    grime = fbm(u, v, 3, 52, 4)
    paint = _rgb(0.22, 0.34, 0.26, u.shape) * (0.85 + grime[..., None] * 0.3)
    bare = _rgb(0.55, 0.55, 0.56, u.shape)
    albedo = _mix(paint, bare, chips) * (1.0 - seam[..., None] * 0.5)
    height = (1.0 - chips) * 0.3 - seam * 0.6 + grime * 0.1
    metallic = chips
    smooth = 0.42 * (1.0 - chips) + 0.68 * chips
    return albedo, height, metallic, smooth, None


def plywood(u, v):
    warp = fbm(u, v, 4, 61, 4) * 0.35
    rings = (v * 24.0 + warp * 6.0) % 1.0
    grain = _step(0.0, 0.5, rings) * (1.0 - _step(0.5, 1.0, rings))
    fibre = value_noise(u, v, 32, 62, freq_y=256)
    knots = 1.0 - _step(0.0, 0.03, voronoi(u, v, 3, 63)[0] * 0.2)
    base = _rgb(0.62, 0.46, 0.28, u.shape)
    dark = _rgb(0.46, 0.31, 0.17, u.shape)
    albedo = _mix(base, dark, grain * 0.6 + knots * 0.8) * (0.92 + fibre[..., None] * 0.16)
    height = grain * 0.25 + fibre * 0.15 - knots * 0.3
    return albedo, height, 0.0, 0.3 + fibre * 0.08, None


def _weave(u, v, threads, seed):
    x = u * threads
    y = v * threads
    ix = np.floor(x).astype(np.int64)
    iy = np.floor(y).astype(np.int64)
    over = ((ix + iy) % 2 == 0)
    fx = x - ix
    fy = y - iy
    bump = np.where(over, 1.0 - np.abs(fy - 0.5) * 2.0, 1.0 - np.abs(fx - 0.5) * 2.0)
    fleck = value_noise(u, v, threads, seed)
    return bump, fleck


def tarp_fabric(u, v):
    bump, fleck = _weave(u, v, 128, 71)
    wrinkle = fbm(u, v, 4, 72, 4)
    fade = fbm(u, v, 2, 73, 3)
    albedo = _rgb(0.1, 0.24, 0.48, u.shape) * (0.8 + fade[..., None] * 0.35) * (0.94 + fleck[..., None] * 0.1)
    height = bump * 0.2 + wrinkle * 0.8
    return albedo, height, 0.0, 0.38 + fade * 0.1, None


def glass(u, v):
    smudge = fbm(u, v, 4, 81, 5)
    dirt = _step(0.55, 0.8, smudge)
    albedo = _rgb(0.62, 0.7, 0.72, u.shape) * (1.0 - dirt[..., None] * 0.35)
    alpha = 0.22 + dirt * 0.45
    height = smudge * 0.05
    return albedo, height, 0.0, 0.94 - dirt * 0.5, alpha


def rubber(u, v):
    grain = value_noise(u, v, 128, 91)
    wear = fbm(u, v, 4, 92, 4)
    tone = 0.05 + grain * 0.03 + wear * 0.04
    albedo = _rgb(tone, tone, tone * 1.05, u.shape)
    height = grain * 0.3 + wear * 0.3
    return albedo, height, 0.0, 0.32 + wear * 0.12, None


def rot_flesh(u, v):
    mottle = fbm(u, v, 4, 101, 5)
    veins = crack_lines(u, v, 6, 102, 0.03)
    sores = _step(0.7, 0.8, fbm(u, v, 8, 103, 4))
    skin = _mix(_rgb(0.36, 0.4, 0.3, u.shape), _rgb(0.3, 0.26, 0.32, u.shape), mottle)
    albedo = _mix(skin, _rgb(0.3, 0.08, 0.1, u.shape), veins * 0.7)
    albedo = _mix(albedo, _rgb(0.4, 0.3, 0.12, u.shape), sores)
    height = mottle * 0.4 + veins * 0.3 - sores * 0.4
    return albedo, height, 0.0, 0.35 + sores * 0.35 + mottle * 0.1, None


def cloth(u, v):
    x = u * 128
    y = v * 128
    twill = ((np.floor(x) + np.floor(y)) % 4 < 2).astype(np.float64)
    fleck = value_noise(u, v, 64, 111)
    wear = fbm(u, v, 4, 112, 4)
    albedo = _rgb(0.3, 0.27, 0.2, u.shape) * (0.82 + twill[..., None] * 0.12 + fleck[..., None] * 0.1) * (0.85 + wear[..., None] * 0.25)
    height = twill * 0.3 + fleck * 0.2 + wear * 0.3
    return albedo, height, 0.0, 0.12 + wear * 0.06, None


BUILDERS = {
    "Asphalt": asphalt,
    "ConcreteCracked": concrete_cracked,
    "BrickRed": brick_red,
    "BrickGrey": brick_grey,
    "MetalRusted": metal_rusted,
    "MetalPainted": metal_painted,
    "Plywood": plywood,
    "TarpFabric": tarp_fabric,
    "Glass": glass,
    "Rubber": rubber,
    "RotFlesh": rot_flesh,
    "Cloth": cloth,
}

NORMAL_STRENGTH = {
    "Asphalt": 3.0, "ConcreteCracked": 3.0, "BrickRed": 4.0, "BrickGrey": 4.0,
    "MetalRusted": 2.5, "MetalPainted": 2.5, "Plywood": 2.0, "TarpFabric": 2.0,
    "Glass": 0.5, "Rubber": 1.5, "RotFlesh": 2.5, "Cloth": 1.5,
}


# ---------------------------------------------------------------- maps

def _bytes(field):
    return np.clip(np.floor(field * 255.0 + 0.5), 0, 255).astype(np.uint8)


def render(family, size=SIZE):
    """Return {'Albedo', 'Normal', 'Mask'} as uint8 arrays (H, W, C)."""
    u, v = grid(size)
    albedo, height, metallic, smooth, alpha = BUILDERS[family](u, v)
    height = np.asarray(height, dtype=np.float64)
    strength = NORMAL_STRENGTH[family] * size / 1024.0
    soft = (height * 4.0 + np.roll(height, 1, 0) + np.roll(height, -1, 0) + np.roll(height, 1, 1) + np.roll(height, -1, 1)) / 8.0
    dx = (np.roll(soft, -1, axis=1) - np.roll(soft, 1, axis=1)) * 0.5
    dy = (np.roll(soft, -1, axis=0) - np.roll(soft, 1, axis=0)) * 0.5
    nx = -dx * strength * 8.0
    ny = dy * strength * 8.0
    nz = np.ones_like(nx)
    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    normal = np.stack([nx / length, ny / length, nz / length], axis=-1) * 0.5 + 0.5

    cavity = np.clip((_blur(height, max(2, size // 128)) - height) * 3.0, 0.0, 1.0)
    occlusion = 1.0 - cavity * 0.7
    metallic = np.broadcast_to(np.asarray(metallic, dtype=np.float64), height.shape)
    smooth = np.clip(np.broadcast_to(np.asarray(smooth, dtype=np.float64), height.shape), 0.0, 1.0)
    mask = np.stack([metallic, occlusion, np.zeros_like(height), smooth], axis=-1)

    albedo = np.clip(albedo, 0.0, 1.0)
    if alpha is not None:
        albedo = np.concatenate([albedo, np.clip(alpha, 0.0, 1.0)[..., None]], axis=-1)
    return {"Albedo": _bytes(albedo), "Normal": _bytes(normal), "Mask": _bytes(mask)}


def encode_png(pixels):
    """PNG with the Up or Sub filter on every row, whichever packs smaller."""
    height, width, channels = pixels.shape
    colour = {3: 2, 4: 6}[channels]
    rows = pixels.reshape(height, width * channels).astype(np.int16)
    up = rows.copy()
    up[1:] = rows[1:] - rows[:-1]
    sub = rows.copy()
    sub[:, channels:] = rows[:, channels:] - rows[:, :-channels]
    best = None
    for kind, diff in ((2, up), (1, sub)):
        filtered = np.empty((height, width * channels + 1), dtype=np.uint8)
        filtered[:, 0] = kind
        filtered[:, 1:] = (diff & 0xFF).astype(np.uint8)
        if kind == 2:
            filtered[0, 0] = 0
            filtered[0, 1:] = pixels[0].reshape(-1)
        packed = zlib.compress(filtered.tobytes(), 9)
        if best is None or len(packed) < len(best):
            best = packed

    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    header = struct.pack(">IIBBBBB", width, height, 8, colour, 0, 0, 0)
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", best) + chunk(b"IEND", b"")


def decode_png(data):
    """Decode the 8-bit RGB/RGBA PNGs this module writes (any filter) back to (H, W, C)."""
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a PNG")
    pos = 8
    idat = b""
    width = height = channels = 0
    while pos < len(data):
        length = struct.unpack(">I", data[pos:pos + 4])[0]
        tag = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + length]
        pos += 12 + length
        if tag == b"IHDR":
            width, height, depth, colour = struct.unpack(">IIBB", body[:10])
            if depth != 8 or colour not in (2, 6):
                raise ValueError("unsupported PNG")
            channels = 3 if colour == 2 else 4
        elif tag == b"IDAT":
            idat += body
    raw = np.frombuffer(zlib.decompress(idat), dtype=np.uint8).reshape(height, width * channels + 1)
    out = np.zeros((height, width * channels), dtype=np.int32)
    for y in range(height):
        kind = raw[y, 0]
        line = raw[y, 1:].astype(np.int32)
        prev = out[y - 1] if y > 0 else np.zeros_like(line)
        if kind == 0:
            out[y] = line
        elif kind == 2:
            out[y] = (line + prev) & 0xFF
        elif kind == 1:
            row = line.copy()
            for x in range(channels, row.size):
                row[x] = (row[x] + row[x - channels]) & 0xFF
            out[y] = row
        else:
            raise ValueError("unsupported filter %d" % kind)
    return out.astype(np.uint8).reshape(height, width, channels)


# ---------------------------------------------------------------- assets

def _guid(seed):
    return hashlib.md5(("outpost-material-library:" + seed).encode("utf-8")).hexdigest()


def texture_path(family, suffix):
    return "%s/%s_%s.png" % (TEXTURE_DIR, family, suffix)


def texture_guid(family, suffix):
    return _guid(texture_path(family, suffix))


def material_path(family, triplanar):
    return "%s/%s_%s.mat" % (MATERIAL_DIR, "MT" if triplanar else "ML", family)


def material_guid(family, triplanar):
    return _guid(material_path(family, triplanar))


def texture_meta(family, suffix):
    text = _texture_meta(texture_path(family, suffix), suffix)
    lines = []
    for line in text.split("\n"):
        if line.startswith("guid: "):
            line = "guid: " + texture_guid(family, suffix)
        elif line.strip() == "maxTextureSize: 256":
            line = line.replace("256", "1024")
        elif line.strip() == "aniso: 1":
            line = line.replace("1", "4")
        elif line.strip() == "alphaIsTransparency: 0" and family == "Glass" and suffix == "Albedo":
            line = line.replace("0", "1")
        lines.append(line)
    return "\n".join(lines)


def _script_guid(root, path):
    with open(os.path.join(root, path + ".meta"), encoding="utf-8") as handle:
        for line in handle:
            if line.startswith("guid: "):
                return line.split()[1].strip()
    raise ValueError("no guid in " + path)


def _tex(name, guid, scale=1.0):
    ref = "{fileID: 2800000, guid: %s, type: 3}" % guid if guid else "{fileID: 0}"
    return (
        "    - %s:\n"
        "        m_Texture: %s\n"
        "        m_Scale: {x: %s, y: %s}\n"
        "        m_Offset: {x: 0, y: 0}\n" % (name, ref, _num(scale), _num(scale))
    )


def _num(value):
    text = ("%.4f" % value).rstrip("0").rstrip(".")
    return text if text not in ("", "-0") else "0"


def _material(name, shader_ref, keywords, textures, floats, colors, tags, queue):
    tex = "".join(textures)
    flt = "".join("    - %s: %s\n" % (k, _num(v)) for k, v in floats)
    col = "".join("    - %s: {r: %s, g: %s, b: %s, a: %s}\n" % (k, _num(c[0]), _num(c[1]), _num(c[2]), _num(c[3])) for k, c in colors)
    kw = "".join("  - %s\n" % k for k in keywords) if keywords else ""
    tag = "".join("    %s: %s\n" % (k, v) for k, v in tags)
    return (
        "%YAML 1.1\n"
        "%TAG !u! tag:unity3d.com,2011:\n"
        "--- !u!21 &2100000\n"
        "Material:\n"
        "  serializedVersion: 8\n"
        "  m_ObjectHideFlags: 0\n"
        "  m_CorrespondingSourceObject: {fileID: 0}\n"
        "  m_PrefabInstance: {fileID: 0}\n"
        "  m_PrefabAsset: {fileID: 0}\n"
        "  m_Name: " + name + "\n"
        "  m_Shader: " + shader_ref + "\n"
        "  m_Parent: {fileID: 0}\n"
        "  m_ModifiedSerializedProperties: 0\n"
        "  m_ValidKeywords:" + ("\n" + kw if kw else " []\n") +
        "  m_InvalidKeywords: []\n"
        "  m_LightmapFlags: 4\n"
        "  m_EnableInstancingVariants: 1\n"
        "  m_DoubleSidedGI: 0\n"
        "  m_CustomRenderQueue: " + str(queue) + "\n"
        "  stringTagMap:" + ("\n" + tag if tag else " {}\n") +
        "  disabledShaderPasses:" + (" []\n" if queue < 3000 else "\n  - DepthOnly\n  - SHADOWCASTER\n") +
        "  m_LockedProperties: \n"
        "  m_SavedProperties:\n"
        "    serializedVersion: 3\n"
        "    m_TexEnvs:\n" + tex +
        "    m_Ints: []\n"
        "    m_Floats:\n" + flt +
        "    m_Colors:\n" + col +
        "  m_BuildTextureStacks: []\n"
        "  m_AllowLocking: 1\n"
    )


def lit_material(family):
    albedo = texture_guid(family, "Albedo")
    normal = texture_guid(family, "Normal")
    mask = texture_guid(family, "Mask")
    transparent = family == "Glass"
    textures = [
        _tex("_BaseMap", albedo),
        _tex("_BumpMap", normal),
        _tex("_DetailAlbedoMap", None),
        _tex("_DetailMask", None),
        _tex("_DetailNormalMap", None),
        _tex("_EmissionMap", None),
        _tex("_MainTex", albedo),
        _tex("_MetallicGlossMap", mask),
        _tex("_OcclusionMap", mask),
        _tex("_ParallaxMap", None),
        _tex("_SpecGlossMap", None),
    ]
    floats = [
        ("_AlphaClip", 0), ("_AlphaToMask", 0), ("_Blend", 0), ("_BlendModePreserveSpecular", 1),
        ("_BumpScale", 1), ("_ClearCoatMask", 0), ("_ClearCoatSmoothness", 0), ("_Cull", 2),
        ("_Cutoff", 0.5), ("_DetailAlbedoMapScale", 1), ("_DetailNormalMapScale", 1),
        ("_DstBlend", 10 if transparent else 0), ("_DstBlendAlpha", 10 if transparent else 0),
        ("_EnvironmentReflections", 1), ("_GlossMapScale", 1), ("_Glossiness", 0),
        ("_GlossyReflections", 1), ("_Metallic", 1), ("_OcclusionStrength", 1), ("_Parallax", 0.005),
        ("_QueueOffset", 0), ("_ReceiveShadows", 1), ("_Smoothness", 1), ("_SmoothnessTextureChannel", 0),
        ("_SpecularHighlights", 1), ("_SrcBlend", 5 if transparent else 1), ("_SrcBlendAlpha", 1),
        ("_Surface", 1 if transparent else 0), ("_WorkflowMode", 1), ("_XRMotionVectorsPass", 1),
        ("_ZWrite", 0 if transparent else 1),
    ]
    colors = [("_BaseColor", (1, 1, 1, 1)), ("_Color", (1, 1, 1, 1)), ("_EmissionColor", (0, 0, 0, 1)), ("_SpecColor", (0.2, 0.2, 0.2, 1))]
    keywords = ["_METALLICSPECGLOSSMAP", "_NORMALMAP", "_OCCLUSIONMAP"]
    if transparent:
        keywords.append("_SURFACE_TYPE_TRANSPARENT")
    tags = [("RenderType", "Transparent" if transparent else "Opaque")]
    shader = "{fileID: 4800000, guid: %s, type: 3}" % LIT_SHADER_GUID
    return _material("ML_" + family, shader, sorted(keywords), textures, floats, colors, tags, 3000 if transparent else -1)


def triplanar_material(family, shader_guid):
    textures = [
        _tex("_BaseMap", texture_guid(family, "Albedo")),
        _tex("_BumpMap", texture_guid(family, "Normal")),
        _tex("_MaskMap", texture_guid(family, "Mask")),
    ]
    floats = [
        ("_Tiling", 1.0 / TILE_METRES[family]), ("_BlendSharpness", 4), ("_NormalStrength", 1),
        ("_TopAmount", 0.5 if family in ("Asphalt", "ConcreteCracked", "BrickRed", "BrickGrey") else 0.25),
        ("_TopThreshold", 0.7), ("_GrimeHeight", 1.2), ("_GrimeStrength", 0.35), ("_Wetness", 0),
        ("_VertexAO", 0),
    ]
    colors = [("_BaseColor", (1, 1, 1, 1)), ("_TopColor", (0.24, 0.26, 0.16, 1)), ("_Tint", (1, 1, 1, 1))]
    shader = "{fileID: 4800000, guid: %s, type: 3}" % shader_guid
    return _material("MT_" + family, shader, [], textures, floats, colors, [("RenderType", "Opaque")], -1)


def material_meta(family, triplanar):
    return (
        "fileFormatVersion: 2\n"
        "guid: %s\n"
        "NativeFormatImporter:\n"
        "  externalObjects: {}\n"
        "  mainObjectFileID: 2100000\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n" % material_guid(family, triplanar)
    )


def triplanar_families():
    return [f for f in FAMILIES if f != "Glass"]


def library_asset(script_guid):
    lines = [
        "%YAML 1.1",
        "%TAG !u! tag:unity3d.com,2011:",
        "--- !u!114 &11400000",
        "MonoBehaviour:",
        "  m_ObjectHideFlags: 0",
        "  m_CorrespondingSourceObject: {fileID: 0}",
        "  m_PrefabInstance: {fileID: 0}",
        "  m_PrefabAsset: {fileID: 0}",
        "  m_GameObject: {fileID: 0}",
        "  m_Enabled: 1",
        "  m_EditorHideFlags: 0",
        "  m_Script: {fileID: 11500000, guid: %s, type: 3}" % script_guid,
        "  m_Name: MaterialLibrary",
        "  m_EditorClassIdentifier: OutpostZero::OutpostZero.Graphics.MaterialLibrary",
        "  entries:",
    ]
    for index, family in enumerate(FAMILIES):
        tri = family != "Glass"
        lines.append("  - family: %d" % (index + 1))
        lines.append("    lit: {fileID: 2100000, guid: %s, type: 2}" % material_guid(family, False))
        lines.append("    triplanar: " + ("{fileID: 2100000, guid: %s, type: 2}" % material_guid(family, True) if tri else "{fileID: 0}"))
    return "\n".join(lines) + "\n"


def library_meta():
    return (
        "fileFormatVersion: 2\n"
        "guid: %s\n"
        "NativeFormatImporter:\n"
        "  externalObjects: {}\n"
        "  mainObjectFileID: 11400000\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n" % _guid(LIBRARY_ASSET)
    )


def repo_root():
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def text_files(root):
    """Every non-texture output as {relative path: text}."""
    shader_guid = _script_guid(root, TRIPLANAR_SHADER)
    files = {}
    for family in FAMILIES:
        for suffix in ("Albedo", "Normal", "Mask"):
            files[texture_path(family, suffix) + ".meta"] = texture_meta(family, suffix)
        files[material_path(family, False)] = lit_material(family)
        files[material_path(family, False) + ".meta"] = material_meta(family, False)
        if family != "Glass":
            files[material_path(family, True)] = triplanar_material(family, shader_guid)
            files[material_path(family, True) + ".meta"] = material_meta(family, True)
    files[LIBRARY_ASSET] = library_asset(_script_guid(root, LIBRARY_SCRIPT))
    files[LIBRARY_ASSET + ".meta"] = library_meta()
    for folder in (MATERIAL_DIR, TEXTURE_DIR):
        files[folder + ".meta"] = (
            "fileFormatVersion: 2\nguid: %s\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n"
            "  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % _guid(folder)
        )
    return files


def write(root=None, families=FAMILIES):
    root = root or repo_root()
    os.makedirs(os.path.join(root, TEXTURE_DIR), exist_ok=True)
    for family in families:
        for suffix, pixels in render(family).items():
            with open(os.path.join(root, texture_path(family, suffix)), "wb") as handle:
                handle.write(encode_png(pixels))
    for path, text in text_files(root).items():
        with open(os.path.join(root, path), "w", encoding="utf-8", newline="\n") as handle:
            handle.write(text)


def stale(root=None):
    """Paths whose committed content no longer matches the generator."""
    root = root or repo_root()
    out = []
    for path, text in text_files(root).items():
        full = os.path.join(root, path)
        if not os.path.exists(full):
            out.append(path)
            continue
        with open(full, encoding="utf-8") as handle:
            if handle.read() != text:
                out.append(path)
    for family in FAMILIES:
        maps = render(family)
        for suffix, pixels in maps.items():
            full = os.path.join(root, texture_path(family, suffix))
            if not os.path.exists(full):
                out.append(texture_path(family, suffix))
                continue
            with open(full, "rb") as handle:
                if not np.array_equal(decode_png(handle.read()), pixels):
                    out.append(texture_path(family, suffix))
    return out


if __name__ == "__main__":
    if "--check" in sys.argv:
        bad = stale()
        for path in bad:
            print("stale: " + path)
        sys.exit(1 if bad else 0)
    write()
    print("wrote %d families" % len(FAMILIES))
