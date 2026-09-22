import os
import unittest

import prefab_tags as tags

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
PREFAB = """--- !u!1001 &1
PrefabInstance:
  m_Modification:
    m_Modifications:
    - target: {fileID: 919132149155446097, guid: abc, type: 3}
      propertyPath: m_Name
      value: Thing
      objectReference: {fileID: 0}
    m_RemovedComponents: []
    m_RemovedGameObjects: []
    m_AddedGameObjects: []
    m_AddedComponents: []
  m_SourcePrefab: {fileID: 100100000, guid: abc, type: 3}
--- !u!1 &22 stripped
GameObject:
  m_CorrespondingSourceObject: {fileID: 919132149155446097, guid: abc, type: 3}
"""


class PrefabTagTests(unittest.TestCase):
    def test_guess_matches_the_csharp_cases(self):
        self.assertEqual(tags.guess("Characters", ["Mat_Metal_Buckles"]), "Flesh")
        self.assertEqual(tags.guess("Props", ["Mat_Concrete_Weathered", "Mat_Rebar_Rusted"]), "Concrete")
        self.assertEqual(tags.guess("Props", ["Mat_Sandbag_Canvas"]), "Gravel")
        self.assertEqual(tags.guess("Kit", ["Mat_Wood_Planks", "Mat_Wood_Nails"]), "Wood")
        self.assertEqual(tags.guess("Kit", ["Mat_Item_Grey"]), "Concrete")
        self.assertEqual(tags.guess("Unknown", None), "Default")

    def test_layers_follow_category_and_id(self):
        self.assertEqual(tags.layer_for("Characters", "Zombie_Brute"), 7)
        self.assertEqual(tags.layer_for("Characters", "Survivor_Leader"), 0)
        self.assertEqual(tags.layer_for("Weapons", "Loot_Flare"), 9)
        self.assertEqual(tags.layer_for("Weapons", "Weapon_Machete"), 0)
        self.assertEqual(tags.layer_for("Kit", "Kit_floor"), 8)

    def test_rewrite_adds_then_updates_in_place(self):
        once = tags.rewrite(PREFAB, "Assets/Prefabs/Kit/Thing.prefab", 8, 1)
        self.assertEqual(tags.read_state(once), (8, 1))
        self.assertIn("m_GameObject: {fileID: 22}", once)
        self.assertIn("addedObject: {fileID: " + tags.component_id("Assets/Prefabs/Kit/Thing.prefab") + "}", once)
        twice = tags.rewrite(once, "Assets/Prefabs/Kit/Thing.prefab", 0, 3)
        self.assertEqual(tags.read_state(twice), (0, 3))
        self.assertEqual(twice.count("MonoBehaviour:"), 1)
        self.assertNotIn("m_Layer", twice)

    def test_committed_prefabs_are_tagged(self):
        self.assertEqual(tags.audit(ROOT), [])


if __name__ == "__main__":
    unittest.main()
