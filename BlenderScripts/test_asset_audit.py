"""A missing texture or an extra FBX has to name itself."""

import json
import os
import shutil
import tempfile
import unittest

from asset_audit import audit, load_manifest, repo_root, triangle_count


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
            manifest = self.floor_manifest()
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
            manifest = self.floor_manifest()
            problems = audit(folder, manifest)
            self.assertIn("Assets/Models/Kit/Kit_extra.fbx orphan fbx", problems)

    def test_a_stale_sidecar_is_named(self):
        root = repo_root()
        relative = "Assets/Models/Kit/Kit_floor.fbx"
        with tempfile.TemporaryDirectory() as folder:
            self.copy_set(root, folder, relative)
            manifest = self.floor_manifest()
            manifest["entries"][0]["collider"] = "mesh"
            problems = audit(folder, manifest)
            self.assertIn("Assets/Models/Kit/Kit_floor.fbx sidecar collider is stale", problems)
            os.remove(os.path.join(folder, "Assets", "Models", "Kit", "Kit_floor.meta.json"))
            self.assertIn("Assets/Models/Kit/Kit_floor.fbx missing sidecar", audit(folder, manifest))

    def test_sidecar_triangles_add_up_to_the_fbx(self):
        root = repo_root()
        path = os.path.join(root, "Assets", "Models", "Props", "Vehicle_Wrecked_Sedan")
        with open(path + ".meta.json", encoding="utf-8") as handle:
            record = json.load(handle)
        self.assertEqual(len(record["lodTris"]), 2)
        self.assertLess(record["lodTris"][1], record["lodTris"][0])
        self.assertEqual(sum(record["lodTris"]), triangle_count(path + ".fbx"))

    def floor_manifest(self):
        manifest = load_manifest(repo_root())
        manifest["entries"] = [entry for entry in manifest["entries"] if entry["id"] == "Kit_floor"]
        return manifest

    def copy_set(self, root, folder, relative):
        stem = relative[:-4]
        names = [relative, relative + ".meta", stem + ".meta.json", stem + ".meta.json.meta"]
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
