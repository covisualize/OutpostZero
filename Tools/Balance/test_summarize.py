import csv
import io
import os
import re
import sys
import tempfile
import unittest

sys.path.insert(0, os.path.dirname(__file__))

import summarize  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def header(name):
    with open(os.path.join(ROOT, "Assets", "Scripts", "Shell", "BalanceLog.cs"), encoding="utf-8") as handle:
        return re.search(r'public const string %s = "([^"]+)"' % name, handle.read()).group(1)


def rows(head, records):
    text = io.StringIO()
    writer = csv.DictWriter(text, fieldnames=head.split(","))
    writer.writeheader()
    for record in records:
        writer.writerow({k: record.get(k, 0) for k in head.split(",")})
    text.seek(0)
    return list(csv.DictReader(text))


def exp(run, day, end, **extra):
    record = {"run": run, "day": day, "difficulty": run.rsplit("-d", 1)[1], "end": end, "kills": 10, "kill_goal": 8,
              "scrap": 30, "scrap_goal": 20, "seconds": 300, "ammo_start": 60, "ammo_end": 40, "hunger_end": 50, "thirst_end": 40}
    record.update(extra)
    return record


class SummarizeTests(unittest.TestCase):
    def test_headers_match_the_game(self):
        self.assertIn("ammo_start", header("ExpeditionHeader"))
        self.assertIn("deaths_total", header("DayHeader"))

    def test_on_target_survivor_run(self):
        e = rows(header("ExpeditionHeader"), [exp("s1-d2", 1, "Extracted"), exp("s1-d2", 5, "Succession"), exp("s1-d2", 30, "Victory", kills=0)])
        d = rows(header("DayHeader"), [{"run": "s1-d2", "day": 4, "deaths_total": 0}, {"run": "s1-d2", "day": 5, "deaths_total": 1}])
        [s] = summarize.summarize(e, d)
        self.assertEqual(5, s["first_death_day"])
        self.assertEqual(30, s["win_day"])
        self.assertEqual(0.67, s["extraction_rate"])
        self.assertEqual(3.0, s["ammo_per_kill"])
        self.assertEqual([], s["misses"])

    def test_misses_are_reported_only_on_survivor(self):
        e = rows(header("ExpeditionHeader"), [exp("s1-d2", 2, "Wiped"), exp("s2-d1", 2, "Wiped"), exp("s3-d2", 12, "Victory")])
        by_run = {s["run"]: s for s in summarize.summarize(e, [])}
        self.assertEqual(["first death on day 2, target 4-7"], by_run["s1-d2"]["misses"])
        self.assertEqual([], by_run["s2-d1"]["misses"])
        self.assertEqual(["won on day 12, target 25-35"], by_run["s3-d2"]["misses"])

    def test_cli_exit_codes(self):
        with tempfile.TemporaryDirectory() as folder:
            self.assertEqual(0, summarize.main(["--dir", folder]))
            with open(os.path.join(folder, "expeditions.csv"), "w", newline="", encoding="utf-8") as handle:
                writer = csv.DictWriter(handle, fieldnames=header("ExpeditionHeader").split(","))
                writer.writeheader()
                writer.writerow({k: exp("s9-d2", 1, "Wiped").get(k, 0) for k in header("ExpeditionHeader").split(",")})
            self.assertEqual(1, summarize.main(["--dir", folder, "--json"]))


if __name__ == "__main__":
    unittest.main()
