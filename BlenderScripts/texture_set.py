"""Procedural albedo, normal, occlusion, mask, and icon maps. No Blender required."""

import hashlib
import os
import struct
import zlib

SIZE = 128
ICON_SIZE = 32
SUFFIXES = ("Albedo", "Normal", "AO", "Mask", "Icon")


def fnv(name):
    value = 2166136261
    for char in name:
        value ^= ord(char)
        value = (value * 16777619) & 0xFFFFFFFF
    return value


def palette(name):
    text = name.lower()
    if "explosive" in text:
        return (148, 38, 30)
    if "toxic" in text:
        return (42, 118, 46)
    if "oil" in text:
        return (26, 26, 30)
    if "walker" in text:
        return (62, 66, 50)
    if "runner" in text:
        return (78, 48, 42)
    if "brute" in text:
        return (50, 46, 42)
    if "merchant" in text:
        return (92, 74, 48)
    if "leader" in text or "colonist" in text or "survivor" in text:
        return (74, 66, 54)
    if "road" in text:
        return (44, 44, 46)
    if "military" in text:
        return (50, 60, 42)
    if "crate" in text or "wood" in text:
        return (96, 70, 44)
    if "sand" in text:
        return (112, 98, 72)
    if "concrete" in text or "jersey" in text:
        return (122, 120, 114)
    if "med" in text or "cot" in text:
        return (156, 156, 148)
    if "water" in text:
        return (46, 82, 96)
    if "generator" in text:
        return (66, 60, 34)
    if "campfire" in text:
        return (96, 44, 22)
    if "watch" in text:
        return (72, 68, 60)
    if "machete" in text:
        return (168, 170, 166)
    if "weapon" in text or "pistol" in text or "rifle" in text or "shotgun" in text:
        return (38, 40, 42)
    if "ammo" in text:
        return (74, 64, 42)
    if "scrap" in text:
        return (82, 66, 42)
    if "building" in text or "store" in text or "warehouse" in text or "ruin" in text:
        return (80, 74, 66)
    if "vehicle" in text or "sedan" in text or "truck" in text:
        return (54, 50, 48)
    if "lamp" in text:
        return (70, 70, 64)
    if "dumpster" in text:
        return (40, 64, 48)
    return (90, 84, 76)


def metal_amount(name):
    text = name.lower()
    keys = (
        "weapon", "pistol", "rifle", "shotgun", "machete", "barrel",
        "dumpster", "vehicle", "sedan", "truck", "generator", "lamp",
    )
    if any(key in text for key in keys):
        return 210
    if "ammo" in text or "scrap" in text:
        return 96
    return 16


def glows(name):
    text = name.lower()
    return "zombie" in text or "campfire" in text or "lamp" in text


def vein(name, x, y, seed):
    if not glows(name):
        return False
    stride = 11 if "zombie" in name.lower() else 29
    return _hash(x, y, seed ^ 0x51) % stride == 0


def _hash(ix, iy, seed):
    mixed = (ix * 374761393 + iy * 668265263 + seed) & 0xFFFFFFFF
    mixed = ((mixed ^ (mixed >> 13)) * 1274126177) & 0xFFFFFFFF
    return mixed & 255


def _clamp(value):
    if value < 0:
        return 0
    if value > 255:
        return 255
    return value


def rasters(name, size=SIZE):
    """Return RGBA bytearrays for Albedo, Normal, AO, Mask, and Icon."""
    seed = fnv(name)
    base = palette(name)
    metal = metal_amount(name)
    glow = glows(name)
    count = size * size
    height = [0] * count
    albedo = bytearray(count * 4)
    for y in range(size):
        row = y * size
        for x in range(size):
            coarse = _hash(x // 4, y // 4, seed)
            fine = _hash(x, y, seed ^ 0x9E3779B9)
            value = (coarse * 3 + fine) // 4
            height[row + x] = value
            edge = min(x, y, size - 1 - x, size - 1 - y)
            shade = 255 if edge > 2 else 168
            grain = 210 + (fine % 46)
            scale = (value * shade * grain) // (255 * 255)
            speck = vein(name, x, y, seed)
            red = _clamp(base[0] * scale // 128 + (70 if speck else 0))
            green = _clamp(base[1] * scale // 128 + (18 if speck else 0))
            blue = _clamp(base[2] * scale // 128)
            index = (row + x) * 4
            albedo[index] = red
            albedo[index + 1] = green
            albedo[index + 2] = blue
            albedo[index + 3] = 255

    normal = bytearray(count * 4)
    ao = bytearray(count * 4)
    mask = bytearray(count * 4)
    for y in range(size):
        row = y * size
        for x in range(size):
            left = height[row + (x - 1 if x > 0 else 0)]
            right = height[row + (x + 1 if x + 1 < size else x)]
            up = height[(y - 1 if y > 0 else 0) * size + x]
            down = height[(y + 1 if y + 1 < size else y) * size + x]
            dx = right - left
            dy = down - up
            nx = -dx * 6
            ny = -dy * 6
            nz = 72
            length = (nx * nx + ny * ny + nz * nz) ** 0.5
            index = (row + x) * 4
            normal[index] = _clamp(int((nx / length) * 127 + 128))
            normal[index + 1] = _clamp(int((ny / length) * 127 + 128))
            normal[index + 2] = _clamp(int((nz / length) * 127 + 128))
            normal[index + 3] = 255
            edge = min(x, y, size - 1 - x, size - 1 - y)
            cavity = 255 if edge > 4 else 150 + edge * 20
            occlusion = _clamp((cavity * (180 + height[row + x] // 3)) // 255)
            ao[index] = occlusion
            ao[index + 1] = occlusion
            ao[index + 2] = occlusion
            ao[index + 3] = 255
            roughness = _clamp(255 - metal // 2 + (height[row + x] % 24) - 12)
            emissive = 255 if vein(name, x, y, seed) else 0
            mask[index] = metal
            mask[index + 1] = roughness
            mask[index + 2] = emissive
            mask[index + 3] = 255

    icon_size = ICON_SIZE if size >= ICON_SIZE else size
    step = max(1, size // icon_size)
    icon = bytearray(icon_size * icon_size * 4)
    for y in range(icon_size):
        for x in range(icon_size):
            source = ((y * step) * size + (x * step)) * 4
            dest = (y * icon_size + x) * 4
            icon[dest:dest + 4] = albedo[source:source + 4]
    return {
        "Albedo": albedo,
        "Normal": normal,
        "AO": ao,
        "Mask": mask,
        "Icon": icon,
    }, size, icon_size


def encode_png(width, height, rgba):
    if len(rgba) != width * height * 4:
        raise ValueError("rgba length does not match the image size")

    def chunk(tag, data):
        return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)

    rows = bytearray()
    stride = width * 4
    for y in range(height):
        rows.append(0)
        start = y * stride
        rows.extend(rgba[start:start + stride])
    header = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", header) + chunk(b"IDAT", zlib.compress(bytes(rows), 9)) + chunk(b"IEND", b"")


def decode_png(data):
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError("not a png")
    position = 8
    width = height = 0
    payload = b""
    while position + 8 <= len(data):
        length = struct.unpack(">I", data[position:position + 4])[0]
        tag = data[position + 4:position + 8]
        chunk = data[position + 8:position + 8 + length]
        if tag == b"IHDR":
            width, height = struct.unpack(">II", chunk[:8])
        elif tag == b"IDAT":
            payload += chunk
        elif tag == b"IEND":
            break
        position += 12 + length
    raw = zlib.decompress(payload)
    stride = width * 4
    pixels = bytearray()
    cursor = 0
    for _y in range(height):
        if raw[cursor] != 0:
            raise ValueError("only filter 0 is written")
        cursor += 1
        pixels.extend(raw[cursor:cursor + stride])
        cursor += stride
    return width, height, pixels


def png_bytes(name, size=SIZE):
    images, full, icon = rasters(name, size)
    encoded = {}
    for suffix, pixels in images.items():
        side = icon if suffix == "Icon" else full
        encoded[suffix] = encode_png(side, side, pixels)
    return encoded


def guid_for(png_path):
    normalized = png_path.replace("\\", "/")
    marker = "Assets/Models/"
    index = normalized.find(marker)
    key = normalized[index:] if index >= 0 else os.path.basename(normalized)
    return hashlib.md5(key.encode("utf-8")).hexdigest()


def meta_text(png_path, suffix):
    linear = suffix in ("Normal", "AO", "Mask")
    texture_type = 1 if suffix == "Normal" else 0
    srgb = 0 if linear else 1
    return (
        "fileFormatVersion: 2\n"
        "guid: {guid}\n"
        "TextureImporter:\n"
        "  internalIDToNameTable: []\n"
        "  externalObjects: {{}}\n"
        "  serializedVersion: 13\n"
        "  mipmaps:\n"
        "    mipMapMode: 0\n"
        "    enableMipMap: 1\n"
        "    sRGBTexture: {srgb}\n"
        "    linearTexture: 0\n"
        "    fadeOut: 0\n"
        "    borderMipMap: 0\n"
        "    mipMapsPreserveCoverage: 0\n"
        "    alphaTestReferenceValue: 0.5\n"
        "    mipMapFadeDistanceStart: 1\n"
        "    mipMapFadeDistanceEnd: 3\n"
        "  bumpmap:\n"
        "    convertToNormalMap: 0\n"
        "    externalNormalMap: 0\n"
        "    heightScale: 0.25\n"
        "    normalMapFilter: 0\n"
        "    flipGreenChannel: 0\n"
        "  isReadable: 0\n"
        "  streamingMipmaps: 0\n"
        "  streamingMipmapsPriority: 0\n"
        "  vTOnly: 0\n"
        "  ignoreMipmapLimit: 0\n"
        "  grayScaleToAlpha: 0\n"
        "  generateCubemap: 6\n"
        "  cubemapConvolution: 0\n"
        "  seamlessCubemap: 0\n"
        "  textureFormat: 1\n"
        "  maxTextureSize: 256\n"
        "  textureSettings:\n"
        "    serializedVersion: 2\n"
        "    filterMode: 1\n"
        "    aniso: 1\n"
        "    mipBias: 0\n"
        "    wrapU: 0\n"
        "    wrapV: 0\n"
        "    wrapW: 0\n"
        "  nPOTScale: 0\n"
        "  lightmap: 0\n"
        "  compressionQuality: 50\n"
        "  spriteMode: 0\n"
        "  spriteExtrude: 1\n"
        "  spriteMeshType: 1\n"
        "  alignment: 0\n"
        "  spritePivot: {{x: 0.5, y: 0.5}}\n"
        "  spritePixelsToUnits: 100\n"
        "  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}\n"
        "  spriteGenerateFallbackPhysicsShape: 1\n"
        "  alphaUsage: 1\n"
        "  alphaIsTransparency: 0\n"
        "  spriteTessellationDetail: -1\n"
        "  textureType: {texture_type}\n"
        "  textureShape: 1\n"
        "  singleChannelComponent: 0\n"
        "  flipbookRows: 1\n"
        "  flipbookColumns: 1\n"
        "  maxTextureSizeSet: 0\n"
        "  compressionQualitySet: 0\n"
        "  textureFormatSet: 0\n"
        "  ignorePngGamma: 0\n"
        "  applyGammaDecoding: 0\n"
        "  swizzle: 50462976\n"
        "  cookieLightType: 0\n"
        "  platformSettings:\n"
        "  - serializedVersion: 3\n"
        "    buildTarget: DefaultTexturePlatform\n"
        "    maxTextureSize: 256\n"
        "    resizeAlgorithm: 0\n"
        "    textureFormat: -1\n"
        "    textureCompression: 1\n"
        "    compressionQuality: 50\n"
        "    crunchedCompression: 0\n"
        "    allowsAlphaSplitting: 0\n"
        "    overridden: 0\n"
        "    ignorePlatformSupport: 0\n"
        "    androidETC2FallbackOverride: 0\n"
        "    forceMaximumCompressionQuality_BC6H_BC7: 0\n"
        "  spriteSheet:\n"
        "    serializedVersion: 2\n"
        "    sprites: []\n"
        "    outline: []\n"
        "    physicsShape: []\n"
        "    bones: []\n"
        "    spriteID: \n"
        "    internalID: 0\n"
        "    vertices: []\n"
        "    indices: \n"
        "    edges: []\n"
        "    weights: []\n"
        "    secondaryTextures: []\n"
        "    nameFileIdTable: {{}}\n"
        "  mipmapLimitGroupName: \n"
        "  pSDRemoveMatte: 0\n"
        "  userData: \n"
        "  assetBundleName: \n"
        "  assetBundleVariant: \n"
    ).format(guid=guid_for(png_path), srgb=srgb, texture_type=texture_type)


def map_paths(fbx_path):
    stem = fbx_path[:-4] if fbx_path.lower().endswith(".fbx") else fbx_path
    return {suffix: stem + "_" + suffix + ".png" for suffix in SUFFIXES}


def write_set(fbx_path, size=SIZE):
    name = os.path.splitext(os.path.basename(fbx_path))[0]
    encoded = png_bytes(name, size)
    written = []
    for suffix, path in map_paths(fbx_path).items():
        directory = os.path.dirname(path)
        if directory:
            os.makedirs(directory, exist_ok=True)
        with open(path, "wb") as handle:
            handle.write(encoded[suffix])
        with open(path + ".meta", "w", encoding="utf-8", newline="\n") as handle:
            handle.write(meta_text(path, suffix))
        written.append(path)
    return written


def bake_manifest(root):
    import json
    manifest_path = os.path.join(root, "BlenderScripts", "assets.manifest.json")
    with open(manifest_path, encoding="utf-8") as handle:
        manifest = json.load(handle)
    written = []
    for relative in manifest["assets"]:
        written.extend(write_set(os.path.join(root, relative)))
    return written


if __name__ == "__main__":
    repo = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
    paths = bake_manifest(repo)
    print("Wrote {0} texture files.".format(len(paths)))
