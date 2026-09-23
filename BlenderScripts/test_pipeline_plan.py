"""Manifest, selection, change detection, and sidecars for pipeline.py. No Blender required."""

import ast
import copy
import json
import os
import shutil
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

import pipeline_plan as plan
from kit_catalog import all_pieces


def repo_root():
    return os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))


def sandbox():
    """A throwaway repo with the real manifest and generator sources, so fingerprints can move."""
    root = tempfile.mkdtemp()
    scripts = os.path.join(root, "BlenderScripts")
    os.makedirs(scripts)
    for name in os.listdir(os.path.join(repo_root(), "BlenderScripts")):
        if name.endswith((".py", ".json")):
            shutil.copy(os.path.join(repo_root(), "BlenderScripts", name), scripts)
    return root


class ManifestSchemaTests(unittest.TestCase):
    def setUp(self):
        self.manifest = plan.load_manifest(repo_root())

    def test_the_committed_manifest_is_valid(self):
        self.assertEqual(plan.validate(self.manifest), [])
        self.assertEqual(self.manifest["schema"], 2)

    def test_every_entry_carries_the_full_schema(self):
        for entry in plan.entries(self.manifest):
            for field in ("id", "category", "generator", "output", "tags", "bake", "lods", "collider", "pivot"):
                self.assertIn(field, entry, entry["id"])
            self.assertEqual(set(entry["bake"]), {"uv", "textures", "size"})

    def test_entries_list_the_same_models_as_the_folders(self):
        paths = plan.asset_paths(self.manifest)
        self.assertEqual(len(paths), len(set(paths)))
        self.assertEqual(len(paths), 113)
        self.assertIn("Assets/Models/Props/Prop_Dumpster.fbx", paths)

    def test_every_generator_is_a_build_function_taking_ctx(self):
        scripts = os.path.join(repo_root(), "BlenderScripts")
        functions = {}
        for entry in plan.entries(self.manifest):
            module, name = entry["generator"].split(".")
            if module not in functions:
                with open(os.path.join(scripts, module + ".py"), encoding="utf-8") as handle:
                    tree = ast.parse(handle.read())
                functions[module] = {
                    node.name: [arg.arg for arg in node.args.args]
                    for node in tree.body if isinstance(node, ast.FunctionDef)
                }
            self.assertTrue(name.startswith("build_"), entry["generator"])
            self.assertEqual(functions[module].get(name), ["ctx"], entry["generator"])

    def test_no_generator_resets_or_exports_on_its_own(self):
        scripts = os.path.join(repo_root(), "BlenderScripts")
        modules = {entry["generator"].split(".")[0] for entry in plan.entries(self.manifest)}
        for module in modules:
            with open(os.path.join(scripts, module + ".py"), encoding="utf-8") as handle:
                source = handle.read()
            self.assertNotIn("reset_scene(", source, module)
            self.assertNotIn("export_fbx(", source, module)

    def test_kit_entries_match_the_catalog(self):
        kit = [entry["id"] for entry in plan.entries(self.manifest) if entry["category"] == "Kit"]
        self.assertEqual(kit, ["Kit_" + item["id"] for item in all_pieces()])

    def test_characters_are_rigged_and_keep_one_lod(self):
        for entry in plan.entries(self.manifest):
            if entry["category"] == "Characters":
                self.assertEqual(entry["rig"], "humanoid")
                self.assertEqual(entry["lods"], [1.0])
            else:
                self.assertNotIn("rig", entry)

    def test_large_set_pieces_carry_a_half_lod(self):
        lodded = sorted(entry["id"] for entry in plan.entries(self.manifest) if len(entry["lods"]) > 1)
        self.assertEqual(lodded, [
            "Building_Storefront_2Story", "Building_Warehouse_Depot",
            "Vehicle_Apocalypse_Truck", "Vehicle_Wrecked_Sedan",
        ])

    def test_validation_names_each_problem(self):
        broken = copy.deepcopy(self.manifest)
        broken["entries"][0]["collider"] = "sphere"
        broken["entries"][1]["pivot"] = "top"
        broken["entries"][2]["id"] = broken["entries"][3]["id"]
        broken["entries"][4]["output"] = "Props/Wrong.fbx"
        broken["entries"][5]["lods"] = [0.5, 1.0]
        broken["entries"][6]["generator"] = "no_dot"
        problems = "\n".join(plan.validate(broken))
        for fragment in ("collider must be", "pivot must be", "duplicate id", "output must be", "lods must", "module.function"):
            self.assertIn(fragment, problems)


class SelectionTests(unittest.TestCase):
    def setUp(self):
        self.manifest = plan.load_manifest(repo_root())

    def test_only_picks_named_assets_in_manifest_order(self):
        chosen = plan.select(self.manifest, only=["Prop_Dumpster", "Zombie_Walker"])
        self.assertEqual([entry["id"] for entry in chosen], ["Zombie_Walker", "Prop_Dumpster"])

    def test_unknown_ids_fail_loudly(self):
        with self.assertRaises(plan.PlanError):
            plan.select(self.manifest, only=["Prop_Nope"])

    def test_categories_are_case_blind_and_accept_old_phase_names(self):
        props = plan.select(self.manifest, categories=["props"])
        self.assertTrue(props and all(entry["category"] == "Props" for entry in props))
        base = plan.select(self.manifest, categories=["base"])
        self.assertTrue(base and all(entry["category"] == "BaseBuilding" for entry in base))
        with self.assertRaises(plan.PlanError):
            plan.select(self.manifest, categories=["hats"])

    def test_only_and_category_intersect(self):
        chosen = plan.select(self.manifest, only=["Prop_Dumpster", "Zombie_Walker"], categories=["Props"])
        self.assertEqual([entry["id"] for entry in chosen], ["Prop_Dumpster"])

    def test_arguments_after_the_blender_separator(self):
        options = plan.parse_args(["blender", "-b", "-P", "pipeline.py", "--", "--only", "Prop_Dumpster,Zombie_Walker", "--changed", "--dry-run"])
        self.assertEqual(options["only"], ["Prop_Dumpster", "Zombie_Walker"])
        self.assertTrue(options["changed"])
        self.assertTrue(options["dry_run"])
        with self.assertRaises(plan.PlanError):
            plan.parse_args(["--frobnicate"])


class ChangeDetectionTests(unittest.TestCase):
    def setUp(self):
        self.root = sandbox()
        self.manifest = plan.load_manifest(self.root)
        self.models = os.path.join(self.root, "Assets", "Models")
        cache = {}
        for entry in plan.entries(self.manifest):
            cache[entry["id"]] = plan.fingerprint(self.root, self.manifest, entry)
            path = plan.output_path(self.models, entry)
            os.makedirs(os.path.dirname(path), exist_ok=True)
            open(path, "wb").close()
        plan.save_cache(self.root, cache)

    def tearDown(self):
        shutil.rmtree(self.root, ignore_errors=True)

    def stale(self):
        chosen = plan.entries(self.manifest)
        return [entry["id"] for entry in plan.changed(self.root, self.manifest, chosen, plan.load_cache(self.root), self.models)]

    def test_nothing_is_stale_right_after_a_build(self):
        self.assertEqual(self.stale(), [])

    def test_editing_one_generator_module_only_rebuilds_its_assets(self):
        path = os.path.join(self.root, "BlenderScripts", "generate_weapons.py")
        with open(path, "a", encoding="utf-8") as handle:
            handle.write("\n# tweak\n")
        weapons = [entry["id"] for entry in plan.entries(self.manifest) if entry["generator"].startswith("generate_weapons.")]
        self.assertEqual(self.stale(), weapons)

    def test_editing_shared_code_rebuilds_everything(self):
        with open(os.path.join(self.root, "BlenderScripts", "blender_utils.py"), "a", encoding="utf-8") as handle:
            handle.write("\n# tweak\n")
        self.assertEqual(len(self.stale()), 113)

    def test_editing_one_entry_only_rebuilds_that_entry(self):
        self.manifest["entries"][0]["collider"] = "mesh"
        self.assertEqual(self.stale(), [self.manifest["entries"][0]["id"]])

    def test_a_missing_fbx_is_rebuilt_even_when_the_hash_matches(self):
        entry = plan.select(self.manifest, only=["Prop_Dumpster"])[0]
        os.remove(plan.output_path(self.models, entry))
        self.assertEqual(self.stale(), ["Prop_Dumpster"])

    def test_windows_line_endings_do_not_count_as_a_change(self):
        path = os.path.join(self.root, "BlenderScripts", "generate_props.py")
        with open(path, "rb") as handle:
            data = handle.read()
        with open(path, "wb") as handle:
            handle.write(data.replace(b"\n", b"\r\n"))
        self.assertEqual(self.stale(), [])

    def test_another_builds_cache_names_what_this_branch_changed(self):
        base = {entry["id"]: plan.fingerprint(self.root, self.manifest, entry) for entry in plan.entries(self.manifest)}
        base["Prop_Dumpster"] = "older"
        del base["Zombie_Walker"]
        chosen = plan.entries(self.manifest)
        moved = [entry["id"] for entry in plan.changed(self.root, self.manifest, chosen, base, self.models)]
        self.assertEqual(moved, ["Zombie_Walker", "Prop_Dumpster"])
        with self.assertRaises(plan.PlanError):
            plan.parse_args(["--cache", "base.json"])
        self.assertTrue(plan.parse_args(["--cache", "base.json", "--dry-run"])["cache"].endswith("base.json"))

    def test_a_corrupt_cache_rebuilds_everything(self):
        with open(os.path.join(self.root, plan.CACHE), "w", encoding="utf-8") as handle:
            handle.write("{not json")
        self.assertEqual(len(self.stale()), 113)


class SidecarTests(unittest.TestCase):
    def test_sidecar_records_what_unity_reads(self):
        manifest = plan.load_manifest(repo_root())
        entry = plan.select(manifest, only=["Vehicle_Wrecked_Sedan"])[0]
        stats = {"tris": 1500, "lodTris": [1000, 500], "width": 1.9, "depth": 4.4, "height": 1.3, "floor": 0.0, "centerX": 0.1, "centerY": -0.2, "materials": ["B", "A"]}
        record = plan.sidecar(entry, stats, "abc", "deadbeef", "4.2.11")
        self.assertEqual(record["collider"], "box")
        self.assertEqual(record["pivot"], "bottom")
        self.assertEqual(record["lods"], [1.0, 0.5])
        self.assertEqual(record["lodTris"], [1000, 500])
        self.assertEqual(record["gitSha"], "deadbeef")
        self.assertEqual(record["generatorHash"], "abc")
        self.assertEqual(record["size"], {"width": 1.9, "depth": 4.4, "height": 1.3})
        self.assertEqual(record["center"], {"x": 0.1, "y": -0.2})

    def test_sidecar_paths_sit_beside_the_fbx(self):
        entry = {"output": "Props/Prop_Dumpster.fbx"}
        self.assertEqual(plan.sidecar_path("/m", entry).replace("\\", "/"), "/m/Props/Prop_Dumpster.meta.json")

    def test_sidecar_meta_guid_is_stable_per_path(self):
        first = plan.sidecar_meta("/a/Assets/Models/Props/Prop_Dumpster.meta.json")
        second = plan.sidecar_meta("/b/Assets/Models/Props/Prop_Dumpster.meta.json")
        other = plan.sidecar_meta("/a/Assets/Models/Props/Prop_Crate_Wood.meta.json")
        self.assertEqual(first, second)
        self.assertNotEqual(first, other)
        self.assertIn("TextScriptImporter", first)

    def test_committed_sidecars_match_their_entries(self):
        root = repo_root()
        manifest = plan.load_manifest(root)
        models = os.path.join(root, plan.MODELS)
        cache = plan.load_cache(root)
        for entry in plan.entries(manifest):
            with open(plan.sidecar_path(models, entry), encoding="utf-8") as handle:
                record = json.load(handle)
            self.assertEqual(record["id"], entry["id"])
            self.assertEqual(record["generatorHash"], cache.get(entry["id"]), entry["id"])
            self.assertGreater(record["tris"], 0)
            self.assertTrue(record["materials"], entry["id"])

    def test_committed_cache_is_current(self):
        root = repo_root()
        manifest = plan.load_manifest(root)
        stale = plan.changed(root, manifest, plan.entries(manifest), plan.load_cache(root))
        self.assertEqual([entry["id"] for entry in stale], [])

    def test_summary_table_lines_up(self):
        table = plan.summary_table([
            {"id": "Prop_Dumpster", "status": "ok", "seconds": 0.2, "tris": 60},
            {"id": "Zombie_Walker", "status": "FAILED", "seconds": 1.25, "tris": ""},
        ])
        lines = table.splitlines()
        self.assertEqual(len(lines), 4)
        self.assertTrue(lines[0].startswith("asset"))
        self.assertIn("FAILED", lines[3])
        self.assertIn("1.25", lines[3])


if __name__ == "__main__":
    unittest.main()
