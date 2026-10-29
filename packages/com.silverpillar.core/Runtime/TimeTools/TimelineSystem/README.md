# Timeline System duration audit

Replace the four `.cs` files in your Unity project with the versions in `Fixed/` (keep their existing asset paths and `.meta` files). Do not import both original and fixed copies into the same Unity assembly. Put `Tests~/Editor/ActionTimelineDurationTests.cs` into an **Editor Test assembly** with references to your timeline runtime assembly and NUnit, then run the Edit Mode tests from Unity Test Runner.

## Root causes and fixes

1. The creator queried `result.Duration` before child `ICachedScore` values were bound and evaluated. Free timeline slots could receive zero or stale values. Slot timing now uses a deferred fraction of the parent duration, refreshed after `SetGameObject()` and every `StartAction()`.
2. `AddSequentialTimeline()` formerly snapshot the nested timeline's Duration. Its new segment instead inherits the nested timeline's actual runtime duration, including per-start recalculation.
3. Calculated timeline duration is derived from **requested** (unclamped) segment timings, ensuring old layout/clamping does not pollute later calculations. Existing inspector-driven score calculations and fallback to nested timeline durations remain intact.
4. Distributed free slot metadata is copied by `ActionData.Clone()`, so generated timing survives `ActionTimelineScriptableObject`/controller cloning.
5. The controller now initializes inline Inspector-authored timelines, removes naturally finished timelines from its active partition, avoids rerunning a canceled timeline forever during auto-start, and connects `m_OnStart` to the start event rather than end.
6. `ActionTimelineScriptableObject.Clone()` now handles a missing source timeline gracefully.

## Invariants

- `SetDurationInTimeline`: parent duration comes from its configured `m_Duration`; sequences/free items are clipped to that duration.
- `CalculateDurationFromTimelineSequenceData`: sum of **unclamped requested** sequential durations, including nested child timeline durations without a separate score calculator.
- `CalculateDurationFromFreeActionData`: greatest requested free-action `start + duration`.
- Creator: parent duration comes from its sequential segments; free segments occupy equal fractions of **that** duration. Their own timeline duration does **not** determine the slot length. A creator with no positive-length sequential content consequently has a zero-length result.
- `OnceWhenSettingTheGameObject` score: evaluated during `SetGameObject` after the score is bound; `EverytimeOnStartingTimeline`: evaluated during start preparation. For nested duration inference, start preparation is cached so it is not repeated when the nested timeline starts.

## Validation status

Static source review and code-level corrections performed. Unity, Odin, RandomController, and project-specific `ICachedScore` implementations are not installed in this sandbox; the supplied NUnit tests have **not** been executed in Unity. Test score-driven Inspector segments in your project with concrete ICachedScore implementations, including per-start scores that change each run.
