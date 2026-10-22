# Control Flow

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/control-flow)

Control flow decides which code runs and how many times. We will build a text repeater, then a sum, then FizzBuzz. Every step starts with a test, the same way the previous chapters did.

The finished solution is in `fundamentals/control-flow`. If you are building it from scratch, set it up like the others: a class library in `src/ControlFlow`, an MSTest project in `tests/ControlFlow.Tests`, a solution and a project reference.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ControlFlow.Tests;

[TestClass]
public sealed class ControlFlowExamplesTests
{
    [TestMethod]
    public void RepeatOnce_ReturnsTheText()
    {
        Assert.AreEqual("a", ControlFlowExamples.Repeat("a"));
    }
}
```

## Try to run the test

Run `dotnet test`:

```text
error CS0103: The name 'ControlFlowExamples' does not exist in the current context
```

## Write the minimal amount of code for the test to run and check the failing test output

Create the class and return the argument:

```csharp
namespace ControlFlow;

public static class ControlFlowExamples
{
    public static string Repeat(string text)
    {
        return text;
    }
}
```

Run the tests. Green. The first requirement was small enough that the minimum is also the real implementation.

## Repeat for new requirements

### Repeat the text a number of times

The next requirement is the one that needs a loop. Write the test:

```csharp
[TestMethod]
public void Repeat_ReturnsTheTextRepeated()
{
    Assert.AreEqual("aaa", ControlFlowExamples.Repeat("a", 3));
}
```

Run the tests. The compiler says:

```text
error CS1501: No overload for method 'Repeat' takes 2 arguments
```

Change the signature to accept a count. Keep the old body for a moment so the code compiles:

```csharp
public static string Repeat(string text, int times)
{
    return text;
}
```

The old test no longer compiles because `Repeat` has no one-argument form anymore. That test described the previous shape of the method; replace it with the two-argument test. Run the suite and the new test fails with `Expected:<aaa>. Actual:<a>`.

Now write the loop:

```csharp
using System.Text;

namespace ControlFlow;

public static class ControlFlowExamples
{
    public static string Repeat(string text, int times)
    {
        var result = new StringBuilder();

        for (var i = 0; i < times; i++)
        {
            result.Append(text);
        }

        return result.ToString();
    }
}
```

Green. The `for` loop has three parts:

- `var i = 0` runs once, before the loop.
- `i < times` is checked before every iteration.
- `i++` runs after every iteration.

`StringBuilder` collects the pieces without allocating a new string for every append. For three characters a plain concatenation would also work, but the builder is the habit you want when the count comes from user input.

### A boundary: zero repetitions

Write a test for the edge case:

```csharp
[TestMethod]
public void Repeat_ZeroTimes_ReturnsEmpty()
{
    Assert.AreEqual(string.Empty, ControlFlowExamples.Repeat("a", 0));
}
```

It passes without changing the code, because `i < 0` is false on the first check and the loop body never runs. A test that passes immediately is still useful: it pins the boundary so a later refactor cannot break it silently.

### Sum with a while loop

Write the test:

```csharp
[TestMethod]
public void SumTo_AddsTheNumbersUpToTheLimit()
{
    Assert.AreEqual(10, ControlFlowExamples.SumTo(4));
}
```

The compiler says `SumTo` is missing. Add it with a placeholder:

```csharp
public static int SumTo(int limit)
{
    return 0;
}
```

The test fails with `Expected:<10>. Actual:<0>`. Now write the loop:

```csharp
public static int SumTo(int limit)
{
    var sum = 0;
    var current = 1;

    while (current <= limit)
    {
        sum += current;
        current++;
    }

    return sum;
}
```

Green. A `for` loop would work here too, because the number of iterations is known. The `while` form keeps the condition and the increment apart, which reads better when the stopping rule depends on the data instead of a counter.

Add the boundary:

```csharp
[TestMethod]
public void SumTo_Zero_ReturnsZero()
{
    Assert.AreEqual(0, ControlFlowExamples.SumTo(0));
}
```

It passes. The loop condition is false before the first iteration, so the method returns `0`.

### FizzBuzz with conditions

FizzBuzz is the classic test of conditional flow: multiples of three say `Fizz`, multiples of five say `Buzz`, multiples of both say `FizzBuzz`, and everything else says the number.

Write the table first:

```csharp
[DataTestMethod]
[DataRow(1, "1")]
[DataRow(3, "Fizz")]
[DataRow(5, "Buzz")]
[DataRow(15, "FizzBuzz")]
public void FizzBuzz_ReturnsTheRightWord(int number, string want)
{
    Assert.AreEqual(want, ControlFlowExamples.FizzBuzz(number));
}
```

Add the method with a placeholder:

```csharp
public static string FizzBuzz(int number)
{
    return string.Empty;
}
```

The first row fails: `Expected:<1>. Actual:<>`. Write the conditions:

```csharp
public static string FizzBuzz(int number)
{
    if (number % 15 == 0)
    {
        return "FizzBuzz";
    }

    if (number % 3 == 0)
    {
        return "Fizz";
    }

    if (number % 5 == 0)
    {
        return "Buzz";
    }

    return number.ToString();
}
```

Green. The order matters. `15` is divisible by both `3` and `5`, so the `FizzBuzz` check has to come first; otherwise the `Fizz` branch would return early. The `%` operator gives the remainder, and `number % 3 == 0` is true for multiples of three.

## Refactor

The three `if` statements all test the same pair of remainders. C# can match on a tuple in a switch expression:

```csharp
public static string FizzBuzz(int number)
{
    return (number % 3, number % 5) switch
    {
        (0, 0) => "FizzBuzz",
        (0, _) => "Fizz",
        (_, 0) => "Buzz",
        _ => number.ToString()
    };
}
```

Run the tests. Green, and the four cases are visible at once. The `_` is a discard: it matches any value and says we do not use it. The switch expression returns a value, so there is no `break` and no fall-through.

## Wrapping up

What we have covered:

- A `for` loop with an initializer, a condition and an increment.
- A `while` loop whose condition controls the iterations.
- Boundary tests for zero repetitions and a zero limit.
- `if` chains and the `%` operator.
- A switch expression with tuple patterns and discards.

The chapter follows the [Iteration chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/iteration): repeat a piece of text, let the count arrive as a new requirement, and keep the loop honest with a boundary test. FizzBuzz and the switch expression are the .NET additions.

Next: [Exceptions](../exceptions/exceptions.md).

