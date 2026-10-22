# Structs and Records

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/records)

A struct is a value type: assigning it copies its data. A record is a reference type with value equality: two records are equal when their values are equal, even though they are different objects. Both are useful for data, and they behave differently when you pass them around.

We will build a small counter to see the copy, then a person record to see equality. The finished solution is in `fundamentals/records`; if you are building it from scratch, use the same layout as the previous chapters: a class library in `src/Records`, an MSTest project in `tests/Records.Tests`, a solution and a project reference.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Records.Tests;

[TestClass]
public sealed class CounterTests
{
    [TestMethod]
    public void Assignment_CopiesTheStruct()
    {
        var original = new Counter { Value = 1 };
        var copy = original;

        copy.Increment();

        Assert.AreEqual(1, original.Value);
        Assert.AreEqual(2, copy.Value);
    }
}
```

## Try to run the test

Run `dotnet test`:

```text
error CS0246: The type or namespace name 'Counter' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace Records;

public struct Counter
{
    public int Value { get; set; }

    public void Increment()
    {
    }
}
```

The code compiles. The first assertion passes because the original still has `1`; the second fails with `Expected:<2>. Actual:<1>` because `Increment` does nothing.

## Write enough code to make it pass

```csharp
public void Increment()
{
    Value++;
}
```

Green. The interesting part is what did not happen to `original`. `copy = original` copied the struct's fields, so `copy` and `original` are two separate values. Calling `Increment` on the copy changed only the copy.

That is value semantics. A struct is copied when it is assigned, passed to a method, or returned from one. It is a good fit for small values that behave like numbers. Mutable structs are easy to misuse, which is why production code usually makes them `readonly`.

## Repeat for new requirements

### A record compares by value

Write the test:

```csharp
[TestClass]
public sealed class PersonTests
{
    [TestMethod]
    public void TwoPeopleWithTheSameValues_AreEqual()
    {
        Assert.AreEqual(new Person("Ada", 36), new Person("Ada", 36));
    }
}
```

Run the tests. The compiler says `Person` does not exist. Define a positional record:

```csharp
namespace Records;

public record Person(string Name, int Age);
```

Green. `Person` is a reference type, but the compiler generated value equality for it: two different objects with the same `Name` and `Age` are equal. `Assert.AreEqual` uses `Equals`, which the record overrides.

The positional syntax also generates a constructor, init-only properties `Name` and `Age`, a `ToString`, and deconstruction support. The properties can be set in an object initializer, but not after construction.

### Different values are not equal

Write the other half of the contract:

```csharp
[TestMethod]
public void TwoPeopleWithDifferentValues_AreNotEqual()
{
    Assert.AreNotEqual(new Person("Ada", 36), new Person("Grace", 36));
}
```

It passes with the record as it is. The test is still useful: it pins the other side of equality and would fail if a later change made every person compare equal.

### A record can have behavior

Write the test:

```csharp
[TestMethod]
public void IsAdult_ChecksTheAge()
{
    Assert.IsTrue(new Person("Ada", 36).IsAdult);
    Assert.IsFalse(new Person("Tim", 10).IsAdult);
}
```

The compiler says `Person` has no `IsAdult`. Add a computed property to the record:

```csharp
public record Person(string Name, int Age)
{
    public bool IsAdult => Age >= 18;
}
```

Green. A record is not just a bag of fields; it can have properties and methods like any other type. The difference is the equality and the concise syntax.

## Refactor

There is not much to clean up. The counter is mutable on purpose: it makes the copy visible. If you were modeling a value you never change, declare it `readonly struct` so the compiler enforces that every field is readonly.

The record's properties are init-only. Trying to assign one after construction is a compile error:

```csharp
var person = new Person("Ada", 36);
person.Age = 37;
// error CS8852: Init-only property or indexer 'Person.Age' can only be
// assigned in an object initializer, or in a constructor or init accessor
```

That is the setup for the next chapter. To "change" a record you create a new one that shares the unchanged values and replaces the ones you name.

A `record struct` combines the two ideas: value-type copying with the generated equality and printing of a record. Use it for small values where the copy is cheap and you want record equality.

## Wrapping up

What we have covered:

- A struct is a value type; assignment copies its data.
- Mutating a copy does not change the original.
- A record is a reference type with value equality.
- Positional records generate a constructor, init-only properties, equality and printing.
- A record can have computed properties and methods.
- `readonly struct` and `record struct` as the value-type variants.

The chapter follows the struct section of [Structs, Methods and Interfaces in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/structs-methods-and-interfaces), where a type groups data and methods. .NET adds records for data that compares by value and stays immutable.

Next: [The with keyword](../with.md).

