# Implementation Plan: Phase 0 Scaffold

> Phase 0 is complete. Its plan and verification notes remain below as history.
> The active increment is Part 3 v15 SignalR.

## Overview
Turn the flat root into the book skeleton with moves only. Foundations first, then moves, then README, then CI, then verification. Each task leaves the tree working. No chapter prose changes and no per-chapter solutions in this phase.

## Architecture Decisions
- Flat fundamentals layout preserves filenames to keep git mv history clean and keep Phase 1 diffs simple.
- Template at fundamentals/TEMPLATE.md as single canonical copy from goal.
- Placeholders testing, build-an-app, qa with .gitkeep so future phases have stable targets without claiming coverage.
- CI discovers sln files so Phase 1 solutions are picked up with no workflow edits.
- README keeps current sections in Phase 0, only link targets change; full 4-part TOC rebuild is deferred to later phases unless human asks now.

## Task List

### Phase A: Foundation
- Task 1: skeleton folders plus template
- Task 2: move chapters and remove orphan

### Checkpoint: Foundation
- All 25 chapter files exist under fundamentals, decimals.md gone, git status shows renames only

### Phase B: Integration
- Task 3: README link repoint
- Task 4: CI workflow

### Checkpoint: Integration
- Every README link resolves, workflow YAML valid

### Phase C: Verification
- Task 5: final verification

### Checkpoint: Complete
- All Phase 0 success criteria met, ready for human review before Phase 1

## Risks and Mitigations
- Risk: dotnet 10 SDK building net6.0 target may need reference packs. Impact: Medium. Mitigation: verify dotnet test early in Task 5, pin CI SDK to 6.0.x if restore fails.
- Risk: README link mass-edit breaks anchors. Impact: Low. Mitigation: scripted link check in Task 5, keep diff to link lines only.
- Risk: git mv history lost if files edited during move. Impact: Low. Mitigation: forbid body edits in this phase, check git diff similarity.

## Open Questions
- Same six questions as SPEC-phase0.md; human approval on flat layout, template path, CI SDK, README scope, commit split, placeholders.
- Tasks tracked here in tasks/todo.md (default target, no external tracker designated).

## Current Increment: Part 3 v15 SignalR

### Overview
Extend the TaskTracker from v14 with SignalR delivery for the task events
already processed by its in-process queue. Keep the version independently
runnable and preserve the existing MSTest, Moq and net6.0 stack.

### Architecture Decisions
- Copy v14 into `build-an-app/v15-signalr`; a project reference to another
  chapter would violate the standalone-solution contract.
- Broadcast from `TaskEventProcessor` through `IHubContext<TaskUpdatesHub>` so
  the existing queue remains the single source of task events.
- Require authentication on `/hubs/tasks`, matching the v14 task API.
- Use the .NET SignalR client with TestServer long polling for an end-to-end
  test of the event contract.
- Keep the Part 4 SignalR sample as optional companion material on connection
  lifecycle and delivery limits; v15 is the primary SignalR chapter.

### Task List
- Task 1: Create the standalone v15 solution and add authenticated event
  broadcasting with owner-boundary tests.
- Task 2: Write the bilingual v15 chapter and clarify the companion appendix.
- Task 3: Add v15 to both indexes, point Part 4 coverage to v15, and publish
  v15 from CI.

### Checkpoint: Complete
- The v15 focused tests pass.
- Every solution passes `dotnet test`; chapter links and bilingual section
  order resolve; CI uses v15 as its published API.

### Risk
The standalone snapshot necessarily adds the prior solution's source files.
Keep the copy self-contained and limit behavioral changes to SignalR.
