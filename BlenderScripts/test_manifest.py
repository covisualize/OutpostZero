"""The asset manifest must point at real FBX files. No Blender required."""

import json
import os
import unittest


def repo_root():
    return os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))


class ManifestTests(unittest.TestCase):
    def test_every_manifest_asset_exists(self):
        root = repo_root()
        with open(os.path.join(root, "BlenderScripts", "assets.manifest.json"), encoding="utf-8") as handle:
            manifest = json.load(handle)
        self.assertGreaterEqual(tuple(int(part) for part in manifest["blender"].split(".")[:2]), (4, 2))
        self.assertGreater(len(manifest["assets"]), 30)
        missing = [path for path in manifest["assets"] if not os.path.isfile(os.path.join(root, path))]
        self.assertEqual(missing, [])

    def test_street_lamp_uses_the_prop_prefix(self):
        root = repo_root()
        with open(os.path.join(root, "BlenderScripts", "assets.manifest.json"), encoding="utf-8") as handle:
            manifest = json.load(handle)
        self.assertIn("Assets/Models/Props/Prop_StreetLamp.fbx", manifest["assets"])
        self.assertFalse(os.path.isfile(os.path.join(root, "Assets", "Models", "Props", "StreetLamp.fbx")))


if __name__ == "__main__":
    unittest.main()
