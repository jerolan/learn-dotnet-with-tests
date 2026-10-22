# Types and Variables

Why it matters for backend .NET: request parsing, money, flags, and text all hinge on using the right type. This chapter makes int, double, bool, and string behavior explicit with tests.

## Write the test first

Add TypeExamplesTests covering Add for ints, IsEven for parity, and Describe for tagged text output.

## Try to run the test

Run dotnet test in fundamentals slash types. It fails because TypeExamples does not exist yet.

## Write the minimal amount of code for the test to run and check the failing test output

Create the Types class library and reference it from Types.Tests. Confirm the failure is about the missing type.

## Write enough code to make it pass

Add TypeExamples with Add, IsEven, and Describe using a switch expression. Keep double handling exact in this slice; tolerance comes later if needed.

## Refactor

Keep three small static methods with no shared state. Keep tests to one behavior each.

## Repeat for new requirements

Add a Describe case for double and bool, watch it fail, then extend the switch. Same loop, wider coverage.

## Wrapping up

Types are now executable knowledge, not prose. Control flow builds on these predicates next.
