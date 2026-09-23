import os
import unittest

import prefab_colliders as colliders

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SIDECAR = {"size": {"width": 0.8, "depth": 0.4, "height": 1.8}, "floor": 0.0, "center": {"x": 0.1, "y": -0.2}}


def prefab(size, center, w="1", x="0", y_offset="0"):
    root = "- target: {fileID: " + colliders.ROOT_TRANSFORM + ", guid: abc, type: 3}\n"
    return (
        root + "      propertyPath: m_LocalPosition.y\n      value: " + y_offset + "\n"
        + root + "      propertyPath: m_LocalRotation.w\n      value: " + w + "\n"
        + root + "      propertyPath: m_LocalRotation.x\n      value: " + x + "\n"
        + "BoxCollider:\n  m_Size: {x: %s, y: %s, z: %s}\n  m_Center: {x: %s, y: %s, z: %s}\n" % (size + center)
    )


class PrefabColliderTests(unittest.TestCase):
    def test_unity_axes_mirror_blender_x_and_y(self):
        middle, size = colliders.expected_box(SIDECAR, (0.0, 0.0, 0.0))
        self.assertEqual(size, (0.8, 1.8, 0.4))
        self.assertAlmostEqual(middle[0], -0.1)
        self.assertAlmostEqual(middle[1], 0.9)
        self.assertAlmostEqual(middle[2], 0.2)

    def test_a_pose_sized_box_is_drift_and_fix_settles_it(self):
        text = prefab(("1.9", "1.66", "2.48"), ("0", "0.9", "0.2"))
        self.assertTrue(any(p.startswith("size.z") for p in colliders.drift(text, SIDECAR)))
        repaired = colliders.fixed(text, SIDECAR)
        self.assertEqual(colliders.drift(repaired, SIDECAR), [])
        self.assertEqual(colliders.fixed(repaired, SIDECAR), repaired)

    def test_the_root_offset_moves_the_expected_centre(self):
        text = prefab(("0.8", "1.8", "0.4"), ("-0.1", "0.8", "0.2"), y_offset="0.1")
        self.assertEqual(colliders.drift(text, SIDECAR), [])

    def test_a_quarter_turn_is_reported_and_fix_clears_it(self):
        text = prefab(("0.8", "1.8", "0.4"), ("-0.1", "0.9", "0.2"), w="0.7071068", x="0.7071068")
        self.assertEqual(colliders.drift(text, SIDECAR), ["root turned 90 degrees"])
        self.assertEqual(colliders.drift(colliders.fixed(text, SIDECAR), SIDECAR), [])

    def test_set_dressing_tilts_are_left_alone(self):
        text = prefab(("9", "9", "9"), ("0", "0", "0"), w="0.9986295")
        self.assertEqual(colliders.drift(text, SIDECAR), [])

    def test_committed_prefabs_match_their_sidecars(self):
        self.assertEqual(colliders.audit(ROOT), [])


if __name__ == "__main__":
    unittest.main()
