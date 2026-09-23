import os
import re
import unittest

try:
    import numpy as np
    import material_library as ml
except ImportError:  # numpy is optional for the rest of the pipeline tests
    np = None
    ml = None

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SMALL = 128


@unittest.skipIf(ml is None, "numpy not installed")
class MaterialLibraryTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.maps = {family: ml.render(family, SMALL) for family in ml.FAMILIES}

    def test_twelve_families_match_the_csharp_enum(self):
        with open(os.path.join(ROOT, ml.LIBRARY_SCRIPT), encoding="utf-8") as handle:
            source = handle.read()
        body = source[source.index("enum SurfaceFamily"):]
        body = body[: body.index("}")]
        names = re.findall(r"(\w+) = (\d+)", body)
        self.assertEqual([(family, str(i + 1)) for i, family in enumerate(ml.FAMILIES)], names[1:])

    def test_every_map_has_the_expected_layout(self):
        for family, maps in self.maps.items():
            self.assertEqual(set(maps), {"Albedo", "Normal", "Mask"}, family)
            self.assertEqual(maps["Albedo"].shape[-1], 4 if family == "Glass" else 3, family)
            self.assertEqual(maps["Normal"].shape, (SMALL, SMALL, 3), family)
            self.assertEqual(maps["Mask"].shape, (SMALL, SMALL, 4), family)
            self.assertTrue(np.all(maps["Mask"][..., 2] == 0), family)
            self.assertTrue(np.all(maps["Normal"][..., 2] >= 128), family + " normals face out")

    def test_metals_are_metallic_and_masonry_is_not(self):
        self.assertGreater(self.maps["MetalRusted"]["Mask"][..., 0].mean(), 60)
        chipped = self.maps["MetalPainted"]["Mask"][..., 0]
        self.assertGreater(chipped.max(), 200, "bare steel shows through chipped paint")
        self.assertLess(chipped.mean(), 60, "paint is dielectric")
        for family in ("BrickRed", "ConcreteCracked", "Cloth", "Asphalt"):
            self.assertLess(self.maps[family]["Mask"][..., 0].mean(), 30, family)

    def test_maps_tile_without_seams(self):
        """The wrap from the last column back to the first is no harsher than any column inside."""
        for family, maps in self.maps.items():
            for suffix, pixels in maps.items():
                p = pixels.astype(np.int32)
                inner = np.abs(np.diff(p, axis=1)).mean(axis=(0, 2)).max()
                edge = np.abs(p[:, 0] - p[:, -1]).mean()
                self.assertLessEqual(edge, inner * 1.1 + 0.5, family + " " + suffix + " horizontal")
                inner = np.abs(np.diff(p, axis=0)).mean(axis=(1, 2)).max()
                edge = np.abs(p[0] - p[-1]).mean()
                self.assertLessEqual(edge, inner * 1.1 + 0.5, family + " " + suffix + " vertical")

    def test_families_look_distinct(self):
        means = {family: maps["Albedo"][..., :3].reshape(-1, 3).mean(axis=0) for family, maps in self.maps.items()}
        families = list(means)
        for i, a in enumerate(families):
            for b in families[i + 1:]:
                va = self.maps[a]["Albedo"][..., :3].astype(np.float64)
                vb = self.maps[b]["Albedo"][..., :3].astype(np.float64)
                apart = np.abs(means[a] - means[b]).sum() + abs(va.std() - vb.std())
                self.assertGreater(apart, 4.0, a + " vs " + b)

    def test_render_is_deterministic(self):
        again = ml.render("BrickRed", SMALL)
        for suffix, pixels in again.items():
            self.assertTrue(np.array_equal(pixels, self.maps["BrickRed"][suffix]), suffix)

    def test_png_round_trips(self):
        for pixels in (self.maps["Glass"]["Albedo"], self.maps["Asphalt"]["Normal"]):
            self.assertTrue(np.array_equal(ml.decode_png(ml.encode_png(pixels)), pixels))

    def test_lit_materials_enable_urp_maps(self):
        for family in ml.FAMILIES:
            text = ml.lit_material(family)
            self.assertIn(ml.LIT_SHADER_GUID, text)
            for keyword in ("_METALLICSPECGLOSSMAP", "_NORMALMAP", "_OCCLUSIONMAP"):
                self.assertIn(keyword, text, family)
            for suffix in ("Albedo", "Normal", "Mask"):
                self.assertIn(ml.texture_guid(family, suffix), text, family)
        self.assertIn("RenderType: Transparent", ml.lit_material("Glass"))

    def test_glass_has_no_triplanar_material(self):
        self.assertNotIn("Glass", ml.triplanar_families())
        self.assertEqual(len(ml.triplanar_families()), len(ml.FAMILIES) - 1)

    def test_guids_are_unique(self):
        guids = []
        for family in ml.FAMILIES:
            guids += [ml.texture_guid(family, s) for s in ("Albedo", "Normal", "Mask")]
            guids.append(ml.material_guid(family, False))
            guids.append(ml.material_guid(family, True))
        self.assertEqual(len(guids), len(set(guids)))

    def test_committed_text_files_match_the_generator(self):
        mismatched = []
        for path, text in ml.text_files(ROOT).items():
            full = os.path.join(ROOT, path)
            if not os.path.exists(full):
                mismatched.append(path)
                continue
            with open(full, encoding="utf-8") as handle:
                if handle.read() != text:
                    mismatched.append(path)
        self.assertEqual([], mismatched)

    def test_committed_textures_exist_at_full_size(self):
        for family in ml.FAMILIES:
            for suffix in ("Albedo", "Normal", "Mask"):
                full = os.path.join(ROOT, ml.texture_path(family, suffix))
                self.assertTrue(os.path.exists(full), full)
                with open(full, "rb") as handle:
                    pixels = ml.decode_png(handle.read())
                self.assertEqual(pixels.shape[:2], (ml.SIZE, ml.SIZE), full)


if __name__ == "__main__":
    unittest.main()
