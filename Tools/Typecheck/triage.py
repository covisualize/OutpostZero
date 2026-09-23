"""Splits EditMode results into passes, native-only skips, and real failures, and writes JUnit XML."""
import collections
import os
import sys
import xml.etree.ElementTree as ET

NS = "{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}"
NATIVE = ("ECall methods must be packaged into a system module", "SecurityException")
ARTIFACT_SUITE = ".Artifacts."
ARTIFACT_BUDGET_SECONDS = 60.0


def seconds(duration):
    if not duration:
        return 0.0
    hours, minutes, rest = duration.split(":")
    return int(hours) * 3600 + int(minutes) * 60 + float(rest)


def classes(root):
    names = {}
    for test in root.iter(NS + "UnitTest"):
        method = test.find(NS + "TestMethod")
        if method is not None:
            names[test.get("id")] = method.get("className") or ""
    return names


def junit(cases):
    suites = collections.OrderedDict()
    for case in cases:
        suites.setdefault(case["class"], []).append(case)
    top = ET.Element("testsuites")
    for name, members in suites.items():
        suite = ET.SubElement(top, "testsuite", name=name, tests=str(len(members)),
                              failures=str(sum(1 for m in members if m["state"] == "failed")),
                              skipped=str(sum(1 for m in members if m["state"] == "native")),
                              time="%.3f" % sum(m["time"] for m in members))
        for member in members:
            node = ET.SubElement(suite, "testcase", classname=name, name=member["name"], time="%.3f" % member["time"])
            if member["state"] == "failed":
                ET.SubElement(node, "failure", message=member["message"][:300]).text = member["trace"]
            elif member["state"] == "native":
                ET.SubElement(node, "skipped", message="needs the Unity player: " + member["message"][:200])
    return ET.ElementTree(top)


def main(path):
    root = ET.parse(path).getroot()
    owners = classes(root)
    counts = collections.Counter()
    native = collections.Counter()
    real = []
    cases = []
    artifact_time = 0.0
    for result in root.iter(NS + "UnitTestResult"):
        outcome = result.get("outcome")
        name = result.get("testName") or ""
        owner = owners.get(result.get("testId"), "")
        took = seconds(result.get("duration"))
        if ARTIFACT_SUITE in owner + ".":
            artifact_time += took
        message = result.findtext(".//" + NS + "Message") or ""
        trace = result.findtext(".//" + NS + "StackTrace") or ""
        case = {"class": owner, "name": name.split(".")[-1], "time": took, "message": message, "trace": trace, "state": "passed"}
        cases.append(case)
        if outcome != "Failed":
            counts[outcome] += 1
            continue
        if any(marker in message for marker in NATIVE):
            frames = [line.strip() for line in trace.splitlines()
                      if ("UnityEngine." in line or "UnityEditor." in line) and "/Assets/" not in line]
            native[frames[0].split("(")[0] if frames else "engine call"] += 1
            case["state"] = "native"
            continue
        case["state"] = "failed"
        real.append((name, " ".join(message.split())[:300]))

    junit(cases).write(os.path.join(os.path.dirname(path), "editmode-junit.xml"), encoding="utf-8", xml_declaration=True)
    print("passed %d, native-only %d, failed %d" % (counts["Passed"], sum(native.values()), len(real)))
    print("artifact suite %.1fs of %.0fs" % (artifact_time, ARTIFACT_BUDGET_SECONDS))
    for frame, count in native.most_common():
        print("  native %3d  %s" % (count, frame))
    for name, message in real:
        print("FAIL %s: %s" % (name, message))
    if artifact_time > ARTIFACT_BUDGET_SECONDS:
        print("FAIL artifact suite took %.1fs" % artifact_time)
        return 1
    return 1 if real else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
