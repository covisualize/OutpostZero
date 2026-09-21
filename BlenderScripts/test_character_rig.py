"""The character rig description has to be a valid humanoid before Blender runs."""

import unittest

from character_rig import REQUIRED_BONES, bone_names, humanoid_bones, locomotion_clips


class CharacterRigTests(unittest.TestCase):
    def test_required_bones_form_a_tree(self):
        names = bone_names()
        self.assertEqual(len(names), len(set(names)))
        parents = {}
        for name, parent, _head, _tail in humanoid_bones():
            parents[name] = parent
            if parent is not None:
                self.assertIn(parent, parents)
        for required in REQUIRED_BONES:
            self.assertIn(required, names)
        self.assertIsNone(parents["Hips"])

    def test_clips_only_move_known_bones(self):
        known = set(bone_names())
        clips = locomotion_clips()
        self.assertIn("Idle", clips)
        self.assertIn("Walk", clips)
        frames = set()
        for keys in clips.values():
            self.assertGreaterEqual(len(keys), 2)
            for bone, frame, rotation in keys:
                self.assertIn(bone, known)
                self.assertGreaterEqual(frame, 1)
                self.assertEqual(len(rotation), 3)
                frames.add(frame)
        self.assertGreaterEqual(len(frames), 2)


if __name__ == "__main__":
    unittest.main()
