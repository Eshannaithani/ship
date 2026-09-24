#!/usr/bin/env python3
"""Scan a Unity project's runtime C# for the playbook's silent-bug patterns.

Usage: source_audit.py <project> [--out Builds/source_audit.json] [--ascii-only true]

Checks (SOP sections 2, 2b, 7, 16):
  non-ascii     a non-ASCII character inside a string literal (font atlas has no glyph for it)
  figure        a currency or percentage figure inside a string literal (should come from the constants class)
  platform      a store, platform or SDK name as a string literal with no #if UNITY_ platform define nearby
  stopcoroutine StopCoroutine(Method()) builds a new enumerator and stops nothing
Editor folders, Packages, Library and the skills' own installed files are skipped. Heuristics: confirm each hit.
"""
import argparse, json, os, re

STRING_RE = re.compile(r'"((?:[^"\\]|\\.)*)"')
FIGURE_RE = re.compile(r"(\$\s?\d|\d+\s?%|\+\s?\d+|x\d+\b)")
PLATFORM_RE = re.compile(r'^(GooglePlay|Google Play|AppStore|App Store|Apple|iOS|Android|AdMob|Unity Ads|IronSource|LevelPlay|AppLovin)$')
STOP_RE = re.compile(r"StopCoroutine\(\s*[A-Za-z_][A-Za-z0-9_]*\s*\(")
SKIP_DIRS = {"Editor", "Packages", "Library", "Temp", "Logs", "obj", "_Playtest", "AssetGen", "GameTools", "Playtest"}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("project")
    ap.add_argument("--out", default="")
    ap.add_argument("--ascii-only", default="true")
    a = ap.parse_args()
    ascii_only = a.ascii_only.lower() == "true"
    findings = []
    scanned = 0
    for root, dirs, files in os.walk(os.path.join(a.project, "Assets")):
        dirs[:] = [d for d in dirs if d not in SKIP_DIRS and not d.startswith(".")]
        for f in files:
            if not f.endswith(".cs"):
                continue
            path = os.path.join(root, f)
            rel = os.path.relpath(path, a.project)
            try:
                lines = open(path, encoding="utf-8", errors="replace").read().split("\n")
            except Exception:
                continue
            scanned += 1
            for i, line in enumerate(lines):
                stripped = line.strip()
                if stripped.startswith("//"):
                    continue
                if STOP_RE.search(line):
                    findings.append({"kind": "stopcoroutine", "file": rel, "line": i + 1, "text": stripped[:160]})
                for m in STRING_RE.finditer(line):
                    s = re.sub(r"<[^>]*>", "", m.group(1))   # drop rich-text tags such as <size=55%> before scanning
                    if not s:
                        continue
                    if ascii_only and any(ord(ch) > 127 for ch in s):
                        findings.append({"kind": "non-ascii", "file": rel, "line": i + 1, "text": s[:120]})
                    if FIGURE_RE.search(s) and not re.search(r"\{\d|\{0|\+ *[A-Za-z]", line):
                        findings.append({"kind": "figure", "file": rel, "line": i + 1, "text": s[:120]})
                    if PLATFORM_RE.match(s.strip()):
                        window = "\n".join(lines[max(0, i - 8):i])
                        if "#if UNITY_" not in window and "#elif UNITY_" not in window:
                            findings.append({"kind": "platform", "file": rel, "line": i + 1, "text": s})
    result = {"scannedFiles": scanned, "findings": findings, "counts": {}}
    for fnd in findings:
        result["counts"][fnd["kind"]] = result["counts"].get(fnd["kind"], 0) + 1
    if a.out:
        os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
        with open(a.out, "w") as fh:
            json.dump(result, fh, indent=2)
    print(json.dumps({"scannedFiles": scanned, "counts": result["counts"]}))
    for fnd in findings[:40]:
        print(f"{fnd['kind']:13} {fnd['file']}:{fnd['line']}  {fnd['text']}")
    if len(findings) > 40:
        print(f"... {len(findings) - 40} more in {a.out or 'the JSON'}")


if __name__ == "__main__":
    main()
