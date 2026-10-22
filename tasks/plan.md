# Implementation Plan: Learn .NET with Tests

> Phase 0 is complete. Its plan and verification notes remain below as history.
> The active increment aligns chapter sections with the canonical template.

## Historical: Phase 0 Scaffold

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

## Completed Increment: Part 3 v15 SignalR

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

## Completed Increment: Part 1 chapter order

### Overview
Move Dependency Injection and mocking immediately after classes in the Part 1
reading order. Keep records, `with`, inheritance and composition after them, then
continue into collections and the remaining language topics.

### Decisions
- Follow the ordering in `SPEC-phase1.md`, which derives from the user's goal.
- Update the English index and the English/Spanish next-chapter links together.
- Leave chapter content and solution code unchanged; only navigation order changes.

### Tasks
- Update the Part 1 index ordering and renumber entries.
- Repoint both language versions' next-chapter links around the reordered topics.
- Check local links and verify the tree remains clean after one atomic commit.

### Checkpoint
- Dependency Injection and mocking are directly after classes in the index and
  navigation flow.
- All Part 1 links resolve in both languages.

## Completed Increment: Phase 2 specification

### Overview
Record the agreed Part 2 scope, chapter responsibilities and completion
evidence in `SPEC-phase2.md`, matching the specifications already kept for the
other phases.

### Checkpoint
- All four Part 2 chapters, standalone solutions and test counts are documented.

## Completed Increment: Part 4 delivery references

### Overview
Make the Docker and CI/CD appendices build and publish the latest TaskTracker
version, v15, so their commands match the Part 3 index and the CI workflow.

### Decisions
- Keep the tutorial focused on the same TaskTracker app and latest version.
- Update the Dockerfile and both language versions of the appendix together.
- Verify the published API path; build the container if Docker is available.

### Tasks
- Replace the v14 project paths in the container and CI/CD examples with v15.
- Record the roadmap source split for integration-testing topics in both Part 4 indexes.
- Check the documentation, Dockerfile and CI workflow use the same project path.
- Run the documented publish or container build where the local tools allow it.

### Checkpoint
- No stale v14 publish path remains in active delivery examples.
- The new paths resolve, both language versions agree, and verification evidence
  is recorded.
- The Part 4 coverage map distinguishes text-map topics from topics added by the
  roadmap image and points to the source.

## Completed Increment: Chapter section order

### Overview
Align code chapters with the section sequence in `fundamentals/TEMPLATE.md`.
Preserve each chapter's examples, red/green evidence and runnable solution while
moving its explanation into the approved teaching flow.

### Decisions
- Include every required section from `fundamentals/TEMPLATE.md` in order in each English/Spanish pair; the source-control note remains optional.
- Preserve existing examples and red/green evidence while moving or adding explanatory sections.
- Apply the TDD template to Parts 1-3 and runnable appendices where the sample supports that flow.
- Keep Phase 4's prose Q&A and explicit deferral structure as its documented exception.
- Work in coherent slices by part and verify each slice before committing it.

### Tasks
- Align Part 1 chapter pairs with the canonical section order.
- Align Part 2 and Part 3 chapter pairs with the same order.
- Align runnable Part 4 appendix pairs where the sample supports the TDD flow.
- Check bilingual heading order, local links and affected solutions after each slice.

### Checkpoint
- Every applicable chapter pair contains the required template sections in the prescribed order, with matching English/Spanish structure.
- Part 4 exceptions remain explicit and consistent with `SPEC-phase4.md`.
- The order audit passed for 44 Part 1-3 solution pairs and 11 runnable Part 4 appendix pairs.
- The repository-wide local Markdown link audit checked 460 links and found none broken.
- All Part 4 appendix solutions passed; the E2E gate was enabled for Playwright, and NBomber retains its explicit CI load-gating deferral.
