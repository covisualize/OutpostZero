"""The asset manifest must point at real FBX files. No Blender required."""

import json
import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pipeline_plan import asset_paths


def repo_root():
    return os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))


class ManifestTests(unittest.TestCase):
    def test_every_manifest_asset_exists(self):
        root = repo_root()
        with open(os.path.join(root, "BlenderScripts", "assets.manifest.json"), encoding="utf-8") as handle:
            manifest = json.load(handle)
        self.assertGreaterEqual(tuple(int(part) for part in manifest["blender"].split(".")[:2]), (4, 2))
        self.assertGreater(len(asset_paths(manifest)), 30)
        missing = [path for path in asset_paths(manifest) if not os.path.isfile(os.path.join(root, path))]
        self.assertEqual(missing, [])

    def test_street_lamp_uses_the_prop_prefix(self):
        root = repo_root()
        with open(os.path.join(root, "BlenderScripts", "assets.manifest.json"), encoding="utf-8") as handle:
            manifest = json.load(handle)
        self.assertIn("Assets/Models/Props/Prop_StreetLamp.fbx", asset_paths(manifest))
        self.assertFalse(os.path.isfile(os.path.join(root, "Assets", "Models", "Props", "StreetLamp.fbx")))

    def test_exported_meshes_have_uvs_and_characters_are_rigged(self):
        root = repo_root()
        with open(os.path.join(root, "BlenderScripts", "assets.manifest.json"), encoding="utf-8") as handle:
            manifest = json.load(handle)
        bare = []
        unrigged = []
        for relative in asset_paths(manifest):
            path = os.path.join(root, relative)
            with open(path, "rb") as handle:
                text = handle.read().decode("latin1", errors="ignore")
            if "LayerElementUV" not in text:
                bare.append(relative)
            if "/Characters/" in relative and "Hips" not in text:
                unrigged.append(relative)
        self.assertEqual(bare, [])
        self.assertEqual(unrigged, [])

    def test_character_takes_include_combat_and_role_clips(self):
        root = repo_root()
        expected = {
            "Assets/Models/Characters/Survivor_Leader.fbx": ("Sprint", "CrouchIdle", "Reload", "ReloadShotgun", "ReloadRifle", "Death", "Takedown"),
            "Assets/Models/Characters/Colonist_Survivor.fbx": ("Sprint", "Reload", "Melee", "Work", "Talk"),
            "Assets/Models/Characters/Zombie_Walker.fbx": ("Shamble", "WalkB", "Attack", "DeathC", "Dissolve"),
            "Assets/Models/Characters/Zombie_Runner.fbx": ("Lunge",),
            "Assets/Models/Characters/Zombie_Brute.fbx": ("Charge", "Roar"),
            "Assets/Models/Characters/NPC_Merchant.fbx": ("Work", "Talk"),
        }
        missing = []
        for relative, names in expected.items():
            path = os.path.join(root, relative)
            with open(path, "rb") as handle:
                text = handle.read().decode("latin1", errors="ignore")
            for name in names:
                if name not in text:
                    missing.append(relative + ":" + name)
        self.assertEqual(missing, [])


if __name__ == "__main__":
    unittest.main()
