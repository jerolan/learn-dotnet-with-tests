# The with Keyword

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/with)

A record's properties are init-only: once the object exists, you cannot assign them. The `with` expression is how you make a changed copy. It reads like a mutation and behaves like a copy, which is what immutable data needs.

The finished solution is in `fundamentals/with`. If you are building it from scratch, use the same layout as the previous chapters: a class library in `src/WithKeyword`, an MSTest project in `tests/WithKeyword.Tests`, a solution and a project reference.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WithKeyword.Tests;

[TestClass]
public sealed class RecordCopyTests
{
    [TestMethod]
    public void With_CreatesAModifiedCopy()
    {
        var original = new Person("Ada", 36);

        var older = original with { Age = 37 };

        Assert.AreEqual(36, original.Age);
        Assert.AreEqual(37, older.Age);
        Assert.AreEqual("Ada", older.Name);
    }
}
```

## Try to run the test

Run `dotnet test`:

```text
error CS0246: The type or namespace name 'Person' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

```csharp
namespace WithKeyword;

public record Person(string Name, int Age);
```

Run the tests. Green.

There was no behavioral red this time: `with` is a language feature that works as soon as the record exists. The test still earned its place, because it pins the behavior we care about: the original keeps its age and the copy has the new one.

## How it works

`original with { Age = 37 }` does three things:

1. Copies every property from `original` into a new record.
2. Applies the assignments in the initializer. Here, `Age` becomes `37`.
3. Returns the new record.

`original` is untouched. The expression does not mutate the object; it builds a second one. `older` and `original` are different objects, and records compare by value, so they are not equal because their ages differ.

You can change more than one property in the same initializer:

```csharp
var renamed = original with { Name = "Grace", Age = 37 };
```

Properties you do not name are copied unchanged.

## Repeat for new requirements

### An empty initializer makes an equal copy

Write the test:

```csharp
[TestMethod]
public void With_AnEmptyInitializer_CreatesAnEqualCopy()
{
    var original = new Person("Ada", 36);

    var copy = original with { };

    Assert.AreEqual(original, copy);
}
```

It passes. An empty `with` copies everything and changes nothing, and record equality says the copy equals the original. This is a useful boundary: it separates "same values" from "same object".

### Nested records

Write the test:

```csharp
[TestMethod]
public void With_NestedRecord_UpdatesOnlyTheInnerValue()
{
    var customer = new Customer("Ada", new Address("London"));

    var moved = customer with { Home = customer.Home with { City = "Paris" } };

    Assert.AreEqual("London", customer.Home.City);
    Assert.AreEqual("Paris", moved.Home.City);
    Assert.AreEqual("Ada", moved.Name);
}
```

Run it. The compiler says `Customer` does not exist. Add the records:

```csharp
public record Address(string City);

public record Customer(string Name, Address Home);
```

Green. The inner `with` creates a new address, and the outer `with` creates a new customer that points at the new address. The original customer still points at the London address.

If you only wrote `customer with { Home = new Address("Paris") }`, you would replace the whole address. The nested form is useful when the address has more fields and you want to change one.

## Refactor

`with` makes a shallow copy. It copies the values of the properties, and for a reference-type property the value is the reference. If a record holds a mutable list, both records see changes made through that list:

```csharp
public record Team(string Name, List<string> Members);

var team = new Team("A", new List<string> { "Ada" });
var copy = team with { Name = "B" };

copy.Members.Add("Grace");

// team.Members now has two entries as well
```

This is not a bug in `with`; it is what "copy the reference" means. If the collection has to be independent, copy it explicitly:

```csharp
var copy = team with { Name = "B", Members = new List<string>(team.Members) };
```

The same rule applies to any mutable object inside a record. Keep records shallow and their members immutable when you can.

`with` works on records and, since C# 10, on structs. It keeps the runtime type: a `with` on a derived record returns the derived type, with the properties you named.

## Wrapping up

What we have covered:

- `with` as non-destructive update for records.
- The original object stays unchanged and the result is a new object.
- An empty initializer returns an equal copy.
- Nested `with` expressions for changes inside a nested record.
- Shallow copy semantics and how to copy a mutable collection explicitly.

The chapter builds on the value semantics of [Structs and Records in Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/structs-methods-and-interfaces): the data stays immutable and each change produces a new value. The `with` expression is the C# syntax for that idea.

Next: [Inheritance and Interfaces](../inheritance.md).

