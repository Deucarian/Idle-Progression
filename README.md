# Deucarian Idle Progression

`com.deucarian.idle-progression` calculates offline elapsed time, capped catch-up rewards, production rates, and cycle rewards from supplied timestamps and definitions.

It does not store timestamps and does not trust client time. Persistence owns storage; this package only calculates from provided values.

Runtime dependencies:

- `com.deucarian.gameplay-foundation`
- `com.deucarian.progression`

The Progression dependency is used only for `RewardBundle`, `CurrencyLine`, `CurrencyId`, and `ProgressionAmount` reward concepts.

Out of scope:

- timestamp storage
- save formats
- server authority or anti-cheat
- simulating enemies, waves, towers, or projectiles while offline
- UI presentation
- automatic reward claiming

`Samples~/OfflineRewardExample` contains a tiny pure C# example that calculates a one-hour offline reward. Starter-game glue belongs in a future template package, not in this runtime package.

## Install

Stable:

```json
"com.deucarian.idle-progression": "https://github.com/Deucarian/Idle-Progression.git#main"
```

Development:

```json
"com.deucarian.idle-progression": "https://github.com/Deucarian/Idle-Progression.git#develop"
```

Use `#main` for stable package consumption and `#develop` when testing active package work.

## When To Use This

Use this package when you need Pure C# idle/offline progression elapsed-time, capped catch-up, cycle rewards, and Progression reward-bundle calculation.

Do not use this package to take ownership of capabilities outside its `AGENTS.md` boundary. Reusable behavior should stay with the package that owns that capability in the Package Registry governance docs.

## Quick Start

1. Install the package through Deucarian Package Installer or Unity Package Manager using the URL above.
2. Let Unity finish resolving packages and compiling assemblies.
3. Import the `Offline Reward Example` sample if you want a working reference scene or setup.
4. Start from the package README sections above and the public runtime/editor APIs in this repository.

## Validation

Run the shared package validator from this repository root:

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Documentation-only updates should still pass:

```powershell
git diff --check
```

## Troubleshooting

- Package does not resolve: confirm the stable or development Git URL matches the Package Registry entry and that required Deucarian dependencies are installed.
- Unity compile errors after install: let Package Manager finish resolving dependencies, then check asmdef references against `package.json` dependencies.
- Behavior appears to belong in another package: consult `AGENTS.md` and the Package Registry governance docs before moving or duplicating code.

## License

MIT. See `LICENSE.md`.
