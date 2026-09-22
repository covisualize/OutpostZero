"""Splits EditMode results into passes, native-only skips, and real failures."""
import collections
import sys
import xml.etree.ElementTree as ET

NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
NATIVE = ("ECall methods must be packaged into a system module", "SecurityException")


def main(path):
    root = ET.parse(path).getroot()
    counts = collections.Counter()
    native = collections.Counter()
    real = []
    for result in root.iter(NS + "UnitTestResult"):
        outcome = result.get("outcome")
        if outcome != "Failed":
            counts[outcome] += 1
            continue
        message = result.findtext(".//" + NS + "Message") or ""
        trace = result.findtext(".//" + NS + "StackTrace") or ""
        if any(marker in message for marker in NATIVE):
            frames = [line.strip() for line in trace.splitlines()
                      if ("UnityEngine." in line or "UnityEditor." in line) and "/Assets/" not in line]
            native[frames[0].split("(")[0] if frames else "engine call"] += 1
            continue
        real.append((result.get("testName"), " ".join(message.split())[:300]))

    print("passed %d, native-only %d, failed %d" % (counts["Passed"], sum(native.values()), len(real)))
    for frame, count in native.most_common():
        print("  native %3d  %s" % (count, frame))
    for name, message in real:
        print("FAIL %s: %s" % (name, message))
    return 1 if real else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
