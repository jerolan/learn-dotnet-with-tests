# Todo: Book build and alignment

> The Phase 0 tasks below are historical and complete; see `SPEC-phase1.md`.

## Active increment: Chapter section order

### Task 1: Align chapter pairs with the canonical template
- [x] Align Part 1 English/Spanish chapters with the section order in `fundamentals/TEMPLATE.md`.
- [ ] Align Part 2 and Part 3 pairs with the same order.
- [ ] Align runnable Part 4 appendices where their sample supports the TDD flow; preserve the Phase 4 Q&A and explicit deferral exceptions.
- [ ] Verify each slice has every required section in matching bilingual order, resolving links and passing solution tests.
- Verify: required-section audit, full local-link audit and `dotnet test` for affected solutions.
- Files: `fundamentals/**`, `testing/**`, `build-an-app/**`, runnable `qa/**`, phase specs and task artifacts.
- Dependency: the chapter pairs and runnable solutions already exist.

### Verified slices
- Part 1 `methods`: English and Spanish pairs now contain the required sections
  in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/methods/Methods.sln -m:1` passed 3 tests.
- Part 1 `classes`: English and Spanish pairs now contain the required sections
  in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/classes/Classes.sln -m:1` passed 5 tests.
- Part 1 `declarations`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/declarations/Declarations.sln -m:1` passed 2 tests.
- Part 1 `types`: English and Spanish pairs now contain the required sections
  in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/types/Types.sln -m:1` passed 6 tests.
- Part 1 `records`: English and Spanish pairs now contain the required sections
  in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/records/Records.sln -m:1` passed 4 tests.
- Part 1 `exceptions`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/exceptions/Exceptions.sln -m:1` passed 4 tests.
- Part 1 `inheritance`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/inheritance/Inheritance.sln -m:1` passed 4 tests.
- Part 1 `composition`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/composition/Composition.sln -m:1` passed 4 tests.
- Part 1 `collections`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/collections/Collections.sln -m:1` passed 5 tests.
- Part 1 `enumerable`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/enumerable/Enumerable.sln -m:1` passed 3 tests.
- Part 1 `generics`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/generics/Generics.sln -m:1` passed 4 tests.
- Part 1 `async`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/async/Async.sln -m:1` passed 3 tests.
- Part 1 `control-flow`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/control-flow/ControlFlow.sln -m:1` passed 8 tests.
- Part 1 `decimal`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/decimal/Decimal.sln -m:1` passed 5 tests.
- Part 1 `dependency-injection`: English and Spanish pairs now contain the
  required sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/dependency-injection/DependencyInjection.sln -m:1` passed 2 tests.
- Part 1 `hello-world`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/hello-world/HelloWorld.sln -m:1` passed 2 tests.
- Part 1 `install`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/install/InstallCheck.sln -m:1` passed 1 test.
- Part 1 `io`: English and Spanish pairs now contain the required sections
  in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/io/Io.sln -m:1` passed 4 tests.
- Part 1 `linq`: English and Spanish pairs now contain the required sections
  in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/linq/Linq.sln -m:1` passed 3 tests.
- Part 1 `mock`: English and Spanish pairs now contain the required sections
  in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/mock/Mock.sln -m:1` passed 2 tests.
- Part 1 `queriable`: English and Spanish pairs now contain the required
  sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/queriable/Queriable.sln -m:1` passed 3 tests.
- Part 1 `ref`: English and Spanish pairs now contain the required sections
  in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/ref/Ref.sln -m:1` passed 5 tests.
- Part 1 `tasks`: English and Spanish pairs now contain the required sections
  in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/tasks/Tasks.sln -m:1` passed 4 tests.
- Part 1 `using`: English and Spanish pairs now contain the required sections
  in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/using/Using.sln -m:1` passed 4 tests.
- Part 1 `with`: English and Spanish pairs now contain the required sections
  in order. `mise exec dotnet@10.0.402 -- dotnet test
  fundamentals/with/With.sln -m:1` passed 3 tests.
- Part 2 `acceptance-tests`: English and Spanish pairs now contain the
  required sections in order. `mise exec dotnet@10.0.402 -- dotnet test
  testing/acceptance-tests/AcceptanceTests.sln -m:1` passed 2 tests.
- Part 2 `working-without-mocks`: English and Spanish pairs now contain the
  required sections in order, including the in-memory fake before the first
  test. `mise exec dotnet@10.0.402 -- dotnet test
  testing/working-without-mocks/WorkingWithoutMocks.sln -m:1` passed 2 tests.
- Part 2 `scaling-acceptance-tests`: English and Spanish pairs now contain
  the required sections in order, with the API driver introduced before the
  first scenario. `mise exec dotnet@10.0.402 -- dotnet test
  testing/scaling-acceptance-tests/ScalingAcceptanceTests.sln -m:1` passed 3
  tests.

## Completed increment: Part 4 delivery references

### Task 1: Align delivery examples with v15
- [x] Update the Dockerfile and English/Spanish containers chapter from v14 to v15.
- [x] Update the CI/CD workflow examples and local publish commands in both languages.
- [x] Mark the integration-testing topics that come from the roadmap image and link the roadmap source in both Part 4 indexes.
- [x] Verify all active delivery examples use the same project path as `.github/workflows/ci.yml`.
- Verify: `git diff --check` and all 142 Markdown files pass the local link check. The v15 Docker image built and served OpenAPI with HTTP 200; its image size is 365 MB.
- Files: `qa/containers/**`, `qa/ci-cd/**`, `qa/README.md`, `qa/README.es.md`, `SPEC-phase4.md`, `tasks/plan.md`, `tasks/todo.md`.
- Dependency: Part 3 v15.

## Completed increment: Phase 2 specification

### Task 1: Record Phase 2 scope and acceptance evidence
- [x] Add `SPEC-phase2.md` from the approved goal and the four existing chapters.
- [x] Record independent solutions, bilingual pairs and local test evidence.

## Completed increment: Part 1 chapter order

### Task 1: Move DI and mocking earlier in the reading flow
- [x] Place Dependency Injection and Mocking directly after Classes in the Part 1 README index.
- [x] Keep records, `with`, inheritance and composition after them; renumber the full index.
- [x] Update English and Spanish next-chapter links to follow the same sequence.
- Verify: all local README and Part 1 chapter links resolve; all 25 bilingual pairs remain linked.
- Files: `README.md`, `SPEC-phase1.md`, `fundamentals/**` next-chapter links, `tasks/plan.md`, `tasks/todo.md`.
- Dependency: none.

## Completed increment: Part 3 v15 SignalR

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
