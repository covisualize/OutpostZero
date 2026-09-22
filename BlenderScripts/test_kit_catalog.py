"""The snap kit can assemble an enterable block without a new mesh generator."""

import json
import os
import unittest

from kit_catalog import (
    DOOR_CLEAR,
    SURFACES,
    catalog,
    catalog_path,
    clear_at,
    opening_width,
    recipe_for,
    storeys,
    unknown_ids,
    write_catalog,
)


class KitCatalogTests(unittest.TestCase):
    def setUp(self):
        self.book = catalog()
        self.pieces = {item["id"]: item for item in self.book["pieces"]}

    def test_kit_covers_building_road_interior_and_fort(self):
        categories = {item["category"] for item in self.book["pieces"]}
        self.assertEqual(categories, {"building", "road", "interior", "fort"})
        self.assertGreaterEqual(len(self.book["pieces"]), 45)
        self.assertEqual(self.book["grid"], 2)
        self.assertGreaterEqual(self.book["doorClear"], DOOR_CLEAR)
        for item in self.book["pieces"]:
            self.assertIn(item["surface"], SURFACES)
            self.assertGreaterEqual(item["lod"], 12)
            self.assertGreater(len(item["colliders"]), 0)

    def test_doors_leave_a_walkable_gap(self):
        for piece_id in ("wall_door", "wall_double_door", "wall_garage", "fence_gate", "plywood_door"):
            item = self.pieces[piece_id]
            self.assertGreaterEqual(item["door"], DOOR_CLEAR)
            self.assertGreaterEqual(opening_width(item), item["door"] * 0.9)
            self.assertTrue(clear_at(item, item["w"] * 0.5, 1.0, item["d"] * 0.5))
            self.assertFalse(clear_at(item, 0.05, 1.0, item["d"] * 0.5))
        self.assertFalse(clear_at(self.pieces["wall_plain"], 1.0, 1.0, 0.1))
        self.assertTrue(clear_at(self.pieces["wall_window_broken"], 1.0, 1.5, 0.1))
        self.assertFalse(clear_at(self.pieces["wall_window"], 1.0, 1.5, 0.1))

    def test_apartment_is_three_storeys_of_known_pieces(self):
        for name in ("storefront", "warehouse", "hospital", "apartment", "edge"):
            missing = unknown_ids(self.book["pieces"], self.book[name])
            self.assertEqual(missing, [], name)
        self.assertEqual(storeys(self.book["apartment"]), [0, 3, 6])
        self.assertIn("wall_door", {item["id"] for item in self.book["apartment"]})
        self.assertIn("stairs", {item["id"] for item in self.book["apartment"]})
        self.assertIn("shelf", {item["id"] for item in self.book["storefront"]})
        self.assertIn("wall_garage", {item["id"] for item in self.book["warehouse"]})
        self.assertIn("hospital_bed", {item["id"] for item in self.book["hospital"]})
        self.assertEqual(recipe_for("ash_market"), "storefront")
        self.assertEqual(recipe_for("rail_yard"), "warehouse")
        self.assertEqual(recipe_for("old_hospital"), "hospital")
        self.assertEqual(recipe_for("north_gate"), "apartment")
        self.assertEqual(recipe_for("anywhere"), "storefront")

    def test_committed_catalog_matches_the_builder(self):
        write_catalog()
        with open(catalog_path(), encoding="utf-8") as handle:
            saved = json.load(handle)
        self.assertEqual(saved["grid"], self.book["grid"])
        self.assertEqual([item["id"] for item in saved["pieces"]], [item["id"] for item in self.book["pieces"]])
        self.assertEqual(len(saved["apartment"]), len(self.book["apartment"]))
        self.assertTrue(os.path.isfile(catalog_path()))


if __name__ == "__main__":
    unittest.main()
