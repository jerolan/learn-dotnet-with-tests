# The ref Keyword

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/ref)

A normal parameter is a copy. `ref` passes a reference to the caller's variable, `out` declares a value the method must produce, and `in` passes a read-only reference. Used well, they avoid copies; used casually, they make code harder to follow.

We will cover the four forms with small, testable examples: swap two variables, try to divide, return a reference to an array slot, and pass a struct by readonly reference. The finished solution is in `fundamentals/ref`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Ref`, an MSTest project in `tests/Ref.Tests`, a solution and a project reference.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ref.Tests;

[TestClass]
public sealed class ValuesTests
{
    [TestMethod]
    public void Swap_ExchangesTheValues()
    {
        var left = 1;
        var right = 2;

        Values.Swap(ref left, ref right);

        Assert.AreEqual(2, left);
        Assert.AreEqual(1, right);
    }
}
```

## Try to run the test

```text
error CS0103: The name 'Values' does not exist in the current context
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace Ref;

public static class Values
{
    public static void Swap(ref int left, ref int right)
    {
    }
}
```

The test compiles and fails because both variables keep their original values. The method received references but did nothing with them.

## Write enough code to make it pass

```csharp
public static void Swap(ref int left, ref int right)
{
    var temp = left;
    left = right;
    right = temp;
}
```

Green. `ref` on the parameter means the method works on the caller's variables, not on copies. Without it, `left` and `right` inside the method would be independent copies and the caller would see no change.

## Repeat for new requirements

### An output parameter

Write the test:

```csharp
[TestMethod]
public void TryDivide_ByDivisor_ReturnsTheQuotient()
{
    var result = Values.TryDivide(10, 2, out var quotient);

    Assert.IsTrue(result);
    Assert.AreEqual(5, quotient);
}
```

Add the method with an `out` parameter:

```csharp
public static bool TryDivide(int dividend, int divisor, out int quotient)
{
    quotient = dividend / divisor;
    return true;
}
```

Green for the valid case. Now the zero divisor:

```csharp
[TestMethod]
public void TryDivide_ByZero_ReturnsFalseAndZero()
{
    var result = Values.TryDivide(10, 0, out var quotient);

    Assert.IsFalse(result);
    Assert.AreEqual(0, quotient);
}
```

Handle the guard:

```csharp
public static bool TryDivide(int dividend, int divisor, out int quotient)
{
    if (divisor == 0)
    {
        quotient = 0;
        return false;
    }

    quotient = dividend / divisor;
    return true;
}
```

Green. An `out` parameter must be assigned before the method returns; the compiler rejects a path that leaves it unset. The caller does not need to initialize it: `out var quotient` declares it at the call site.

### A ref return

Write the test:

```csharp
[TestMethod]
public void At_ReturnsARefToTheSlot()
{
    var values = new[] { 1, 2, 3 };

    ref var slot = ref Values.At(values, 1);
    slot = 20;

    Assert.AreEqual(20, values[1]);
}
```

Add the method:

```csharp
public static ref int At(int[] values, int index)
{
    return ref values[index];
}
```

Green. `ref int` is a ref return: the method returns a reference to the array element, not a copy of its value. `ref var slot = ref ...` is a ref local. Assigning `20` to `slot` writes through to the array.

A ref return must refer to a location that outlives the call, such as an array element or a field. Returning a reference to a local variable would leave a dangling reference, and the compiler rejects it.

### A readonly reference

Write the test:

```csharp
[TestMethod]
public void ManhattanDistance_AddsTheCoordinateDifferences()
{
    var origin = new Point(0, 0);

    Assert.AreEqual(7, origin.ManhattanDistance(new Point(3, 4)));
}
```

Add the readonly struct and the `in` parameter:

```csharp
public readonly struct Point
{
    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }

    public int Y { get; }

    public int ManhattanDistance(in Point other)
    {
        return Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
    }
}
```

Green. `in` passes a readonly reference. The method avoids copying the struct, and the compiler prevents it from assigning to `other`. For a large struct in a hot path, that matters; for a two-int point, it is mostly a syntax lesson. The caller can pass the argument with or without the `in` keyword.

## Refactor

The examples are deliberately small. The rule of thumb is to use these keywords only when they buy something:

- `ref` when the method must mutate the caller's variable, as in `Swap`.
- `out` for a method that returns success plus a value, the `Try...` pattern.
- `in` for a large readonly struct that you do not want to copy.
- A ref return when the caller needs to write into a slot, such as an array or a field.

For ordinary values, return a new value. Immutable data and clear return types are easier to reason about than a method that mutates several arguments.

## Wrapping up

What we have covered:

- `ref` parameters and mutating the caller's variables.
- `out` parameters and the `Try...` pattern.
- Ref returns and ref locals.
- `in` for a readonly reference to a struct.
- When the keywords help and when a normal return is clearer.

The chapter follows the reference behavior of [Pointers in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/pointers-and-errors): a function can work on the caller's value instead of a copy. C# makes the choice explicit with `ref`, `out` and `in` instead of exposing pointer syntax.

Next: [Tasks](../tasks/tasks.md).

