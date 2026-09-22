import os
import sys
import unittest

sys.path.insert(0, os.path.dirname(__file__))

import release  # noqa: E402


class ParseCommitTests(unittest.TestCase):
    def test_conventional_subjects(self):
        c = release.parse_commit("feat(camp): add a well")
        self.assertEqual(("feat", "camp", False, "add a well"), (c["type"], c["scope"], c["breaking"], c["text"]))
        self.assertTrue(release.parse_commit("fix!: drop save v1")["breaking"])
        self.assertTrue(release.parse_commit("refactor: x", "BREAKING CHANGE: saves reset")["breaking"])

    def test_issue_and_plain_subjects_fall_into_other(self):
        c = release.parse_commit("PRO-63: ScreenStack for menus")
        self.assertEqual(("other", "PRO-63", "ScreenStack for menus"), (c["type"], c["scope"], c["text"]))
        c = release.parse_commit("Make Blender FBX exports byte-reproducible")
        self.assertEqual(("other", ""), (c["type"], c["scope"]))

    def test_merges_and_release_commits_are_noise(self):
        self.assertTrue(release.is_noise("Merge origin/main into x"))
        self.assertTrue(release.is_noise("chore(release): v0.6.0"))
        self.assertFalse(release.is_noise("feat: merge sort"))


class VersionTests(unittest.TestCase):
    def test_bump_kind(self):
        p = release.parse_commit
        self.assertEqual("patch", release.bump_kind([p("fix: a"), p("PRO-1: b")]))
        self.assertEqual("minor", release.bump_kind([p("fix: a"), p("feat: b")]))
        self.assertEqual("major", release.bump_kind([p("feat!: a")]))
        self.assertEqual("patch", release.bump_kind([]))

    def test_next_version(self):
        n = release.next_version
        self.assertEqual("0.5.1", n("0.5.0", "patch"))
        self.assertEqual("0.6.0", n("0.5.3", "minor"))
        self.assertEqual("0.6.0", n("0.5.3", "major"))
        self.assertEqual("2.0.0", n("1.4.2", "major"))
        self.assertEqual("0.6.0-rc.1", n("0.5.0", "minor", "rc"))
        self.assertEqual("0.6.0-rc.2", n("0.6.0-rc.1", "minor", "rc"))
        self.assertEqual("0.6.0", n("0.6.0-rc.2", "patch"))
        with self.assertRaises(ValueError):
            n("1.0", "patch")

    def test_parse_accepts_tags(self):
        self.assertEqual((1, 0, 0, "rc.1"), release.parse_version("v1.0.0-rc.1"))
        self.assertEqual((0, 5, 0, ""), release.parse_version("0.5.0+abc"))


class RenderTests(unittest.TestCase):
    def test_groups_breaking_and_hidden(self):
        p = release.parse_commit
        text = release.render_section("0.6.0", "2026-09-22", [
            p("feat(camp): wells"), p("fix: bite gap"), p("PRO-63: stack"), p("chore: tidy"), p("feat!: new saves"), p("ci: cache"),
        ])
        self.assertTrue(text.startswith("## 0.6.0 - 2026-09-22\n"))
        self.assertIn("### Breaking changes\n\n- new saves", text)
        self.assertIn("### Features\n\n- **camp:** wells\n- new saves", text)
        self.assertIn("### Fixes\n\n- bite gap", text)
        self.assertIn("### Other changes\n\n- **PRO-63:** stack", text)
        self.assertIn("### Build and CI\n\n- cache", text)
        self.assertNotIn("tidy", text)
        self.assertLess(text.index("Features"), text.index("Fixes"))

    def test_empty_section_says_so(self):
        self.assertIn("No user-facing changes.", release.render_section("Unreleased", "", [release.parse_commit("chore: x")]))

    def test_render_has_header_and_newest_first(self):
        text = release.render([("Unreleased", "", []), ("0.5.0", "2026-09-01", [release.parse_commit("feat: a")])])
        self.assertTrue(text.startswith("# Changelog\n"))
        self.assertLess(text.index("## Unreleased"), text.index("## 0.5.0 - 2026-09-01"))
        self.assertTrue(text.endswith("\n") and not text.endswith("\n\n"))


class RepoTests(unittest.TestCase):
    def test_version_sources_agree(self):
        version = release.read_version()
        with open(os.path.join(release.ROOT, release.SCENE_ROUTE), encoding="utf-8") as handle:
            self.assertIn('public const string Version = "%s"' % version, handle.read())

    def test_changelog_renders_from_history(self):
        text = release.render(release.collect())
        self.assertIn("## Unreleased", text)
        self.assertIn("**PRO-63:**", text)
        self.assertNotIn("Merge origin/main", text)

    def test_notes_reads_a_section(self):
        self.assertEqual("", release.notes("9.9.9"))
        self.assertTrue(release.notes("Unreleased").startswith("### "))


if __name__ == "__main__":
    unittest.main()
