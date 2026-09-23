"""Build every model. Kept so older commands keep working; pipeline.py is the runner.

    blender -b -P BlenderScripts/build_all_assets.py -- --only characters,weapons --models-dir /tmp/m

``--only`` here takes the old phase names (characters, weapons, architecture, props, kit, base)
and forwards them to ``pipeline.py --category``. Use pipeline.py directly for single assets.
"""

import os
import sys

script_dir = os.path.dirname(os.path.abspath(__file__))
if script_dir not in sys.path:
    sys.path.insert(0, script_dir)

import pipeline


def translate(args):
    forwarded = []
    index = 0
    while index < len(args):
        token = args[index]
        if token == "--only" and index + 1 < len(args):
            forwarded.extend(["--category", args[index + 1]])
            index += 2
            continue
        forwarded.append(token)
        index += 1
    return forwarded


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    sys.exit(pipeline.main(translate(argv)))
