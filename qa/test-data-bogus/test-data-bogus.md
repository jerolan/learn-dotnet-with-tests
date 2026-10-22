# Test data with Bogus

[You can find all the code for this appendix here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/qa/test-data-bogus)

An order validator checks an email and a placement date. Testing the happy path needs a hundred valid orders: names, emails, totals, timestamps. Writing them by hand turns the test into a data-entry exercise, and when it fails you have to work out whether the validator or the fixture is wrong.

Bogus builds that data from rules. This appendix uses it for the bulk of the valid cases and keeps explicit values for the edges.

## Set up the solution

From the appendix folder:

```sh
dotnet new classlib -n Shop -o src/Shop -f net6.0
dotnet new mstest -n Shop.Tests -o tests/Shop.Tests -f net6.0
dotnet new sln -n TestDataBogus
dotnet sln TestDataBogus.sln add src/Shop/Shop.csproj tests/Shop.Tests/Shop.Tests.csproj
dotnet add tests/Shop.Tests/Shop.Tests.csproj reference src/Shop/Shop.csproj
```

Delete the generated `Class1.cs` and `UnitTest1.cs`. The test project is the only place that will reference Bogus; the production project does not need it.

## Write the test first

Create `tests/Shop.Tests/OrderValidatorTests.cs`:

```csharp
using Bogus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shop;

namespace Shop.Tests;

[TestClass]
public sealed class OrderValidatorTests
{
    [TestMethod]
    public void ValidGeneratedOrdersPass()
    {
        var orders = new Faker<Order>()
            .UseSeed(1234)
            .CustomInstantiator(f => new Order(
                f.Name.FullName(),
                f.Internet.Email(),
                f.Random.Decimal(1m, 500m),
                f.Date.Between(
                    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc))))
            .Generate(100);

        var validator = new OrderValidator();

        foreach (var order in orders)
        {
            CollectionAssert.AreEqual(
                Array.Empty<string>(),
                validator.Validate(order).ToArray(),
                order.ToString());
        }
    }
}
```

The rules describe valid data once. `Generate(100)` applies them a hundred times, and the loop checks every result through the same assertion.

## Try to run the test

```text
error CS0246: The type or namespace name 'Bogus' could not be found (are you missing a using directive or an assembly reference?)
```

Bogus is a test dependency, so add it to the test project and pin the version:

```sh
dotnet add tests/Shop.Tests/Shop.Tests.csproj package Bogus --version 34.0.1
```

Run again:

```text
error CS0246: The type or namespace name 'Order' could not be found (are you missing a using directive or an assembly reference?)
error CS0246: The type or namespace name 'OrderValidator' could not be found (are you missing a using directive or an assembly reference?)
```

Now the compiler is asking for the types the test describes.

## Write enough code to compile

`src/Shop/Order.cs`:

```csharp
namespace Shop;

public sealed record Order(
    string CustomerName,
    string Email,
    decimal Total,
    DateTime PlacedAt);
```

`src/Shop/OrderValidator.cs`:

```csharp
namespace Shop;

public sealed class OrderValidator
{
    public IReadOnlyList<string> Validate(Order order)
    {
        return Array.Empty<string>();
    }
}
```

Run `dotnet test`. Green, with one test. Be suspicious of that green: the validator rejects nothing yet, so the test only proves that the generated data reaches the loop.

## Why CustomInstantiator

Bogus usually creates an object with its parameterless constructor, then fills writable properties named by `RuleFor`. A positional record has neither. If you write the test with `RuleFor`:

```text
System.MissingMethodException: Cannot dynamically create an instance of type 'Shop.Order'.
Reason: No parameterless constructor defined.
```

`CustomInstantiator` hands the faker to a factory you write, so the record keeps its constructor and every field is set once. For a class with settable properties, `RuleFor` remains the shorter form.

## Repeat for new requirements

The first rule: an order without an `@` in its email is invalid. This is an edge case, so give it a literal value instead of generating one:

```csharp
[TestMethod]
public void OrderWithoutEmailIsRejected()
{
    var order = new Order(
        "Ana Torres",
        "ana.example.com",
        12.50m,
        DateTime.UtcNow.AddMinutes(-1));

    var errors = new OrderValidator().Validate(order);

    CollectionAssert.Contains(errors.ToList(), "Email is invalid.");
}
```

Run it. The assertion fails because `Validate` returns an empty list.

Add the check:

```csharp
var errors = new List<string>();

if (!order.Email.Contains('@'))
{
    errors.Add("Email is invalid.");
}

return errors;
```

Green. The generated test still passes, which is the point of keeping the valid fixture broad: a new rule should not break valid data.

The next rule: an order cannot be placed in the future. Write the test with a date one day ahead:

```csharp
[TestMethod]
public void OrderPlacedInTheFutureIsRejected()
{
    var order = new Order(
        "Ana Torres",
        "ana@example.com",
        12.50m,
        DateTime.UtcNow.AddDays(1));

    var errors = new OrderValidator().Validate(order);

    CollectionAssert.Contains(errors.ToList(), "Placed date cannot be in the future.");
}
```

Red. Add the comparison:

```csharp
if (order.PlacedAt > DateTime.UtcNow)
{
    errors.Add("Placed date cannot be in the future.");
}
```

Green.

## The seed does not freeze the clock

A generated fixture is only useful if the same seed produces the same data. Move the generation into a helper so both calls use the same rules:

```csharp
private static List<Order> ValidOrders(int seed)
{
    return new Faker<Order>()
        .UseSeed(seed)
        .CustomInstantiator(f => new Order(
            f.Name.FullName(),
            f.Internet.Email(),
            f.Random.Decimal(1m, 500m),
            f.Date.Past(1).ToUniversalTime()))
        .Generate(100);
}
```

Now write the guarantee as a test:

```csharp
[TestMethod]
public void SameSeedProducesTheSameOrders()
{
    var first = ValidOrders(seed: 1234);
    var second = ValidOrders(seed: 1234);

    CollectionAssert.AreEqual(first, second);
}
```

It still fails:

```text
CollectionAssert.AreEqual failed. (Element at index 0 do not match.)
```

The seed controls the random choices. It does not control `f.Date.Past(1)`, which reads the current time each time the faker runs. Two calls a few milliseconds apart produce different timestamps.

Replace the date rule in the helper with a fixed window:

```csharp
f.Date.Between(
    new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
    new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc))
```

Run again. Green. If the validator compared against a clock you could not fix, you would inject that clock instead of reading `DateTime.UtcNow` inside the validator.

## Refactor

The generation rules now live in one helper, and both tests that need valid orders call it:

```csharp
private static List<Order> ValidOrders(int seed)
{
    return new Faker<Order>()
        .UseSeed(seed)
        .CustomInstantiator(f => new Order(
            f.Name.FullName(),
            f.Internet.Email(),
            f.Random.Decimal(1m, 500m),
            f.Date.Between(
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc))))
        .Generate(100);
}
```

`ValidGeneratedOrdersPass` keeps the loop and the assertion; only the fixture moved.

## When to use Bogus

- Generate the bulk of valid data for a path. Keep the edge cases and the expected messages explicit.
- Seed every faker used in a test. A failure that cannot be reproduced is a rumour.
- Fix everything else the seed does not control: dates, identifiers, locale, and external state.
- Do not generate the expected value with the same rules that generate the input. The generator is not an oracle.
- A generated value that appears in an assertion is fine only while the seed and the library version stay pinned.

## Verification

```text
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 98 ms
```

The four tests cover a hundred generated valid orders, a bad email, a future date and the seed guarantee.

## Wrapping up

What we have covered:

- Adding Bogus to a test project and pinning its version.
- Building a positional record with `CustomInstantiator`.
- Using generated data for the happy path and literal values for edge cases.
- A seed that reproduces the random choices, and a fixed window for the timestamps it does not control.
- A helper that keeps the generation rules in one place.

Next: CI/CD.
