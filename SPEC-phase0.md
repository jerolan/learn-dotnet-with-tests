# Spec: Phase 0 - Scaffold (moves only)

ASSUMPTIONS:
1. Phase 0 is moves-only, no chapter prose rewritten.
2. 25 README-linked files move flat into fundamentals/ preserving filenames.
3. decimals.md (0 bytes, unlinked) is removed with git rm.
4. Chapter template lives at fundamentals/TEMPLATE.md.
5. CI is new .github/workflows/ci.yml on ubuntu-latest running dotnet test.
6. Local SDK is mise dotnet 10.0.402; CI SDK version is open question.
7. README keeps prose, only links repointed to fundamentals/name.md.

## Objective
Execute Goal Phase 0: flat root with 26 md files becomes book skeleton without changing teaching content.
Success: every README link resolves, orphan gone, CI green on main, git log shows moves only.

## Tech Stack
- MyFirstDotNetApp solution net6.0 MSTest 2.2.8 Moq 4.20.70 Test SDK 17.1.0.
- SDK local 10.0.402 via mise dotnet-root; CI SDK TBD.
- GitHub Actions ubuntu-latest.

## Commands
- Build: dotnet build MyFirstDotNetApp/MyFirstDotNetApp.sln
- Test: dotnet test MyFirstDotNetApp/MyFirstDotNetApp.sln
- Links: check every README md link target exists on disk.

## Project Structure
- README.md updated to fundamentals/ links plus Parts 2-4 placeholders
- fundamentals/ holds 25 moved chapters flat plus TEMPLATE.md
- testing/ build-an-app/ qa/ placeholders with .gitkeep
- MyFirstDotNetApp untouched in Phase 0
- .github/workflows/ci.yml new

## Code Style
Moves use git mv to preserve history. Only README link targets change.

## Testing Strategy
No new tests. Verify: dotnet test passes, all README links resolve, decimals.md absent, CI workflow valid and green.

## Boundaries
Always: git mv/git rm, clean tree, atomic commits, verify links plus dotnet test.
Ask first: edits inside chapter bodies, TFM or package upgrades, CI trigger changes, file renames.
Never: migrate MSTest to xUnit, rewrite prose, commit secrets, add per-chapter solutions now.

## Success Criteria
- All 25 README links point to fundamentals files and exist.
- decimals.md absent from tree and index.
- fundamentals/TEMPLATE.md has 8 LGWT sections in order.
- ci.yml exists and runs dotnet test.
- dotnet test passes locally.
- Chapter diffs are renames only, README diff links only.
- No TFM or framework migration changes.

## Open Questions
1. Flat fundamentals files vs per-chapter folders?
2. Template path fundamentals/TEMPLATE.md vs root?
3. CI SDK pin 6.0.x vs 8 vs 10, triggers main only or all?
4. README minimal repoint vs full 4-part TOC with marks now?
5. One scaffold commit vs split commits?
6. Empty testing/build-an-app/qa placeholders now or later?

Capability map: phase0-scaffold then phase1-fundamentals then phase2-testing then phase3-app then phase4-qa. This spec covers only phase0-scaffold.
