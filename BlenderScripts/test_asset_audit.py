"""A missing texture or an extra FBX has to name itself."""

import os
import shutil
import tempfile
import unittest

from asset_audit import audit, repo_root, triangle_count


class AssetAuditTests(unittest.TestCase):
    def test_the_committed_set_is_complete(self):
        self.assertEqual(audit(), [])

    def test_floor_tile_is_a_handful_of_triangles(self):
        path = os.path.join(repo_root(), "Assets", "Models", "Kit", "Kit_floor.fbx")
        self.assertEqual(triangle_count(path), 12)

    def test_a_deleted_texture_is_named(self):
        root = repo_root()
        relative = "Assets/Models/Kit/Kit_floor.fbx"
        with tempfile.TemporaryDirectory() as folder:
            self.copy_set(root, folder, relative)
            os.remove(os.path.join(folder, "Assets", "Models", "Kit", "Kit_floor_Albedo.png"))
            manifest = {
                "assets": [relative],
                "textures": ["Albedo", "Normal", "AO", "Mask", "Icon"],
                "textureSize": 128,
            }
            problems = audit(folder, manifest)
            self.assertIn("Assets/Models/Kit/Kit_floor_Albedo missing texture", problems)

    def test_an_orphan_fbx_is_named(self):
        root = repo_root()
        relative = "Assets/Models/Kit/Kit_floor.fbx"
        with tempfile.TemporaryDirectory() as folder:
            self.copy_set(root, folder, relative)
            orphan = os.path.join(folder, "Assets", "Models", "Kit", "Kit_extra.fbx")
            with open(orphan, "wb") as handle:
                handle.write(b"fbx")
            manifest = {
                "assets": [relative],
                "textures": ["Albedo", "Normal", "AO", "Mask", "Icon"],
                "textureSize": 128,
            }
            problems = audit(folder, manifest)
            self.assertIn("Assets/Models/Kit/Kit_extra.fbx orphan fbx", problems)

    def copy_set(self, root, folder, relative):
        stem = relative[:-4]
        names = [relative, relative + ".meta"]
        for suffix in ("Albedo", "Normal", "AO", "Mask", "Icon"):
            names.append(stem + "_" + suffix + ".png")
            names.append(stem + "_" + suffix + ".png.meta")
        for name in names:
            source = os.path.join(root, name)
            dest = os.path.join(folder, name)
            os.makedirs(os.path.dirname(dest), exist_ok=True)
            shutil.copy(source, dest)


if __name__ == "__main__":
    unittest.main()
