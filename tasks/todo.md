# Todo: Phase 0 Scaffold

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
