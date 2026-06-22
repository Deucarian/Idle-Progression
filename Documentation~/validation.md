# Validation Notes

Unity version: `6000.3.5f1`.

Validated through shared project:

`C:\Repositories\Deucarian-Validation\AllPackages-TestProject`

No per-package validation project was created for Phase 1S.

Completion gate expectations:

- package imports through the shared validation project
- Idle Progression EditMode tests pass twice through package `testables`
- Basic Auto Defense sample smoke validates sample-only offline reward glue
- runtime dependencies remain limited to Gameplay Foundation and Progression
- no Persistence, Auto Defense, Encounters, Combat, UI, or ECS dependency is introduced
- package remains a focused runtime package, not a template or suite

Phase 1S results:

- Import: `Logs\AllPackages-TestProject-phase1s-import.log`, exit code 0. Local package registered as `com.deucarian.idle-progression`.
- EditMode pass 1: 33 passed, 0 failed, 0 skipped, 0 inconclusive, duration 0.918 seconds, `callbackCompleted=True`.
- EditMode pass 2: 33 passed, 0 failed, 0 skipped, 0 inconclusive, duration 0.931 seconds, `callbackCompleted=True`.
- PlayMode pass 1: 1 passed, 0 failed, 0 skipped, 0 inconclusive, duration 2.291 seconds, `callbackCompleted=True`.
- PlayMode pass 2: 1 passed, 0 failed, 0 skipped, 0 inconclusive, duration 2.351 seconds, `callbackCompleted=True`.

The corrected durable-runner invocation omits Unity `-quit`; `Deucarian.TestAutomation.BatchTestRunner` exits after test callbacks complete. Earlier `-runTests` and `-executeMethod ... -quit` attempts imported and compiled but did not execute tests, so those attempts are not counted as validation passes.

The logs contain Unity licensing token warnings but no compiler or test failures.
