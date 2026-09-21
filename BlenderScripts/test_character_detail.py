"""Character faces, eyes, and clothing tints stay recognisable and uncloned."""

import os
import unittest

from character_detail import (
    GORE_LINE,
    clothing_tint,
    eye_color,
    eye_strength,
    face_parts,
    glows,
    role_of,
    thumb_box,
    wounded,
)


class CharacterDetailTests(unittest.TestCase):
    def test_eyes_differ_by_role_and_stay_bright(self):
        walker = eye_color("walker")
        runner = eye_color("Zombie_Runner")
        brute = eye_color("brute")
        self.assertGreater(walker[1], walker[0])
        self.assertGreater(walker[0], walker[2])
        self.assertGreater(runner[0], runner[1])
        self.assertGreater(runner[0], runner[2])
        self.assertGreater(brute[0], brute[1])
        self.assertGreater(brute[1], brute[2])
        self.assertEqual(eye_strength("walker"), 2.5)
        self.assertEqual(eye_strength("runner"), 2.5)
        self.assertLess(eye_strength("brute"), eye_strength("walker"))
        self.assertTrue(glows("walker"))
        self.assertFalse(glows("survivor"))

    def test_face_and_thumb_sit_on_the_body(self):
        face, left, right = face_parts((0.05, 0.16, 1.54))
        self.assertEqual(face[0], "Face")
        self.assertEqual(left[0], "EyeL")
        self.assertEqual(right[0], "EyeR")
        self.assertGreater(left[1][1], 0.16)
        self.assertLess(left[1][0], right[1][0])
        thumb, size = thumb_box((-0.30, 0.48, 1.35), "L")
        self.assertLess(thumb[0], -0.30)
        self.assertEqual(len(size), 3)

    def test_three_bodies_do_not_share_a_tint(self):
        first = clothing_tint(1)
        second = clothing_tint(2)
        third = clothing_tint(3)
        self.assertNotEqual(first, second)
        self.assertNotEqual(second, third)
        for tint in (first, second, third):
            for channel in tint:
                self.assertGreater(channel, 0.4)
                self.assertLess(channel, 2.0)
        self.assertEqual(role_of("Zombie_Walker(Clone)"), "walker")
        self.assertAlmostEqual(GORE_LINE, 0.4)
        self.assertTrue(wounded(39, 100))
        self.assertFalse(wounded(40, 100))
        self.assertFalse(wounded(0, 0))

    def test_exported_characters_keep_face_eye_and_thumb_materials(self):
        root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "Assets", "Models", "Characters"))
        expected = {
            "Zombie_Walker.fbx": "Walker",
            "Zombie_Runner.fbx": "Runner",
            "Zombie_Brute.fbx": "Brute",
            "Survivor_Leader.fbx": "Survivor",
            "NPC_Merchant.fbx": "Merchant",
            "Colonist_Survivor.fbx": "Colonist",
        }
        for filename, label in expected.items():
            with open(os.path.join(root, filename), "rb") as handle:
                data = handle.read()
            self.assertIn(b"Hips", data)
            self.assertIn(("Mat_Eye_" + label).encode("ascii"), data)
            self.assertIn(("Mat_Face_" + label).encode("ascii"), data)
            self.assertIn(("Mat_Thumb_" + label).encode("ascii"), data)


if __name__ == "__main__":
    unittest.main()
