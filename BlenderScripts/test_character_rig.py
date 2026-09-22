"""The character rig description has to be a valid humanoid before Blender runs."""

import unittest

from character_rig import REQUIRED_BONES, SURVIVOR_CLIPS, WALKER_CLIPS, bone_names, clips_for, humanoid_bones, locomotion_clips


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

    def test_each_role_has_its_action_list(self):
        known = set(bone_names())
        survivor = clips_for("Survivor_Leader")
        for name in SURVIVOR_CLIPS:
            self.assertIn(name, survivor)
        self.assertEqual(set(locomotion_clips()), set(survivor))
        walker = clips_for("Zombie_Walker")
        for name in WALKER_CLIPS:
            self.assertIn(name, walker)
        spine = [rotation[0] for bone, _frame, rotation in walker["Idle"] if bone == "Spine"]
        self.assertGreaterEqual(min(spine), 12.0)
        self.assertIn("Lunge", clips_for("Zombie_Runner"))
        brute = clips_for("Zombie_Brute")
        self.assertIn("Charge", brute)
        self.assertIn("Roar", brute)
        merchant = clips_for("NPC_Merchant")
        self.assertIn("Work", merchant)
        self.assertIn("Talk", merchant)
        for clips in (survivor, walker, brute, merchant, clips_for("Colonist_Survivor")):
            for keys in clips.values():
                for bone, _frame, _rotation in keys:
                    self.assertIn(bone, known)


if __name__ == "__main__":
    unittest.main()
