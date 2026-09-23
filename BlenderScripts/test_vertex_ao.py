import glob
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(__file__))

import fbx_uv  # noqa: E402
import vertex_ao  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))


def never(_start, _direction, _reach):
    return False


def always(_start, _direction, _reach):
    return True


class VertexAoTests(unittest.TestCase):
    def test_fan_is_unit_and_balanced(self):
        directions = vertex_ao.fan()
        self.assertEqual(vertex_ao.RAYS, len(directions))
        for d in directions:
            self.assertAlmostEqual(1.0, sum(c * c for c in d), places=9)
        for axis in range(3):
            self.assertLess(abs(sum(d[axis] for d in directions)), 0.2)
        self.assertEqual(directions, vertex_ao.fan())

    def test_open_sky_is_full_and_a_closed_box_is_dark(self):
        self.assertEqual(1.0, vertex_ao.visibility((0, 0, 2), (0, 0, 1), never))
        self.assertEqual(0.0, vertex_ao.visibility((0, 0, 2), (0, 0, 1), always))
        self.assertEqual(vertex_ao.DARKEST, vertex_ao.shade(0.0))
        self.assertEqual(1.0, vertex_ao.shade(1.0))
        self.assertEqual(255, vertex_ao.to_byte(vertex_ao.shade(1.0)))

    def test_the_floor_shades_what_stands_on_it(self):
        side_at_foot = vertex_ao.visibility((0, 0, 0.02), (1, 0, 0), never)
        side_up_high = vertex_ao.visibility((0, 0, 3.0), (1, 0, 0), never)
        self.assertLess(side_at_foot, 0.75)
        self.assertEqual(1.0, side_up_high)
        self.assertTrue(vertex_ao.floor_hit((0, 0, 0.1), (0, 0, -1)))
        self.assertFalse(vertex_ao.floor_hit((0, 0, 0.1), (0, 0, 1)))
        self.assertFalse(vertex_ao.floor_hit((0, 0, 2.0), (0, 0, -1)))

    def test_rays_weight_by_the_cosine(self):
        def overhead(_start, direction, _reach):
            return direction[2] > 0.7

        grazing = vertex_ao.visibility((0, 0, 2), (0, 0, 1), overhead)
        self.assertGreater(grazing, 0.0)
        self.assertLess(grazing, 0.5)

    def test_every_committed_model_carries_the_ao_layer(self):
        models = sorted(glob.glob(os.path.join(ROOT, "Assets", "Models", "**", "*.fbx"), recursive=True))
        self.assertGreater(len(models), 100)
        shaded = 0
        for path in models:
            relative = os.path.relpath(path, ROOT)
            self.assertEqual([], fbx_uv.colour_problems(path, relative))
            with open(path, "rb") as handle:
                data = handle.read()
            for top in fbx_uv.parse(data):
                if top.name != "Objects":
                    continue
                for geometry in top.all("Geometry"):
                    layer = geometry.child("LayerElementColor")
                    if layer is None:
                        continue
                    reds = layer.child("Colors").props[0][0::4]
                    self.assertGreaterEqual(min(reds), vertex_ao.DARKEST - 0.01, relative)
                    self.assertLessEqual(max(reds), 1.0, relative)
                    if min(reds) < 0.95:
                        shaded += 1
        self.assertGreater(shaded, len(models) // 2, "most models get some contact shade")


if __name__ == "__main__":
    unittest.main()
