import os
import unittest

import codex_icons as ci
import icon_render
import texture_set


class CodexIconsTests(unittest.TestCase):
    def test_icon_path_sits_beside_the_model(self):
        self.assertEqual(os.path.join("Assets", "Models", "Characters", "Zombie_Walker_Icon.png"), ci.icon_path("Characters/Zombie_Walker.fbx"))

    def test_render_lists_each_icon_as_a_texture(self):
        text = ci.render([("Zombie_Walker", "b" * 32)])
        self.assertIn("m_Script: {fileID: 11500000, guid: " + ci.CODEX_ICONS_GUID + ", type: 3}", text)
        self.assertIn("  - id: Zombie_Walker\n    icon: {fileID: 2800000, guid: " + "b" * 32 + ", type: 3}\n", text)

    def test_every_codex_model_has_an_icon_and_the_asset_is_current(self):
        root = ci.repo_root()
        found, missing = ci.entries(root)
        self.assertEqual([], missing)
        ids = [model_id for model_id, _ in found]
        for model_id in ("Zombie_Walker", "Zombie_Runner", "Zombie_Brute", "NPC_Merchant",
                         "Base_Generator_Diesel", "Barricade_Wood_Wire", "Base_MedicalCot"):
            self.assertIn(model_id, ids)
        with open(os.path.join(root, ci.ASSET), encoding="utf-8") as handle:
            self.assertEqual(ci.render(found), handle.read())

    def test_codex_icons_are_mesh_renders(self):
        root = ci.repo_root()
        for model_id, _ in ci.entries(root)[0]:
            path = os.path.join(root, "Assets", "Models")
            matches = [os.path.join(d, f) for d, _, files in os.walk(path) for f in files if f == model_id + "_Icon.png"]
            self.assertEqual(1, len(matches), model_id)
            width, height, pixels = texture_set.decode_png(open(matches[0], "rb").read())
            self.assertEqual((icon_render.ICON_RENDER_SIZE, icon_render.ICON_RENDER_SIZE), (width, height), model_id)
            alpha = [pixels[i + 3] for i in range(0, len(pixels), 4)]
            self.assertIn(0, alpha, model_id + " has no transparent background")
            self.assertTrue(any(a > 0 for a in alpha), model_id + " is blank")

    def test_script_guid_matches_the_meta(self):
        root = ci.repo_root()
        with open(os.path.join(root, "Assets/Scripts/Shell/CodexIcons.cs.meta"), encoding="utf-8") as handle:
            self.assertEqual(ci.CODEX_ICONS_GUID, ci.meta_guid(handle.read()))


if __name__ == "__main__":
    unittest.main()
