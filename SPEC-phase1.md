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
- methods, classes, records, with, inheritance, composition
- dependency-injection, mock
- collections, enumerable, queriable, linq, generics, ref
- tasks, async, io, using

## Success Criteria
- Every fundamentals chapter folder has runnable solution with passing dotnet test.
- Every chapter bilingual and template-ordered, CI green.
