"""Procedural surface maps are deterministic PNGs with a readable palette."""

import json
import os
import tempfile
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pipeline_plan import asset_paths
import icon_render
from icon_render import ICON_RENDER_SIZE

from texture_set import (
    ICON_SIZE,
    SIZE,
    SUFFIXES,
    decode_png,
    glows,
    metal_amount,
    palette,
    png_bytes,
    rasters,
    write_set,
)


def repo_root():
    return os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))


def average(name, suffix, size=32):
    images, _full, _icon = rasters(name, size)
    pixels = images[suffix]
    count = len(pixels) // 4
    red = green = blue = 0
    peak_blue = 0
    for index in range(count):
        offset = index * 4
        red += pixels[offset]
        green += pixels[offset + 1]
        blue += pixels[offset + 2]
        if pixels[offset + 2] > peak_blue:
            peak_blue = pixels[offset + 2]
    return red / count, green / count, blue / count, peak_blue


class TextureSetTests(unittest.TestCase):
    def test_palettes_separate_hazards_and_roles(self):
        explosive = palette("Prop_Barrel_Red_Explosive")
        toxic = palette("Prop_Barrel_Toxic")
        oil = palette("Prop_Barrel_Oil")
        self.assertGreater(explosive[0], explosive[1])
        self.assertGreater(explosive[0], explosive[2])
        self.assertGreater(toxic[1], toxic[0])
        self.assertGreater(toxic[1], toxic[2])
        self.assertLess(oil[0], 40)
        self.assertGreater(metal_amount("Weapon_AssaultRifle"), metal_amount("Prop_Crate_Wood"))
        self.assertTrue(glows("Zombie_Walker"))
        self.assertFalse(glows("Prop_Crate_Wood"))

    def test_albedo_reads_the_palette_and_zombies_glow(self):
        red, green, blue, _peak = average("Prop_Barrel_Red_Explosive", "Albedo")
        self.assertGreater(red, green)
        self.assertGreater(red, blue)
        green_r, green_g, green_b, _peak = average("Prop_Barrel_Toxic", "Albedo")
        self.assertGreater(green_g, green_r)
        self.assertGreater(green_g, green_b)
        _r, _g, _b, walker_peak = average("Zombie_Walker", "Mask")
        _r, _g, _b, crate_peak = average("Prop_Crate_Wood", "Mask")
        self.assertEqual(walker_peak, 255)
        self.assertEqual(crate_peak, 0)
        rifle_metal, _rough, _emit, _peak = average("Weapon_AssaultRifle", "Mask")
        crate_metal, _rough, _emit, _peak = average("Prop_Crate_Wood", "Mask")
        self.assertGreater(rifle_metal, crate_metal)

    def test_png_roundtrip_keeps_size_and_bytes(self):
        first = png_bytes("Zombie_Runner", SIZE)
        second = png_bytes("Zombie_Runner", SIZE)
        self.assertEqual(set(first), set(SUFFIXES))
        self.assertEqual(first["Albedo"], second["Albedo"])
        self.assertNotEqual(first["Albedo"], first["AO"])
        width, height, pixels = decode_png(first["Albedo"])
        self.assertEqual((width, height), (SIZE, SIZE))
        self.assertEqual(len(pixels), SIZE * SIZE * 4)
        icon_w, icon_h, _icon = decode_png(first["Icon"])
        self.assertEqual((icon_w, icon_h), (ICON_SIZE, ICON_SIZE))

    def test_write_set_lands_beside_the_fbx(self):
        with tempfile.TemporaryDirectory() as folder:
            fbx = os.path.join(folder, "Prop_Crate_Wood.fbx")
            with open(fbx, "wb") as handle:
                handle.write(b"fbx")
            written = write_set(fbx, size=32)
            self.assertEqual(len(written), len(SUFFIXES))
            for path in written:
                self.assertTrue(os.path.isfile(path))
                self.assertTrue(os.path.isfile(path + ".meta"))
                with open(path, "rb") as handle:
                    width, height, _pixels = decode_png(handle.read())
                side = 32 if path.endswith("_Icon.png") else 32
                self.assertEqual((width, height), (side, side))

    def test_every_manifest_model_has_a_texture_set(self):
        root = repo_root()
        with open(os.path.join(root, "BlenderScripts", "assets.manifest.json"), encoding="utf-8") as handle:
            manifest = json.load(handle)
        self.assertEqual(manifest["textures"], list(SUFFIXES))
        self.assertEqual(manifest["textureSize"], SIZE)
        missing = []
        rendered = {"Assets/Models/" + e["output"] for e in manifest["entries"] if icon_render.rendered(e.get("tags"))}
        self.assertGreaterEqual(len(rendered), 26)
        for relative in asset_paths(manifest):
            stem = relative[:-4]
            for suffix in SUFFIXES:
                path = os.path.join(root, stem + "_" + suffix + ".png")
                meta = path + ".meta"
                if not os.path.isfile(path) or not os.path.isfile(meta):
                    missing.append(stem + "_" + suffix)
                    continue
                with open(path, "rb") as handle:
                    width, height, _pixels = decode_png(handle.read())
                icon_side = ICON_RENDER_SIZE if relative in rendered else ICON_SIZE
                expected = icon_side if suffix == "Icon" else SIZE
                if (width, height) != (expected, expected):
                    missing.append(stem + "_" + suffix + ":size")
        self.assertEqual(missing, [])

    def test_metas_compress_per_platform_and_stream_by_role(self):
        import re
        import texture_set
        path = "Assets/Models/Props/Prop_Dumpster_Normal.png"
        normal = texture_set.meta_text(path, "Normal")
        albedo = texture_set.meta_text(path, "Albedo")
        icon = texture_set.meta_text(path, "Icon")

        def fmt(text, target):
            match = re.search(r"buildTarget: " + target + r"\n(?:    .*\n)*?    textureFormat: (-?\d+)\n(?:    .*\n)*?    overridden: (\d)", text)
            self.assertIsNotNone(match, target)
            return int(match.group(1)), match.group(2)

        self.assertEqual(fmt(normal, "Standalone"), (texture_set.FORMAT_BC5, "1"))
        self.assertEqual(fmt(albedo, "Standalone"), (texture_set.FORMAT_BC7, "1"))
        self.assertEqual(fmt(normal, "Android"), (texture_set.FORMAT_ASTC_4X4, "1"))
        self.assertEqual(fmt(albedo, "iPhone"), (texture_set.FORMAT_ASTC_6X6, "1"))
        self.assertEqual(fmt(albedo, "DefaultTexturePlatform"), (-1, "0"))
        self.assertIn("  streamingMipmaps: 1\n", albedo)
        self.assertIn("  streamingMipmaps: 0\n", icon)
        self.assertIn("    enableMipMap: 1\n", icon)
        import decal_atlas
        self.assertIn("  streamingMipmaps: 0\n", decal_atlas.meta_text())


if __name__ == "__main__":
    unittest.main()
