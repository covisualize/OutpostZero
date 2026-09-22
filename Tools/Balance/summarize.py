"""Summarise balance telemetry and check it against the PRO-70 targets.

    python Tools/Balance/summarize.py                     # reads the default Telemetry folder
    python Tools/Balance/summarize.py --dir path/to/Telemetry --json

The game writes expeditions.csv and days.csv to <persistentDataPath>/Telemetry (on Windows,
%USERPROFILE%\\AppData\\LocalLow\\DefaultCompany\\OutpostZero\\Telemetry). Targets on Survivor
(difficulty 2): the first death usually lands on day 4-7, and the radio tower is won around day 25-35.
Exit code 1 means a finished run landed outside a target.
"""

import argparse
import csv
import json
import os
import statistics
import sys

SURVIVOR = 2
FIRST_DEATH = (4, 7)
WIN_DAY = (25, 35)
DEATH_ENDS = {"Succession", "Wiped", "Dragged"}


def default_dir():
    home = os.path.expanduser("~")
    if sys.platform.startswith("win"):
        return os.path.join(os.environ.get("USERPROFILE", home), "AppData", "LocalLow", "DefaultCompany", "OutpostZero", "Telemetry")
    if sys.platform == "darwin":
        return os.path.join(home, "Library", "Application Support", "DefaultCompany", "OutpostZero", "Telemetry")
    return os.path.join(home, ".config", "unity3d", "DefaultCompany", "OutpostZero", "Telemetry")


def read(path):
    if not os.path.exists(path):
        return []
    with open(path, newline="", encoding="utf-8") as handle:
        return list(csv.DictReader(handle))


def num(row, key, cast=float):
    try:
        return cast(row.get(key, "") or 0)
    except ValueError:
        return cast(0)


def summarize(expeditions, days):
    runs = {}
    for row in expeditions:
        run = runs.setdefault(row["run"], {"expeditions": [], "days": []})
        run["expeditions"].append(row)
    for row in days:
        run = runs.setdefault(row["run"], {"expeditions": [], "days": []})
        run["days"].append(row)

    out = []
    for run_id in sorted(runs):
        exps = runs[run_id]["expeditions"]
        day_rows = runs[run_id]["days"]
        difficulty = num(exps[0], "difficulty", int) if exps else int(run_id.rsplit("-d", 1)[-1]) if "-d" in run_id else 0
        first_death = None
        for row in exps:
            if row["end"] in DEATH_ENDS:
                first_death = num(row, "day", int)
                break
        for row in day_rows:
            if num(row, "deaths_total", int) > 0:
                day = num(row, "day", int)
                first_death = day if first_death is None else min(first_death, day)
                break
        win = next((num(r, "day", int) for r in exps if r["end"] == "Victory"), None)
        extracted = sum(1 for r in exps if r["end"] in ("Extracted", "Victory"))
        kills = sum(num(r, "kills", int) for r in exps)
        spent = sum(max(0, num(r, "ammo_start", int) - num(r, "ammo_end", int)) for r in exps)
        quota = sum(1 for r in exps if num(r, "kills", int) >= num(r, "kill_goal", int) and num(r, "scrap", int) >= num(r, "scrap_goal", int))
        summary = {
            "run": run_id,
            "difficulty": difficulty,
            "expeditions": len(exps),
            "days_logged": len(day_rows),
            "last_day": max([num(r, "day", int) for r in exps + day_rows] or [0]),
            "first_death_day": first_death,
            "win_day": win,
            "extraction_rate": round(extracted / len(exps), 2) if exps else None,
            "quota_rate": round(quota / len(exps), 2) if exps else None,
            "kills": kills,
            "ammo_spent": spent,
            "ammo_per_kill": round(spent / kills, 2) if kills else None,
            "median_seconds": round(statistics.median([num(r, "seconds") for r in exps]), 1) if exps else None,
            "mean_hunger_end": round(statistics.mean([num(r, "hunger_end") for r in exps]), 1) if exps else None,
            "mean_thirst_end": round(statistics.mean([num(r, "thirst_end") for r in exps]), 1) if exps else None,
            "misses": [],
        }
        if difficulty == SURVIVOR:
            if first_death is not None and not FIRST_DEATH[0] <= first_death <= FIRST_DEATH[1]:
                summary["misses"].append("first death on day %d, target %d-%d" % (first_death, FIRST_DEATH[0], FIRST_DEATH[1]))
            if win is not None and not WIN_DAY[0] <= win <= WIN_DAY[1]:
                summary["misses"].append("won on day %d, target %d-%d" % (win, WIN_DAY[0], WIN_DAY[1]))
        out.append(summary)
    return out


def table(summaries):
    cols = ["run", "difficulty", "expeditions", "last_day", "first_death_day", "win_day", "extraction_rate", "quota_rate", "ammo_per_kill", "median_seconds"]
    rows = [[("" if s[c] is None else str(s[c])) for c in cols] for s in summaries]
    widths = [max(len(c), *(len(r[i]) for r in rows)) if rows else len(c) for i, c in enumerate(cols)]
    lines = ["  ".join(c.ljust(w) for c, w in zip(cols, widths))]
    lines += ["  ".join(v.ljust(w) for v, w in zip(r, widths)) for r in rows]
    for s in summaries:
        for miss in s["misses"]:
            lines.append("MISS %s: %s" % (s["run"], miss))
    return "\n".join(lines)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dir", default=default_dir())
    parser.add_argument("--json", action="store_true")
    args = parser.parse_args(argv)
    summaries = summarize(read(os.path.join(args.dir, "expeditions.csv")), read(os.path.join(args.dir, "days.csv")))
    if not summaries:
        print("no telemetry in %s" % args.dir)
        return 0
    print(json.dumps(summaries, indent=2) if args.json else table(summaries))
    return 1 if any(s["misses"] for s in summaries) else 0


if __name__ == "__main__":
    sys.exit(main())
