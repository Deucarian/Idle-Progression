# API Notes

Idle Progression is an analytical calculator.

- `IdleProgressionDefinition` holds max offline duration, continuous production rates, and cycle rewards.
- `IdleProductionRate` expresses deterministic continuous currency production per elapsed second.
- `IdleCycleReward` grants a Progression amount for each completed elapsed cycle.
- `IdleProgressionCalculator.Calculate` returns raw elapsed time, effective capped elapsed time, result code, and a Progression `RewardBundle`.
- Negative elapsed time returns `ClockRollback` and zero rewards.
- Huge elapsed time is capped by definition.
- Reward arithmetic floors fractional continuous amounts and saturates extreme arithmetic at `long.MaxValue`.
- The package does not simulate individual enemies, agents, projectiles, towers, or encounters.
- Persistence and application layers own last-seen timestamps and reward claiming operation IDs.
