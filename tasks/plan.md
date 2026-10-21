# Implementation Plan: Phase 0 Scaffold

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
