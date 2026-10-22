# Exceptions

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/exceptions)

The signup form receives `"42"` and should produce the age `42`. It accepts whole numbers from 0 to 120 and rejects other input with an `InvalidAgeException` the caller can handle.

The first call cannot compile because `Age` does not exist:

```csharp
var age = Age.Parse("42");
```

Running `dotnet test` reports `error CS0103: The name 'Age' does not exist in the current context`. We will use the compiler and failing tests to build the parser's contract. The finished solution is in `fundamentals/exceptions`. If you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Exceptions`, an MSTest project in `tests/Exceptions.Tests`, a solution and a project reference.

## How it works

`int.Parse` converts text to an integer, but it throws `FormatException` when the text is malformed. `int.TryParse` reports success as a `bool` and writes the parsed integer to an `out` variable. A domain exception lets the parser state which invalid input its caller must handle.

## How to test

The parser only converts an input string to an age or rejects it. It has no file, network or clock dependency, so a unit test can call it directly. The tests will check both the returned age and the exception type for invalid input.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Exceptions.Tests;

[TestClass]
public sealed class AgeTests
{
    [TestMethod]
    public void Parse_Number_ReturnsTheAge()
    {
        Assert.AreEqual(42, Age.Parse("42"));
    }
}
```

## Try to run the test

Run `dotnet test`:

```text
error CS0103: The name 'Age' does not exist in the current context
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace Exceptions;

public static class Age
{
    public static int Parse(string text)
    {
        return 0;
    }
}
```

The test fails with `Expected:<42>. Actual:<0>`.

## Write enough code to make it pass

Let the framework parse the number:

```csharp
public static int Parse(string text)
{
    return int.Parse(text);
}
```

Green. `int.Parse` also throws `FormatException` when the text is not a number. That is useful behavior, but it is the wrong exception for our callers.

## Refactor

The valid path is one call to `int.Parse`, so there is nothing to extract yet. Run the test again; it remains green. Next, a failing test will define the exception type for invalid text.

## Repeat for new requirements

### Invalid text needs a domain error

Write the test for the error path:

```csharp
[TestMethod]
public void Parse_Text_ThrowsInvalidAge()
{
    Assert.ThrowsException<InvalidAgeException>(() => Age.Parse("abc"));
}
```

Run it. The test fails because the method threw the wrong exception:

```text
Assert.ThrowsException failed. Threw exception FormatException, but exception InvalidAgeException was expected.
```

This is the point of the test. The caller asked for an age; the fact that the implementation used `int.Parse` is an implementation detail. Introduce a domain exception:

```csharp
namespace Exceptions;

public sealed class InvalidAgeException : Exception
{
    public InvalidAgeException(string message)
        : base(message)
    {
    }
}
```

Then use `int.TryParse`, which returns a `bool` instead of throwing, and throw our own exception:

```csharp
public static int Parse(string text)
{
    if (!int.TryParse(text, out var age))
    {
        throw new InvalidAgeException($"'{text}' is not a number");
    }

    return age;
}
```

Green. `out var age` declares the variable at the call site and lets `TryParse` assign it. When the method returns `false`, we never use `age` because the guard throws first.

### Out-of-range ages

The form has a range. Write the test for a negative age:

```csharp
[TestMethod]
public void Parse_NegativeAge_ThrowsInvalidAge()
{
    Assert.ThrowsException<InvalidAgeException>(() => Age.Parse("-1"));
}
```

It fails with `No exception thrown. InvalidAgeException exception was expected.` because `-1` parses successfully. Add the lower bound:

```csharp
if (age < 0)
{
    throw new InvalidAgeException($"'{text}' is outside the valid age range");
}

return age;
```

Green. Now the upper bound. Write a test with `130`, watch it fail for the same reason, and extend the guard:

```csharp
private const int MaxAge = 120;

public static int Parse(string text)
{
    if (!int.TryParse(text, out var age))
    {
        throw new InvalidAgeException($"'{text}' is not a number");
    }

    if (age < 0 || age > MaxAge)
    {
        throw new InvalidAgeException($"'{text}' is outside the valid age range");
    }

    return age;
}
```

Green.

The three invalid cases check the same contract with different inputs, so they belong in one table:

```csharp
[DataTestMethod]
[DataRow("abc")]
[DataRow("-1")]
[DataRow("130")]
public void Parse_InvalidAge_ThrowsInvalidAge(string text)
{
    Assert.ThrowsException<InvalidAgeException>(() => Age.Parse(text));
}
```

### Keep the error boundary narrow

The production code is already small: one parse, two guards, one return. The constant gives the upper bound a name, and the two messages say which rule failed.

`Assert.ThrowsException` is the test-side tool. In production, a caller that expects invalid input uses `try`/`catch`:

```csharp
try
{
    var age = Age.Parse(input);
    Console.WriteLine($"Age: {age}");
}
catch (InvalidAgeException ex)
{
    Console.WriteLine(ex.Message);
}
```

Catch the specific exception you can handle. A bare `catch (Exception)` at a lower layer hides bugs that have nothing to do with the input. The top-level boundary of a process is the place for a broad catch, and even there you log the exception before continuing.

When invalid input is normal rather than exceptional, prefer a method that returns a result instead of throwing. `int.TryParse` is the framework example: a form can call it on every keystroke without paying for exceptions. Our `Parse` sits at the boundary where invalid input is a contract violation.

## Wrapping up

What we have covered:

- A domain exception type that callers can catch without knowing the parser's implementation.
- `int.TryParse` and an `out` variable.
- Guard clauses for an invalid number and an out-of-range value.
- `Assert.ThrowsException` for the error path, and `try`/`catch` for production callers.
- The rule of thumb: exceptions for contract violations, a try-pattern when invalid input is expected.

The chapter follows the error-path habit of [Pointers and Errors in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/pointers-and-errors): the invalid case is a requirement with its own test. .NET expresses that requirement as an exception type instead of a returned error value.

Next: [Properties and Methods](../methods/methods.md).

