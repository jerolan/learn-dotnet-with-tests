# Spec: Phase 1 - Part 1 Fundamentals via TDD (draft)

ASSUMPTIONS:
1. Phase 0 stays as committed: 25 chapters flat in fundamentals, README repointed, CI 6.0.x.
2. Phase 1 rewrites each chapter on the LGWT template, bilingual pair, with its own runnable solution.
3. Order follows goal: grammar first, then methods-classes-records, then DI and mocking early, then collections and advanced topics.
4. MSTest stays, net6.0 stays unless a chapter justifies an upgrade in its own PR.
5. CI with 6.0.x covers new solutions with no workflow change.

## Objective
Rewrite Part 1 chapters in TDD rhythm so every README-linked fundamentals chapter has a runnable dotnet test solution, template-ordered prose, and English plus Spanish pair.

## Proposed chapter order
- install, hello-world, types, decimal, declarations, control-flow, exceptions
- methods, classes, records, with, inheritance, composition
- dependency-injection, mock (early per LGWT positions 9-10)
- collections, enumerable, queriable, linq, generics, ref
- tasks, async, io, using

## Tech Stack
- net6.0, MSTest 2.2.8, Moq 4.20.70, one solution per chapter.

## Commands
- Build and test per chapter solution with dotnet test.
- Link check for README and bilingual pairs.

## Structure
- fundamentals per-chapter folder or file plus solution, exact layout TBD in plan.
- Each chapter: EN md plus ES md, same sections.

## Testing Strategy
- Each chapter solution green with dotnet test, CI discovers sln files.

## Boundaries
- Always: TDD red-green-refactor narrative, minimal code, bilingual pair.
- Ask first: TFM upgrades, new packages beyond MSTest plus Moq plus Bogus if needed.
- Never: migrate to xUnit, rewrite Part 2-4 scope here.

## Success Criteria
- Every fundamentals chapter has runnable solution with passing tests.
- Every chapter bilingual and template-ordered, CI green.

## Open Questions
1. Flat file plus solution folder versus per-chapter folder?
2. Spanish file naming: name.es.md and README bilingual index format?
3. DI chapter depth versus app deepening split?
Note: draft only, Phase 0 remote CI still pending push approval.
