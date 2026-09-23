import os
import re
import unittest

import decal_atlas
from texture_set import decode_png

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


class DecalAtlasTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        with open(os.path.join(ROOT, decal_atlas.OUTPUT), "rb") as handle:
            cls.width, cls.height, cls.pixels = decode_png(handle.read())

    def alpha(self, index, x, y):
        ox, oy = decal_atlas.cell_origin(index)
        return self.pixels[((oy + y) * self.width + ox + x) * 4 + 3]

    def test_layout_fits_the_grid_and_matches_the_issue(self):
        counts = dict(decal_atlas.LAYOUT)
        self.assertEqual(8, counts["blood"])
        self.assertEqual(4, counts["drip"])
        for surface in ("concrete", "metal", "wood"):
            self.assertEqual(3, counts["hole_" + surface])
        for kind in ("scorch", "oil", "footprint"):
            self.assertEqual(2, counts[kind])
        self.assertLessEqual(len(decal_atlas.cells()), decal_atlas.COLUMNS * decal_atlas.ROWS)
        self.assertEqual((decal_atlas.CELL * decal_atlas.COLUMNS, decal_atlas.CELL * decal_atlas.ROWS), (self.width, self.height))

    def test_the_unity_table_matches(self):
        with open(os.path.join(ROOT, "Assets", "Scripts", "Graphics", "DecalAtlas.cs")) as handle:
            source = handle.read()
        constants = dict(re.findall(r'public const string (\w+) = "(\w+)";', source))
        kinds = re.search(r"Kinds = \{ ([^}]+) \}", source).group(1).replace(" ", "").split(",")
        counts = [int(c) for c in re.search(r"Counts = \{ ([^}]+) \}", source).group(1).replace(" ", "").split(",")]
        self.assertEqual(list(decal_atlas.LAYOUT), list(zip([constants[k] for k in kinds], counts)))

    def test_every_cell_is_drawn_and_clear_at_its_border(self):
        for kind, variant, index in decal_atlas.cells():
            covered = 0
            for y in range(0, decal_atlas.CELL, 2):
                for x in range(0, decal_atlas.CELL, 2):
                    if self.alpha(index, x, y) > 40:
                        covered += 1
            share = covered / float((decal_atlas.CELL // 2) ** 2)
            self.assertGreater(share, 0.015, "%s %d is blank" % (kind, variant))
            self.assertLess(share, 0.8, "%s %d fills its cell" % (kind, variant))
            for t in range(decal_atlas.CELL):
                for x, y in ((t, 0), (t, decal_atlas.CELL - 1), (0, t), (decal_atlas.CELL - 1, t)):
                    self.assertEqual(0, self.alpha(index, x, y), "%s %d bleeds into its neighbour" % (kind, variant))

    def test_takes_of_one_kind_differ(self):
        for kind, count in decal_atlas.LAYOUT:
            if count < 2:
                continue
            first = decal_atlas.render_cell(kind, 0)
            second = decal_atlas.render_cell(kind, 1)
            self.assertNotEqual(first, second, kind)

    def test_cells_render_the_same_every_time(self):
        self.assertEqual(decal_atlas.render_cell("blood", 3), decal_atlas.render_cell("blood", 3))

    def test_committed_atlas_is_current(self):
        self.assertFalse(decal_atlas.stale(ROOT), "run python3 BlenderScripts/decal_atlas.py")

    def test_material_samples_the_atlas(self):
        with open(os.path.join(ROOT, "Assets", "Resources", "Decals", "DecalAtlas.mat")) as handle:
            material = handle.read()
        self.assertIn("guid: " + decal_atlas.guid(), material)


if __name__ == "__main__":
    unittest.main()
