---
name: ship
description: >-
  Audit, version and build a Unity game for release. Runs the automated half of the pre-launch checklist (development flag, scenes in build, bundle ids, version and build numbers against the last shipped ones, IL2CPP and ARM64, minification and stripping, one CanvasScaler configuration, safe-area presence, icons, hardcoded figures and non-ASCII strings in player copy, platform-named literals, StopCoroutine no-ops), prints the manual device checklist filtered to the game's features, bumps version and build numbers, triggers iOS, Android, WebGL or Mac builds through the request-file helper and reads the build report, and drafts store-listing text with every claim listed for tracing. Use it whenever the user says ship, release, build, submit, TestFlight, Play Console, App Store, "bump the version", "is it ready to ship", "pre-launch check", or asks for a build of any target. Not for playtesting or generating assets.
---

# ship

The playbook's pre-launch list exists because every item on it once cost a release. This skill runs the part a machine can check, prints the part only a device can, and makes the build itself boring: one request file, one report, no BuildPipeline calls from tool threads.

## Modes

- `--check` (default when the brief is a question): audit only, read-only, writes `Builds/ship_report.md`.
- `--bump [patch|minor|major]`: bump `bundleVersion` and both build numbers. Refuses when the audit has fails unless the user says override.
- `--build <ios|android|webgl|mac> [--dev]`: request a build and wait for the report.
- `--release <target>`: check, bump, build, listing, in that order, stopping at the first fail.
- `--listing`: draft store text into `Builds/listing/`.
- `--plan`: print what each step would do and touch nothing.

## Stage 0: Preflight

1. One editor, right project (`Application.dataPath` through `Unity_RunCommand`). Read `.claude/game-project.json` for platforms, features (iap, ads, analytics sdks) and `lastShipped`; create a minimal one if missing and say so.
2. Helpers installed and matching `assets/`: `Assets/Editor/GameTools/ShipAudit.cs` and `Assets/Editor/GameTools/BuildRequestPoller.cs`. Install with `Unity_CreateScript`, refresh, read the console for compile errors.
3. A project may have its own build menu (`Unity_ManageMenuItem` List with search "Build"). When the descriptor names one under `build.menu`, use it instead of the generic poller; the report path is still `Builds/build_report.txt` unless the descriptor says otherwise.
4. Not in play mode, not compiling.

## Stage 1: Audit

1. `GameTools.ShipAudit.Run(outJsonPath, lastIosBuildNumber, lastAndroidVersionCode)` through `Unity_RunCommand`; the two numbers come from `lastShipped` in the descriptor. It never loads scenes; canvas scalers and safe areas are read from the YAML.
2. `python3 scripts/source_audit.py <project> --out Builds/source_audit.json` scans the game's C# for the silent-bug patterns in `references/prelaunch-checklist.md`.
3. `python3 scripts/ship_report.py --audit Builds/ship_audit.json --source Builds/source_audit.json --descriptor .claude/game-project.json --out Builds/ship_report.md` writes the report: automated table, source findings, then the manual device checklist filtered by the game's features.
4. Reply with the fails and warns, one line each, and the count of manual items left. A fail blocks `--bump` and `--release`.

## Stage 2: Bump

Semver on `PlayerSettings.bundleVersion`; iOS `buildNumber` and Android `bundleVersionCode` become `lastShipped + 1` or higher. One RunCommand, then re-read the values and print them. The descriptor's `lastShipped` is updated only after a build reports success, so a failed build does not consume a number.

## Stage 3: Build

1. `GameTools.BuildRequestPoller.WriteRequest(target, development)` through `Unity_RunCommand` (or the project's own menu item). The poller picks the request up on the next editor update and runs on the main loop.
2. Wait for `Builds/build_report.txt` to contain `FINISHED`. Use the Monitor tool on the file when available, otherwise poll every 30 seconds; builds take minutes. Do not narrate the wait.
3. Read `RESULT`, `OUTPUT`, error lines. On success update `lastShipped` in the descriptor. On failure print the error lines and stop; do not retry a build without a change.
4. iOS produces an Xcode project. Print the archive and upload commands from `references/prelaunch-checklist.md` but do not run them: signing and upload are the user's.
5. Android release: after the build, grep `usage.txt` and `mapping.txt` under the Gradle output for every string-referenced class the descriptor lists under `analytics.sdks` and any `AndroidJavaClass("...")` literal the source audit found; a name in `usage.txt` was deleted by the shrinker.

## Stage 4: Listing

`references/store-listing.md` has the template and the character limits. Fill it from the descriptor and the brief, list every claim that names a number or a feature under "Claims to trace" so the user can grep the constants (SOP section 3), and save to `Builds/listing/<platform>.md`. Screenshots come from the latest `Playtest/` run when one exists.

## Rules

- Never build a release with the development flag. Never bump before a clean audit. Never update `lastShipped` before a successful build.
- Only one build at a time; the poller refuses while compiling, playing or already building.
- The manual checklist is not optional and cannot be ticked by this skill. It prints, the user ticks.
- Anything the audit cannot see (receipt validation on device, ATT prompt, dashboard events) is listed as manual, never assumed.

## Reference map

- `references/prelaunch-checklist.md`: the full checklist tagged auto or manual, gated by feature, plus the iOS archive commands and the shrinker greps.
- `references/store-listing.md`: template and limits per store.
- `assets/ShipAudit.cs`, `assets/BuildRequestPoller.cs`: installed into the project. `scripts/source_audit.py`, `scripts/ship_report.py`: run with python3.
