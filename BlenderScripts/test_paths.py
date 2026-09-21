"""Path tests that run with the system Python. No Blender required."""

import os
import tempfile
import unittest

from blender_paths import apply_cli_overrides, get_repo_root, models_dir


class BlenderPathTests(unittest.TestCase):
    def test_repo_root_finds_manifest(self):
        root = get_repo_root()
        self.assertTrue(os.path.isfile(os.path.join(root, "Packages", "manifest.json")))

    def test_models_dir_is_inside_the_repo(self):
        path = models_dir("Props")
        self.assertTrue(path.replace("\\", "/").endswith("Assets/Models/Props"))
        self.assertTrue(os.path.isdir(path))

    def test_env_override(self):
        previous = os.environ.get("OUTPOST_MODELS_DIR")
        with tempfile.TemporaryDirectory() as temp:
            os.environ["OUTPOST_MODELS_DIR"] = temp
            try:
                path = models_dir("Weapons")
                self.assertEqual(os.path.abspath(path), os.path.abspath(os.path.join(temp, "Weapons")))
            finally:
                if previous is None:
                    os.environ.pop("OUTPOST_MODELS_DIR", None)
                else:
                    os.environ["OUTPOST_MODELS_DIR"] = previous

    def test_cli_only_and_models_dir(self):
        previous = os.environ.get("OUTPOST_MODELS_DIR")
        try:
            only = apply_cli_overrides(
                ["blender", "-b", "-P", "build_all_assets.py", "--", "--only", "characters,weapons", "--models-dir", "/tmp/oz-models"]
            )
            self.assertEqual(only, ["characters", "weapons"])
            self.assertEqual(os.environ["OUTPOST_MODELS_DIR"], os.path.abspath("/tmp/oz-models"))
        finally:
            if previous is None:
                os.environ.pop("OUTPOST_MODELS_DIR", None)
            else:
                os.environ["OUTPOST_MODELS_DIR"] = previous


if __name__ == "__main__":
    unittest.main()
