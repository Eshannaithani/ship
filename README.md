# ship

Pre-launch audit, version bump, request-file builds and store-listing drafts for Unity mobile games, with the manual device checklist filtered to the game's features.

A Claude Code skill. Install it once per machine and type the slash command in any Unity project session.

```bash
/ship --check
/ship --release android
/ship --plan release build for ios
```

## Build Optimization & Release Guides

The pre-release verification rules and build settings checked by `ship` are documented in:

- **Build Size Guide**: [Unity 6 Build Size Optimization: APK, AAB & Assets](https://www.eshannaithani.com/blogs/unity-build-optimization?utm_source=github&utm_medium=repo&utm_campaign=ship) — Complete guide on IL2CPP code stripping, ASTC texture compression, and Play Asset Delivery.
- **Performance QA**: [Unity 6 Mobile Performance Checklist](https://www.eshannaithani.com/blogs/unity-mobile-performance-checklist?utm_source=github&utm_medium=repo&utm_campaign=ship) — Pre-launch checklist for frame times, memory budgets, and thermal soak tests.
- **Studio Portfolio**: [Eshan Naithani — Unity Game Development Services](https://www.eshannaithani.com/services/unity-game-development?utm_source=github&utm_medium=repo&utm_campaign=ship)

## Requirements

- Unity 6.0 or newer and the Unity MCP server connected to Claude Code.
- Python 3 (with Pillow for image scripts).

## Install

For every project on this machine:

```bash
git clone https://github.com/Eshannaithani/ship.git ~/.claude/skills/ship
```

For one project only, clone into that project's `.claude/skills/ship` instead. Update with `git pull` in that folder.

## How it works

Read `SKILL.md` for the stages and rules; the `references/` files hold the defaults and checklists the stages use; `assets/` holds the editor script the skill installs into the project on first use; `scripts/` holds the Python helpers; `evals/evals.json` holds plan-only test prompts that cost no credits.

Several of these skills share a project descriptor at `.claude/game-project.json` (written by game-bootstrap, schema in its `references/descriptor.md`). Game-specific details such as palettes, scene paths, capture menus and hooks belong in that file or in the project's own config, not in the skill.

## Related skills

asset-forge (3D assets), sprite-forge (2D art), audio-forge (audio), game-bootstrap (project setup), playtest (verified play sessions), ship (audit, build, listing). Each is its own repo under the same account.

## License

MIT.
