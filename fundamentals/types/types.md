# Types and Variables

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/types)

C# is a statically typed language: every variable has a type, and the compiler knows it before the program runs. Integers work about as you would expect. Let's write an `Add` function to try things out.

The finished solution is in `fundamentals/types`. If you are building it from scratch, set it up the same way as Hello, World: a class library in `src/Types`, an MSTest project in `tests/Types.Tests`, a solution, and a project reference from the tests to the library. Then create `tests/Types.Tests/TypeExamplesTests.cs`.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Types.Tests;

[TestClass]
public sealed class TypeExamplesTests
{
    [TestMethod]
    public void Add()
    {
        var sum = TypeExamples.Add(2, 2);
        var expected = 4;

        Assert.AreEqual(expected, sum);
    }
}
```

`TypeExamples` is going to be a static class, so we can call `Add` without creating an object.

## Try to run the test

Run `dotnet test`.

The project does not compile:

```text
error CS0103: The name 'TypeExamples' does not exist in the current context
```

That is the compiler telling us the type is missing. We have not written it yet.

## Write the minimal amount of code for the test to run and check the failing test output

Write enough code to satisfy the compiler and nothing more:

```csharp
namespace Types;

public static class TypeExamples
{
    public static int Add(int x, int y)
    {
        return 0;
    }
}
```

Now run the tests. The code compiles, and the test fails for the right reason:

```text
  Failed Add [13 ms]
  Error Message:
   Assert.AreEqual failed. Expected:<4>. Actual:<0>.
```

We want to see that failure. It proves the test is connected to the method and is checking the result, not just the fact that the method exists.

## Write enough code to make it pass

In the strictest sense of TDD, we now write the minimal amount of code that makes the test pass. A pedantic programmer could write:

```csharp
public static int Add(int x, int y)
{
    return 4;
}
```

TDD is a sham, right? The test passes.

The problem is that the test only used one pair of numbers. We could add another test with different numbers, but chasing a hard-coded answer with one test at a time becomes a game of cat and mouse.

For now, fix the method properly:

```csharp
public static int Add(int x, int y)
{
    return x + y;
}
```

Run the tests again. Green.

## Refactor

There is not much code to improve. This is a good moment to look at the type signature. `int Add(int x, int y)` says what goes in and what comes out; the names help when you read it in an editor.

C# has XML documentation comments. They show up in IntelliSense and in generated documentation:

```csharp
/// <summary>Adds two integers.</summary>
public static int Add(int x, int y)
{
    return x + y;
}
```

The Go book uses testable examples here. C# does not run examples from doc comments, so we use a table of cases instead. Replace the single `Add` test with:

```csharp
[DataTestMethod]
[DataRow(2, 2, 4)]
[DataRow(1, 5, 6)]
[DataRow(-3, 3, 0)]
public void Add_ReturnsSum(int x, int y, int want)
{
    Assert.AreEqual(want, TypeExamples.Add(x, y));
}
```

`[DataTestMethod]` with `[DataRow]` is the MSTest version of a table-driven test. Each row runs as a separate case, and a hard-coded `return 4` would now fail two of them.

## Repeat for new requirements

### Integer division

Our next requirement is division. Write the test first:

```csharp
[TestMethod]
public void DivideIntegers_TruncatesTheFraction()
{
    Assert.AreEqual(2, TypeExamples.Divide(10, 4));
}
```

Run the tests. The compiler complains that `TypeExamples` does not contain `Divide`.

Add the method with the smallest body that compiles:

```csharp
public static int Divide(int x, int y)
{
    return 0;
}
```

The test now fails with `Expected:<2>. Actual:<0>`. Make it pass:

```csharp
public static int Divide(int x, int y)
{
    return x / y;
}
```

Green. Notice what happened to the fraction: `10 / 4` is `2`, not `2.5`. When both operands are `int`, C# performs integer division and discards the remainder. That is correct for the type, and it is a common source of bugs when you expected a fraction.

### Doubles

If we want the fraction, we need a type that can represent it. Write the test:

```csharp
[TestMethod]
public void DivideDoubles_KeepsTheFraction()
{
    Assert.AreEqual(2.5, TypeExamples.Divide(10.0, 4.0));
}
```

`10.0` and `4.0` are `double` literals, so the call looks for a `Divide` that takes two `double` values. The compiler says there is no such overload.

Add it:

```csharp
public static double Divide(double x, double y)
{
    return x / y;
}
```

Run the tests. Both division tests pass. C# chose the right method from the argument types: `10` is an `int`, `10.0` is a `double`. Two methods with the same name and different parameter types are overloads.

### Booleans

One more type before we move on. Write a test for a yes-or-no question:

```csharp
[TestMethod]
public void IsPositive_ChecksTheSign()
{
    Assert.IsTrue(TypeExamples.IsPositive(5));
    Assert.IsFalse(TypeExamples.IsPositive(-1));
    Assert.IsFalse(TypeExamples.IsPositive(0));
}
```

It fails to compile because `IsPositive` does not exist. Add the method:

```csharp
public static bool IsPositive(int value)
{
    return value > 0;
}
```

Run the tests. Green. The comparison returns a `bool`, and `Assert.IsTrue` and `Assert.IsFalse` check it. Zero is neither positive nor negative, which is why the third assertion is there.

## Wrapping up

What we have covered:

- More practice with the TDD loop: red, green, refactor.
- `int`, `double` and `bool`, and how literal syntax chooses the type.
- Integer division truncates; floating-point division keeps the fraction.
- Method overloading, so `Divide` can serve both numeric types.
- `[DataTestMethod]` and `[DataRow]` for a table of cases.
- XML documentation comments as the C# answer to Go doc.

The chapter follows the [Integers chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/integers): one small `Add` function, a compiler error, a deliberately wrong minimal implementation, then a real one. The table of cases and the extra numeric types are the .NET translation of the same lesson.

Next: [Decimals](../decimal/decimal.md), where `double` stops being good enough.

