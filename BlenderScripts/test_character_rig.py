"""The character rig description has to be a valid humanoid before Blender runs."""

import os
import re
import unittest

from character_rig import (
    GAIT_CLIPS,
    REQUIRED_BONES,
    SURVIVOR_CLIPS,
    WALKER_CLIPS,
    bone_names,
    clips_for,
    cycle_frames,
    ground_speed,
    ground_speeds,
    humanoid_bones,
    locomotion_clips,
)

ROLES = ("Survivor_Leader", "Zombie_Walker", "Zombie_Runner", "Zombie_Brute", "NPC_Merchant")
GAIT_SHEET = os.path.join(os.path.dirname(__file__), "..", "Assets", "Scripts", "Core", "StrideSheet.cs")


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
        colonist = clips_for("Colonist_Survivor")
        self.assertIn("Work", colonist)
        self.assertIn("Talk", colonist)
        for name in SURVIVOR_CLIPS:
            self.assertIn(name, colonist)
        for clips in (survivor, walker, brute, merchant, clips_for("Colonist_Survivor")):
            for keys in clips.values():
                for bone, _frame, _rotation in keys:
                    self.assertIn(bone, known)

    def test_every_gait_clip_loops_on_its_stride(self):
        for role in ROLES:
            for name, keys in clips_for(role).items():
                if name not in GAIT_CLIPS:
                    continue
                legs = [frame for bone, frame, _rotation in keys if bone == "LeftUpperLeg"]
                stride = max(legs) - min(legs)
                self.assertEqual(stride, cycle_frames(keys), role + " " + name + " holds still after its last step")

    def test_ground_speed_follows_the_swing(self):
        wide = (("LeftUpperLeg", 1, (30.0, 0.0, 0.0)), ("LeftUpperLeg", 9, (-30.0, 0.0, 0.0)), ("LeftUpperLeg", 17, (30.0, 0.0, 0.0)))
        self.assertAlmostEqual(ground_speed(wide, fps=24, leg=1.0), 4.0 * 0.5 / (16.0 / 24.0))
        self.assertEqual(ground_speed((("Spine", 1, (0.0, 0.0, 0.0)), ("Spine", 9, (4.0, 0.0, 0.0)))), 0.0)
        survivor = ground_speeds("Survivor_Leader")
        self.assertGreater(survivor["Sprint"], survivor["Walk"])
        self.assertGreater(survivor["Walk"], survivor["CrouchWalk"])
        self.assertNotIn("Lunge", ground_speeds("Zombie_Runner"))

    def test_the_runtime_stride_table_matches_the_rig(self):
        with open(GAIT_SHEET, encoding="utf-8") as handle:
            text = handle.read()
        rows = re.findall(r'Speed\("(\w+)", "(\w+)", ([0-9.]+)f\)', text)
        self.assertTrue(rows)
        seen = set()
        for family, clip, value in rows:
            role = {"Survivor": "Survivor_Leader", "Zombie": "Zombie_Walker"}.get(family, family)
            self.assertAlmostEqual(float(value), ground_speeds(role)[clip], places=3, msg=family + " " + clip)
            seen.add((role, clip))
        for role in ("Survivor_Leader", "Zombie_Walker", "Zombie_Brute"):
            for clip in ground_speeds(role):
                if role == "Zombie_Brute" and clip != "Charge":
                    continue
                self.assertIn((role, clip), seen, "StrideSheet is missing " + role + " " + clip)
        for role in ("Zombie_Runner", "Zombie_Brute"):
            for clip in ("Walk", "Shamble", "Sprint"):
                self.assertAlmostEqual(ground_speeds(role)[clip], ground_speeds("Zombie_Walker")[clip])


if __name__ == "__main__":
    unittest.main()
