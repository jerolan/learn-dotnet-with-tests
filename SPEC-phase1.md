# Spec: Phase 1 - Part 1 Fundamentals via TDD

Derived from goal-objective.md, no new questions.

## Decisions locked from goal file
- Chapters live as fundamentals folders: fundamentals slash name slash name.md plus name.es.md plus solution.
- Rationale: target structure says one solution per chapter, ground rule says chapter equals runnable solution alone.
- Bilingual pair: name.md English, name.es.md Spanish, same sections, README indexes both.
- Rationale: ground rule bilingual, every chapter is a pair.
- DI plus mocking early in Part 1, deepening later in Part 3 app.
- Rationale: roadmap mapping plus Phase 1 order note positions 9-10.
- MSTest stays, net6.0 stays unless a chapter needs newer in its own PR.

## Chapter order from goal Phase 1
- install, hello-world, types, decimal, declarations, control-flow, exceptions
- methods, classes, dependency-injection, mock
- records, with, inheritance, composition
- collections, generics, enumerable, linq, queriable, ref
- tasks, async, io, using

Dependency Injection and mocking follow the first class-based chapters, before the
remaining object-oriented topics and collections, matching the requested early
placement in Learn Go with Tests.

## Success Criteria
- Every fundamentals chapter folder has runnable solution with passing dotnet test.
- Every chapter bilingual and template-ordered, CI green.

## Status: complete (2026-10-21)

- All 25 indexed chapters have a folder with `<name>.md`, `<name>.es.md` and a standalone `.sln`.
- 26 solutions total (the 25 chapters plus `MyFirstDotNetApp`), 140 passing tests.
- CI run `39707185992` passed on the full tree with the 6.0.x SDK.
- The local link checker reports 0 broken links; only `fundamentals/TEMPLATE.md` remains at the folder root.
- The README index is ordered by dependency and marks every fundamentals chapter as must-know.
