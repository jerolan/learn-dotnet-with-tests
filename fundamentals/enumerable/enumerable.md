# IEnumerable

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/enumerable)

`IEnumerable<T>` is the contract for a sequence: something a `foreach` can walk one item at a time. Arrays and lists implement it, and a method can produce one lazily with `yield return`. The lazy part is what makes infinite sequences possible.

We will build three sequences: the natural numbers, the even numbers up to a limit, and a sequence that records when it runs. The finished solution is in `fundamentals/enumerable`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Enumerable`, an MSTest project in `tests/Enumerable.Tests`, a solution and a project reference.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Enumerable.Tests;

[TestClass]
public sealed class SequenceTests
{
    [TestMethod]
    public void Naturals_FirstThreeAreZeroOneTwo()
    {
        var first = new List<int>();

        foreach (var number in Sequence.Naturals())
        {
            first.Add(number);

            if (first.Count == 3)
            {
                break;
            }
        }

        Assert.AreEqual(3, first.Count);
        Assert.AreEqual(0, first[0]);
        Assert.AreEqual(1, first[1]);
        Assert.AreEqual(2, first[2]);
    }
}
```

The `break` is part of the specification. The sequence is infinite; the test takes only what it needs.

## Try to run the test

```text
error CS0246: The type or namespace name 'Sequence' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace Enumerable;

public static class Sequence
{
    public static IEnumerable<int> Naturals()
    {
        yield break;
    }
}
```

The code compiles and the test fails with `Expected:<3>. Actual:<0>`. The method returns an empty sequence.

## Write enough code to make it pass

```csharp
public static IEnumerable<int> Naturals()
{
    var current = 0;

    while (true)
    {
        yield return current;
        current++;
    }
}
```

Green. `yield return` produces one value and pauses the method; the next `MoveNext` resumes after it. The `while (true)` would run forever if the caller kept asking, but the `break` in the test stops after the third value.

## How it works

`foreach` does not know about `yield`. It calls `GetEnumerator()` on the sequence, then `MoveNext()` and `Current` until `MoveNext()` returns `false`, and finally disposes the enumerator. The compiler turns the method with `yield return` into a class that implements that protocol.

The method body does not run when you call `Sequence.Naturals()`. It runs when the enumeration starts. Each call to `Naturals()` creates a fresh enumerable; each `foreach` starts the sequence from the beginning.

## Repeat for new requirements

### A finite sequence

Write the test:

```csharp
[TestMethod]
public void Evens_UpToTen_ReturnsTheEvenNumbers()
{
    var evens = new List<int>();

    foreach (var number in Sequence.Evens(10))
    {
        evens.Add(number);
    }

    CollectionAssert.AreEqual(new[] { 0, 2, 4, 6, 8, 10 }, evens);
}
```

Add the method with an empty body first:

```csharp
public static IEnumerable<int> Evens(int max)
{
    yield break;
}
```

The test fails with an empty collection. Implement it:

```csharp
public static IEnumerable<int> Evens(int max)
{
    for (var i = 0; i <= max; i += 2)
    {
        yield return i;
    }
}
```

Green. The same `yield return` works with a finite loop. The method produces values only when the caller asks for them.

### Deferred execution

Write the test:

```csharp
[TestMethod]
public void Tracked_DoesNotRunUntilEnumerated()
{
    var log = new List<int>();
    var sequence = Sequence.Tracked(log);

    Assert.AreEqual(0, log.Count);

    using var enumerator = sequence.GetEnumerator();

    Assert.AreEqual(0, log.Count);

    enumerator.MoveNext();

    Assert.AreEqual(1, log.Count);
    Assert.AreEqual(1, enumerator.Current);
}
```

Add the method with a placeholder, watch the test fail, then implement:

```csharp
public static IEnumerable<int> Tracked(List<int> log)
{
    log.Add(1);
    yield return 1;

    log.Add(2);
    yield return 2;
}
```

Green. The first two assertions prove that creating the sequence and getting the enumerator do not run the body. The first `MoveNext()` runs it up to the first `yield return`, which is why the log has one entry.

If you enumerate `Tracked` twice, the log gets the entries again. The sequence is not a cached list; it is a recipe that runs each time.

## Refactor

The three methods return `IEnumerable<int>`, not a concrete collection. That is the smallest contract the caller needs: it can iterate, and it cannot assume indexing or mutation.

A few habits make sequences safer:

- Never return `null` for a sequence. Return `Array.Empty<T>()` or `Enumerable.Empty<T>()` instead, so callers can always iterate.
- Be aware of how many times a sequence is enumerated. A lazy sequence with side effects runs them again on every pass.
- If the caller needs random access or a stable snapshot, materialize the sequence into a list at the boundary.
- Infinite sequences need a consumer that stops, such as the `break` in the first test. LINQ's `Take`, coming in a later chapter, is the usual way to express that limit.

## Wrapping up

What we have covered:

- `IEnumerable<T>` as the contract for iteration.
- `yield return` and `yield break`.
- The `GetEnumerator`/`MoveNext`/`Current` protocol behind `foreach`.
- An infinite sequence consumed with a `break`.
- A finite sequence built with a `for` loop.
- Deferred execution and the fact that each enumeration starts again.

The chapter follows the loop discipline of the [Iteration chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/iteration): a sequence is produced step by step, and the caller decides when to stop. The `yield` state machine is the .NET way to express that.

Next: [IQueryable](../queriable.md).

