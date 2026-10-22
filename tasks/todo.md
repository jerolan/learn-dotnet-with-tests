# Todo: Book build and alignment

> The Phase 0 tasks below are historical and complete; see `SPEC-phase1.md`.

## Active increment: Part 3 v15 SignalR

### Task 1: Add the standalone SignalR version
- [x] Copy the v14 solution into `build-an-app/v15-signalr` so it remains independent.
- [x] Broadcast queued task events through an authenticated SignalR hub.
- [x] Verify an authenticated client receives a created-task event and an anonymous client cannot negotiate the hub (34 tests pass).
- Verify: `dotnet test build-an-app/v15-signalr/TaskTracker.sln`.
- Files: `build-an-app/v15-signalr/**` (standalone snapshot plus SignalR changes).
- Dependency: none.

### Task 2: Write the chapter pair and clarify the companion appendix
- [x] Add the English and Spanish v15 chapters in template order with the LGWT WebSockets rhythm.
- [x] Distinguish the existing Part 4 SignalR sample as supplementary material.
- Verify: section-order and bilingual-link checks.
- Files: v15 chapter pair and `qa/signalr/signalr.md`, `qa/signalr/signalr.es.md`.
- Dependency: Task 1.

### Task 3: Align navigation and CI
- [x] Index v15 as 🟢 and update Part 4 coverage in both languages.
- [x] Publish the latest TaskTracker version from CI.
- Verify: README link check and all-solution `dotnet test`.
- Files: `README.md`, `qa/README.md`, `qa/README.es.md`, `.github/workflows/ci.yml`.
- Dependency: Tasks 1-2.

## Historical: Phase 0 Scaffold (complete)

## Task 1: Skeleton folders plus template
Description: Create fundamentals, testing, build-an-app, qa folders with gitkeep placeholders and add the LGWT chapter template.
Acceptance criteria:
- fundamentals/TEMPLATE.md exists with 8 LGWT sections in order
- testing, build-an-app, qa exist with .gitkeep
Verification: ls folders, head template, git status clean except new files
Dependencies: None
Files likely touched: fundamentals/TEMPLATE.md, testing/.gitkeep, build-an-app/.gitkeep, qa/.gitkeep
Estimated scope: Small 1-2 files plus dirs

## Task 2: Move chapters and remove orphan
Description: git mv the 25 README-linked files into fundamentals and git rm decimals.md.
Acceptance criteria:
- 25 files under fundamentals with same names, root has no chapter files left except README
- decimals.md absent from tree and index
Verification: ls counts, test minus f decimals.md, git status shows renames
Dependencies: Task 1
Files likely touched: 25 moves plus 1 delete
Estimated scope: Medium 3-5 effective operations, bulk move

## Task 3: README link repoint
Description: Repoint all 25 chapter links to fundamentals/name.md, keep prose and order.
Acceptance criteria:
- All 25 links resolve to existing files
- No prose changes outside link lines
Verification: link check script plus git diff README shows link lines only
Dependencies: Task 2
Files likely touched: README.md
Estimated scope: Small 1 file

## Task 4: CI workflow
Description: Add .github/workflows/ci.yml running dotnet test across discovered solutions on push and pull request.
Acceptance criteria:
- Workflow file exists with push and pull_request triggers
- Runs dotnet test over sln files
Verification: YAML parses, workflow triggers as specified, local dotnet test still passes
Dependencies: Task 2
Files likely touched: .github/workflows/ci.yml
Estimated scope: Small 1 file

## Checkpoint: After Tasks 1-4
- All tests pass with dotnet test on MyFirstDotNetApp solution
- Link check passes
- Review with human before final commit split if requested

## Task 5: Final verification
Description: Prove Phase 0 completion criteria without changing content.
Acceptance criteria:
- dotnet test passes
- 25 links resolve, orphan gone, template has 8 sections, ci.yml exists
- Diffs are renames plus README links only, no TFM changes
Verification: run dotnet test, link check, git diff stat, git status
Dependencies: Tasks 1-4
Files likely touched: none, verification only
Estimated scope: Small verification
