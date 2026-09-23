"""The UV check has to read the committed FBXs and see an overlap when islands stack."""

import glob
import os
import unittest

import fbx_uv
from asset_audit import repo_root

LEFT = ((0.0, 0.0), (0.5, 0.0), (0.0, 0.5))
RIGHT = ((0.6, 0.6), (1.0, 0.6), (0.6, 1.0))


class FbxUvTests(unittest.TestCase):
    def test_a_committed_prop_decodes_to_triangles_inside_the_unit_square(self):
        path = os.path.join(repo_root(), "Assets", "Models", "Props", "Prop_Crate_Wood.fbx")
        with open(path, "rb") as handle:
            meshes = fbx_uv.layouts(handle.read())
        self.assertEqual(len(meshes), 1)
        name, triangles = meshes[0]
        self.assertTrue(triangles)
        for triangle in triangles:
            for u, v in triangle:
                self.assertTrue(-0.001 <= u <= 1.001 and -0.001 <= v <= 1.001, (name, u, v))

    def test_separate_islands_do_not_overlap(self):
        covered, doubled = fbx_uv.overlap([LEFT, RIGHT], 64)
        self.assertGreater(covered, 0)
        self.assertEqual(doubled, 0)

    def test_a_stacked_island_is_all_overlap(self):
        covered, doubled = fbx_uv.overlap([LEFT, LEFT], 64)
        self.assertEqual(doubled, covered)

    def test_a_shifted_copy_overlaps_after_wrapping(self):
        shifted = tuple((u + 1.0, v) for u, v in LEFT)
        covered, doubled = fbx_uv.overlap([LEFT, shifted], 64)
        self.assertEqual(doubled, covered)

    def test_winding_does_not_hide_a_triangle(self):
        flipped = (LEFT[0], LEFT[2], LEFT[1])
        self.assertEqual(fbx_uv.overlap([LEFT], 64), fbx_uv.overlap([flipped], 64))

    def test_problems_name_the_mesh_and_share(self):
        original = fbx_uv.report
        fbx_uv.report = lambda path: [("Crate", 100, 10, 0.1), ("Lid", 0, 0, None), ("Plank", 100, 1, 0.01)]
        try:
            found = fbx_uv.problems("x.fbx", "Assets/Models/x.fbx")
        finally:
            fbx_uv.report = original
        self.assertEqual(found, [
            "Assets/Models/x.fbx Crate UVs overlap on 10.0% of the layout",
            "Assets/Models/x.fbx Lid has no UV layer",
        ])

    def test_every_committed_mesh_is_unwrapped_without_overlap(self):
        root = repo_root()
        paths = sorted(glob.glob(os.path.join(root, "Assets", "Models", "**", "*.fbx"), recursive=True))
        self.assertTrue(paths)
        for path in paths:
            self.assertEqual(fbx_uv.problems(path, os.path.relpath(path, root)), [])


if __name__ == "__main__":
    unittest.main()
