import os
import unittest

import kit_prefab_set as kps


VARIANT = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1001 &424822908469634359
PrefabInstance:
  m_ObjectHideFlags: 0
--- !u!1 &111 stripped
GameObject:
  m_CorrespondingSourceObject: {fileID: 555, guid: 61bcc63bb8e43caf1a4a81e9f86b4e28, type: 3}
--- !u!1 &658704217024419942 stripped
GameObject:
  m_CorrespondingSourceObject: {fileID: 919132149155446097, guid: 61bcc63bb8e43caf1a4a81e9f86b4e28, type: 3}
  m_PrefabInstance: {fileID: 424822908469634359}
"""


class KitPrefabSetTests(unittest.TestCase):
    def test_root_object_is_the_stripped_model_root(self):
        self.assertEqual("658704217024419942", kps.root_object(VARIANT))
        self.assertIsNone(kps.root_object("--- !u!1 &5\nGameObject:\n"))

    def test_meta_guid(self):
        self.assertEqual("c75b3bf8091a2b3c4d5e6f708192a469", kps.meta_guid("fileFormatVersion: 2\nguid: c75b3bf8091a2b3c4d5e6f708192a469\n"))
        self.assertIsNone(kps.meta_guid("fileFormatVersion: 2\n"))

    def test_render_lists_each_entry(self):
        text = kps.render([("shelf", "658704217024419942", "a" * 32)])
        self.assertIn("m_Script: {fileID: 11500000, guid: " + kps.KIT_PREFAB_SET_GUID + ", type: 3}", text)
        self.assertIn("  - id: shelf\n    prefab: {fileID: 658704217024419942, guid: " + "a" * 32 + ", type: 3}\n", text)

    def test_every_catalog_piece_has_a_prefab_and_the_asset_is_current(self):
        root = kps.repo_root()
        found, missing = kps.entries(root)
        self.assertEqual([], missing)
        self.assertGreaterEqual(len(found), 50)
        with open(os.path.join(root, kps.ASSET), encoding="utf-8") as handle:
            self.assertEqual(kps.render(found), handle.read())

    def test_script_guid_matches_the_meta(self):
        root = kps.repo_root()
        with open(os.path.join(root, "Assets/Scripts/Expedition/KitPrefabSet.cs.meta"), encoding="utf-8") as handle:
            self.assertEqual(kps.KIT_PREFAB_SET_GUID, kps.meta_guid(handle.read()))


if __name__ == "__main__":
    unittest.main()
