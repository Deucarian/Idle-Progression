# Deucarian Idle Progression Agent Notes

Package ID: `com.deucarian.idle-progression`
Repository: `Deucarian/Idle-Progression`

Follow the canonical Deucarian governance docs in [Package Registry](https://github.com/Deucarian/Package-Registry/blob/main/ARCHITECTURE.md), especially capability ownership and dependency rules.

## Ownership

This package owns:

- Pure C# idle/offline progression elapsed-time, capped catch-up, cycle rewards, and Progression reward-bundle calculation.

Registered capabilities:
- None.

This package must not own:

- Persistence, monetization, combat, encounters, UI, product economy tuning, save files, or template flow glue.

## Dependencies

Allowed dependency shape:

- May depend on Gameplay Foundation and Progression for deterministic IDs, progression definitions, and reward bundles.
- Runtime assembly keeps `noEngineReferences` enabled.

Required dependencies and why:

- `com.deucarian.gameplay-foundation`: shared identifiers and deterministic primitives.
- `com.deucarian.progression`: reward bundle and progression primitives used by idle reward calculation.

Optional/version-defined dependencies:

- None.

Architecture exceptions:

- None.

## Policies

- Keep idle calculations deterministic, data-driven, and save-system agnostic.
- Do not add persistence, monetization, UI, or template-specific orchestration here.
- Logging: Do not introduce direct Unity Debug calls.
- Testing: Keep offline elapsed time, caps, cycle rewards, reward bundles, and edge cases covered by EditMode tests.

## Validation

Run the shared validator before committing:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Also run existing repository tests when changing code or asmdefs. Documentation-only updates should still run `git diff --check`.

## Codex Guidance

- Inspect current files before changing anything.
- Work on `develop`; do not edit or merge `main` unless the task is promotion-only.
- Do not edit `Library/PackageCache`.
- Do not guess package versions or dependency versions.
- Do not add package dependencies casually; update asmdefs, `package.json`, `deucarian-package.json`, Package Registry, and fallback catalogs together when a dependency is truly required.
- Do not create local copies of shared helpers.
- Keep commits focused and report exactly what changed and what was validated.

