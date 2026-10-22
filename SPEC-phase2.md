# Spec: Phase 2 - Testing fundamentals

## Goal

Teach acceptance testing and test-suite design in .NET with MSTest. Show tests at
the HTTP boundary, how to scale those tests, when an in-memory fake is simpler
than a mock, and how to refactor while tests stay green.

## Rules

- Each chapter has an English/Spanish pair with the same sections and reading
  flow, following the section order in `fundamentals/TEMPLATE.md`.
- Each chapter has a standalone solution. Running `dotnet test` in one chapter
  must not depend on another chapter's project.
- Use `net6.0`, MSTest 2.2.8 and the established Moq version. Keep integration
  tests on `WebApplicationFactory`/`TestServer` where the chapter crosses HTTP.
- Keep Respawn, Testcontainers and Docker in the Part 4 integration-tools
  appendix; Part 2 links to that appendix instead of duplicating its lab.
- Use a real in-memory implementation in the "Working Without Mocks" chapter
  because its behavior is the subject of the lesson.

## Chapter map

| # | Chapter | Focus | Acceptance evidence |
|---|---|---|---|
| 1 | `acceptance-tests` | Create and read a Todo through HTTP with `WebApplicationFactory<Program>` | The test observes an HTTP 201 response and the created item; the API runs in TestServer without a machine port |
| 2 | `scaling-acceptance-tests` | Organize HTTP scenarios with a test driver, isolated factory and reusable setup | Tests cover creation, ordering and duplicate IDs through the HTTP boundary |
| 3 | `working-without-mocks` | Replace an interaction mock with a small in-memory fake | Tests cover successful creation and rejection of a blank title using the fake |
| 4 | `refactoring-checklist` | Apply a repeatable checklist while keeping tests green | Tests pin the discount examples used by the checklist |

## Completion

- The four chapters appear in the Part 2 README index, each with an English and
  Spanish file.
- Each chapter contains its own solution and its test project uses MSTest.
- The Part 2 solutions pass `dotnet test` independently.
- The acceptance chapters exercise the API through `WebApplicationFactory`;
  persistence reset and external container setup remain in the Part 4 appendix.
- The root index links the four chapters and their Spanish versions.

## Status: implementation present; template-order alignment pending

The four pairs and four solutions are present. Their tests passed in the full
57-solution run on 2026-10-22: 10 tests passed across Part 2. Local Markdown
links resolve throughout the repository, and the root README indexes both
languages for all four chapters.
