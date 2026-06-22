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
