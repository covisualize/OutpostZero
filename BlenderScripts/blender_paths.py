"""Repo-relative paths for the Outpost Zero Blender pipeline.

This module does not import bpy, so it can be unit-tested without Blender.
"""

import os


def get_repo_root(start=None):
    """Walk upward from start (or this file) until Packages/manifest.json is found."""
    current = os.path.abspath(start or os.path.dirname(__file__))
    while True:
        manifest = os.path.join(current, "Packages", "manifest.json")
        if os.path.isfile(manifest):
            return current
        parent = os.path.dirname(current)
        if parent == current:
            raise FileNotFoundError(
                "Could not find Packages/manifest.json above {0}".format(current)
            )
        current = parent


def models_dir(category, start=None):
    """Return <repo>/Assets/Models/<category>, creating it if needed.

    OUTPOST_MODELS_DIR overrides the Assets/Models root (not the category folder).
    """
    override = os.environ.get("OUTPOST_MODELS_DIR")
    if override:
        root = os.path.abspath(override)
    else:
        root = os.path.join(get_repo_root(start), "Assets", "Models")
    path = os.path.join(root, category) if category else root
    os.makedirs(path, exist_ok=True)
    return path


def apply_cli_overrides(argv=None):
    """Parse arguments that follow Blender's ``--`` separator.

    Supports ``--models-dir <path>`` and ``--only characters,weapons``.
    Returns the list of selected categories, or None for all.
    """
    import sys

    args = list(sys.argv if argv is None else argv)
    if "--" in args:
        args = args[args.index("--") + 1 :]
    else:
        args = [arg for arg in args if arg.startswith("--")]

    only = None
    index = 0
    while index < len(args):
        token = args[index]
        if token == "--models-dir" and index + 1 < len(args):
            os.environ["OUTPOST_MODELS_DIR"] = os.path.abspath(args[index + 1])
            index += 2
            continue
        if token == "--only" and index + 1 < len(args):
            only = [part.strip() for part in args[index + 1].split(",") if part.strip()]
            index += 2
            continue
        index += 1
    return only
