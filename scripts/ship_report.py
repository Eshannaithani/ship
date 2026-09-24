#!/usr/bin/env python3
"""Build Builds/ship_report.md from the audit JSON, the source audit and the descriptor.

Usage: ship_report.py --audit Builds/ship_audit.json --source Builds/source_audit.json --descriptor .claude/game-project.json --out Builds/ship_report.md [--build Builds/build_report.txt]
"""
import argparse, json, os, re

HERE = os.path.dirname(os.path.abspath(__file__))
CHECKLIST = os.path.join(os.path.dirname(HERE), "references", "prelaunch-checklist.md")


def load(path, default):
    try:
        return json.load(open(path))
    except Exception:
        return default


def manual_items(features):
    items, section, gate = [], "", "always"
    for line in open(CHECKLIST).read().split("\n"):
        h = re.match(r"^## (.+?)(?: \((.+)\))?$", line)
        if h:
            section, gate = h.group(1), (h.group(2) or "always")
            continue
        m = re.match(r"^- manual: (.+)$", line)
        if m:
            gates = [g.strip() for g in gate.replace(" or ", ",").split(",")]
            if "always" in gates or any(features.get(g) for g in gates):
                items.append((section, m.group(1)))
    return items


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--audit", required=True)
    ap.add_argument("--source", default="")
    ap.add_argument("--descriptor", default="")
    ap.add_argument("--out", required=True)
    ap.add_argument("--build", default="")
    a = ap.parse_args()

    audit = load(a.audit, {})
    source = load(a.source, {"findings": [], "counts": {}}) if a.source else {"findings": [], "counts": {}}
    d = load(a.descriptor, {}) if a.descriptor else {}
    features = {"iap": bool(d.get("iap")), "ads": d.get("ads", "none") not in ("none", "", None, False), "analytics": bool((d.get("analytics") or {}).get("sdks")), "saves": True}

    lines = [f"# Ship report: {audit.get('product', d.get('name', 'game'))} {audit.get('version', '')}", ""]
    lines.append(f"**Automated:** {audit.get('fails', 0)} fail, {audit.get('warns', 0)} warn  ·  **Source findings:** {len(source.get('findings', []))}  ·  **Features:** " + ", ".join(k for k, v in features.items() if v))
    lines += ["", "## Automated checks", "", "| Status | Check | Detail |", "|---|---|---|"]
    for i in audit.get("items", []):
        lines.append(f"| {i.get('status', '').upper()} | {i.get('check', '')} | {i.get('detail', '')} |")
    lines += ["", "## Source findings (heuristic, confirm each)", ""]
    if source.get("findings"):
        for f in source["findings"][:60]:
            lines.append(f"- `{f['kind']}` {f['file']}:{f['line']}: {f['text']}")
        if len(source["findings"]) > 60:
            lines.append(f"- ... {len(source['findings']) - 60} more in the JSON")
    else:
        lines.append("- none")
    if a.build and os.path.exists(a.build):
        lines += ["", "## Last build report", "", "```", open(a.build).read().strip()[:3000], "```"]
    lines += ["", "## Manual checklist (device build)", ""]
    current = ""
    for section, item in manual_items(features):
        if section != current:
            lines.append(f"\n**{section}**")
            current = section
        lines.append(f"- [ ] {item}")
    os.makedirs(os.path.dirname(os.path.abspath(a.out)), exist_ok=True)
    with open(a.out, "w") as fh:
        fh.write("\n".join(lines) + "\n")
    print(a.out, f"{audit.get('fails', 0)} fails, {audit.get('warns', 0)} warns, {len(source.get('findings', []))} source findings, {len(manual_items(features))} manual items")


if __name__ == "__main__":
    main()
