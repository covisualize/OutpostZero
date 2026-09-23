"""Changelog and version bumps for Outpost Zero.

    python Tools/Release/release.py changelog [--output CHANGELOG.md] [--check]
    python Tools/Release/release.py bump [--major|--minor|--patch] [--pre rc] [--dry-run]
    python Tools/Release/release.py notes v0.6.0

Commits follow Conventional Commits (``feat(camp): ...``, ``fix!: ...``). Older subjects that start
with an issue id (``PRO-63: ...``) or plain text are kept under "Other changes".

``bump`` reads the commits since the last ``v*`` tag, picks major/minor/patch from them (or the flag),
writes the new version into Assets/Resources/version.json, ProjectSettings bundleVersion and
SceneRoute.Version, regenerates CHANGELOG.md, and prints the tag command. It never tags or pushes.
"""

import argparse
import datetime
import json
import os
import re
import subprocess
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
VERSION_JSON = os.path.join("Assets", "Resources", "version.json")
PROJECT_SETTINGS = os.path.join("ProjectSettings", "ProjectSettings.asset")
SCENE_ROUTE = os.path.join("Assets", "Scripts", "Shell", "SceneRoute.cs")
CHANGELOG = "CHANGELOG.md"

CONVENTIONAL = re.compile(r"^(?P<type>[a-z]+)(?:\((?P<scope>[^)]+)\))?(?P<bang>!)?: (?P<text>.+)$")
ISSUE = re.compile(r"^(?P<scope>[A-Z]+-\d+): (?P<text>.+)$")
SEMVER = re.compile(r"^v?(\d+)\.(\d+)\.(\d+)(?:-([0-9A-Za-z.-]+))?(?:\+[0-9A-Za-z.-]+)?$")

GROUPS = [
    ("feat", "Features"),
    ("fix", "Fixes"),
    ("perf", "Performance"),
    ("refactor", "Refactoring"),
    ("docs", "Documentation"),
    ("test", "Tests"),
    ("build", "Build and CI"),
    ("ci", "Build and CI"),
    ("other", "Other changes"),
]
HIDDEN = {"chore", "style"}


def parse_commit(subject, body=""):
    """Returns a dict with type, scope, breaking and text for one commit subject."""
    subject = (subject or "").strip()
    breaking = "BREAKING CHANGE" in (body or "") or "BREAKING-CHANGE" in (body or "")
    match = CONVENTIONAL.match(subject)
    if match:
        return {
            "type": match.group("type"),
            "scope": match.group("scope") or "",
            "breaking": breaking or bool(match.group("bang")),
            "text": match.group("text").strip(),
        }
    match = ISSUE.match(subject)
    if match:
        return {"type": "other", "scope": match.group("scope"), "breaking": breaking, "text": match.group("text").strip()}
    return {"type": "other", "scope": "", "breaking": breaking, "text": subject}


def is_noise(subject):
    return subject.startswith("Merge ") or subject.startswith("chore(release)")


def bump_kind(commits):
    kind = "patch"
    for commit in commits:
        if commit["breaking"]:
            return "major"
        if commit["type"] == "feat":
            kind = "minor"
    return kind


def parse_version(text):
    match = SEMVER.match((text or "").strip())
    if not match:
        raise ValueError("not a semantic version: %r" % text)
    return int(match.group(1)), int(match.group(2)), int(match.group(3)), match.group(4) or ""


def format_version(major, minor, patch, pre=""):
    text = "%d.%d.%d" % (major, minor, patch)
    return text + ("-" + pre if pre else "")


def next_version(current, kind, pre=None):
    """Next version after ``current``. Before 1.0 a breaking change bumps minor, as SemVer allows.

    With ``pre`` ("rc"), the result is a prerelease; bumping an rc again only advances its number.
    """
    major, minor, patch, current_pre = parse_version(current)
    if pre and current_pre.startswith(pre + "."):
        number = current_pre.split(".", 1)[1]
        if number.isdigit():
            return format_version(major, minor, patch, "%s.%d" % (pre, int(number) + 1))
    if current_pre and not pre:
        return format_version(major, minor, patch)
    if kind == "major" and major == 0:
        kind = "minor"
    if kind == "major":
        major, minor, patch = major + 1, 0, 0
    elif kind == "minor":
        minor, patch = minor + 1, 0
    else:
        patch += 1
    return format_version(major, minor, patch, pre + ".1" if pre else "")


def render_section(title, date, commits):
    lines = ["## %s" % title + (" - %s" % date if date else ""), ""]
    by_group = {}
    breaking = []
    for commit in commits:
        if commit["type"] in HIDDEN:
            continue
        label = dict(GROUPS).get(commit["type"], "Other changes")
        scope = "**%s:** " % commit["scope"] if commit["scope"] else ""
        by_group.setdefault(label, []).append("- %s%s" % (scope, commit["text"]))
        if commit["breaking"]:
            breaking.append("- %s%s" % (scope, commit["text"]))
    if breaking:
        lines += ["### Breaking changes", ""] + breaking + [""]
    seen = set()
    for _, label in GROUPS:
        if label in seen or label not in by_group:
            continue
        seen.add(label)
        lines += ["### %s" % label, ""] + by_group[label] + [""]
    if len(lines) == 2:
        lines += ["No user-facing changes.", ""]
    return "\n".join(lines)


def render(releases):
    """``releases`` is newest first: (title, date, commits)."""
    head = [
        "# Changelog",
        "",
        "All notable changes to Outpost Zero. Generated by `python Tools/Release/release.py changelog` from",
        "Conventional Commits; do not edit by hand. Versions follow [Semantic Versioning](https://semver.org).",
        "",
    ]
    return "\n".join(head) + "\n" + "\n".join(render_section(*release) for release in releases).rstrip() + "\n"


def git(*args):
    return subprocess.run(["git"] + list(args), cwd=ROOT, capture_output=True, text=True, check=True).stdout


def tags():
    out = git("tag", "--list", "v*", "--sort=-v:refname")
    return [t for t in out.split() if SEMVER.match(t)]


def commits_between(start, end):
    span = "%s..%s" % (start, end) if start else end
    out = git("log", "--no-merges", "--pretty=format:%s%x1f%b%x1e", span)
    commits = []
    for record in out.split("\x1e"):
        record = record.strip("\n")
        if not record:
            continue
        subject, _, body = record.partition("\x1f")
        if is_noise(subject):
            continue
        commits.append(parse_commit(subject, body))
    return commits


def tag_date(tag):
    return git("log", "-1", "--format=%cs", tag).strip()


def collect(unreleased_title="Unreleased", unreleased_date=""):
    found = tags()
    releases = []
    pending = commits_between(found[0] if found else None, "HEAD")
    if pending or not found:
        releases.append((unreleased_title, unreleased_date, pending))
    for i, tag in enumerate(found):
        older = found[i + 1] if i + 1 < len(found) else None
        releases.append((tag.lstrip("v"), tag_date(tag), commits_between(older, tag)))
    return releases


def read_version():
    with open(os.path.join(ROOT, VERSION_JSON), encoding="utf-8") as handle:
        return json.load(handle)["version"]


def write_version(version):
    path = os.path.join(ROOT, VERSION_JSON)
    with open(path, encoding="utf-8") as handle:
        data = json.load(handle)
    data["version"] = version
    with open(path, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(json.dumps(data, indent=2) + "\n")
    replace_in(PROJECT_SETTINGS, r"(\n  bundleVersion: ).*", r"\g<1>" + version.split("-")[0])
    replace_in(SCENE_ROUTE, r'(public const string Version = ")[^"]*(")', r"\g<1>" + version + r"\g<2>")


def replace_in(relative, pattern, replacement):
    path = os.path.join(ROOT, relative)
    with open(path, encoding="utf-8", newline="") as handle:
        text = handle.read()
    updated, count = re.subn(pattern, replacement, text, count=1)
    if count != 1:
        raise RuntimeError("could not find the version in " + relative)
    with open(path, "w", encoding="utf-8", newline="") as handle:
        handle.write(updated)


def write_changelog(text, output):
    with open(os.path.join(ROOT, output), "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)


def notes(version):
    """Release notes for one version: its CHANGELOG section without the heading."""
    with open(os.path.join(ROOT, CHANGELOG), encoding="utf-8") as handle:
        text = handle.read()
    wanted = version.lstrip("v")
    match = re.search(r"^## %s(?: - [^\n]*)?\n(.*?)(?=^## |\Z)" % re.escape(wanted), text, re.S | re.M)
    return match.group(1).strip() + "\n" if match else ""


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    log = sub.add_parser("changelog")
    log.add_argument("--output", default=CHANGELOG)
    log.add_argument("--check", action="store_true", help="fail if the file is not current")
    bump = sub.add_parser("bump")
    kind = bump.add_mutually_exclusive_group()
    kind.add_argument("--major", action="store_const", const="major", dest="kind")
    kind.add_argument("--minor", action="store_const", const="minor", dest="kind")
    kind.add_argument("--patch", action="store_const", const="patch", dest="kind")
    bump.add_argument("--pre", help="prerelease label, e.g. rc")
    bump.add_argument("--dry-run", action="store_true")
    note = sub.add_parser("notes")
    note.add_argument("version")
    args = parser.parse_args(argv)

    if args.command == "changelog":
        text = render(collect())
        if args.check:
            path = os.path.join(ROOT, args.output)
            current = open(path, encoding="utf-8").read() if os.path.exists(path) else ""
            if current != text:
                print("%s is stale; run: python Tools/Release/release.py changelog" % args.output)
                return 1
            print("%s is current" % args.output)
            return 0
        write_changelog(text, args.output)
        print("wrote %s" % args.output)
        return 0

    if args.command == "notes":
        text = notes(args.version)
        if not text:
            print("no CHANGELOG section for %s" % args.version, file=sys.stderr)
            return 1
        sys.stdout.write(text)
        return 0

    current = read_version()
    found = tags()
    pending = commits_between(found[0] if found else None, "HEAD")
    chosen = args.kind or bump_kind(pending)
    version = next_version(current, chosen, args.pre)
    print("%s -> %s (%s, %d commits since %s)" % (current, version, chosen, len(pending), found[0] if found else "the start"))
    if args.dry_run:
        return 0
    write_version(version)
    today = datetime.date.today().isoformat()
    write_changelog(render(collect(version, today)), CHANGELOG)
    print("Updated %s, %s, %s and %s." % (VERSION_JSON, PROJECT_SETTINGS, SCENE_ROUTE, CHANGELOG))
    print('Next: git commit -am "chore(release): v%s" && git tag v%s && git push origin v%s' % (version, version, version))
    return 0


if __name__ == "__main__":
    sys.exit(main())
