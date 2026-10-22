# Decimals

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/decimal)

An invoice total should add two tenths to exactly three tenths. With `double`, it does not:

Write a test that adds two tenths:

```csharp
[TestMethod]
public void AddTwoTenths()
{
    var total = 0.1 + 0.2;

    Assert.AreEqual(0.3, total);
}
```

Run it:

```text
  Failed AddTwoTenths [13 ms]
  Error Message:
   Assert.AreEqual failed. Expected:<0.3>. Actual:<0.30000000000000004>.
```

## How it works

The literals `0.1`, `0.2` and `0.3` are `double` values, and `double` stores binary fractions. None of those numbers has an exact binary representation, so each is rounded to the nearest value the type can hold and the sum carries the error. The error is tiny; across a long invoice it is still money.

C# also has `decimal`, which stores a base-10 significand and scale. The `m` suffix selects it, so these tenths can be represented exactly:

```csharp
[TestMethod]
public void AddTwoTenthsWithDecimal()
{
    var total = 0.1m + 0.2m;

    Assert.AreEqual(0.3m, total);
}
```

Run it. Green. Only the suffix changed, and the arithmetic is now exact.

## How to test

These arithmetic examples need no I/O or timing. After confirming the failure above, keep passing tests that pin the difference between `double` and `decimal`:

```csharp
[TestMethod]
public void DoubleTenthsAreNotExact()
{
    Assert.AreNotEqual(0.3, 0.1 + 0.2);
}

[TestMethod]
public void DecimalTenthsAreExact()
{
    Assert.AreEqual(0.3m, 0.1m + 0.2m);
}
```

Both pass, and together they say what the chapter is about: the same digits give different results depending on the type. The suite should not keep the failing test around; replace it with the `DoubleTenthsAreNotExact` assertion.

## Write the test first

A raw `decimal` does not stop you from adding a quantity to a price or passing an amount where a count belongs. Give money a name and the operations it needs. The finished solution is in `fundamentals/decimal`; set up your project the same way as the previous chapters if you are following along from scratch.

```csharp
[TestMethod]
public void AddTwoAmounts()
{
    var total = new Money(5.5m).Add(new Money(2.5m));

    Assert.AreEqual(new Money(8.0m), total);
}
```

## Try to run the test

Run `dotnet test`. The compiler says:

```text
error CS0246: The type or namespace name 'Money' could not be found
```

## Write the minimal amount of code for the test to run and check the failing test output

Write the smallest type:

```csharp
namespace Decimal;

public sealed record Money(decimal Amount);
```

The test still cannot compile because `Add` does not exist:

```text
error CS1061: 'Money' does not contain a definition for 'Add'
```

## Write enough code to make it pass

Add the method:

```csharp
public sealed record Money(decimal Amount)
{
    public Money Add(Money other) => new(Amount + other.Amount);
}
```

Run the tests. Green. A `record` gives us value equality for free, which is why `Assert.AreEqual(new Money(8.0m), total)` compares the amounts instead of the references. `new(Amount + other.Amount)` uses the target-typed `new` introduced in C# 9; the compiler knows the result is a `Money`.

## Refactor

The immutable record and its `Add` method are already small and direct. Run the test again; it stays green. There is nothing to extract, so add the next operation as a separate requirement.

## Repeat for new requirements

### Multiply an amount by a quantity

Write the next test:

```csharp
[TestMethod]
public void MultiplyPriceByQuantity()
{
    var total = new Money(19.95m).Multiply(3);

    Assert.AreEqual(new Money(59.85m), total);
}
```

The compiler says `Multiply` is missing. Add it:

```csharp
public Money Multiply(int quantity) => new(Amount * quantity);
```

Green. The quantity is an `int` because you buy a whole number of items; the amount stays a `decimal` because money is decimal.

### Rounding

Money has one more decision that the type cannot make for you: what to do with a midpoint. Write a test that forces the question:

```csharp
[TestMethod]
public void RoundingAwayFromZero()
{
    var rounded = new Money(10.545m).Rounded(2);

    Assert.AreEqual(new Money(10.55m), rounded);
}
```

Add the method with the obvious implementation:

```csharp
public Money Rounded(int places = 2) => new(Math.Round(Amount, places));
```

Run the test. It fails:

```text
  Failed RoundingAwayFromZero [13 ms]
  Error Message:
   Assert.AreEqual failed. Expected:<10.55>. Actual:<10.54>.
```

The default for `Math.Round` is banker's rounding, also called round half to even. The digit before the midpoint is `4`, which is even, so the value stays at `10.54`. That is a reasonable default for statistics and a surprising one for an invoice.

Make the policy explicit:

```csharp
public Money Rounded(int places = 2) =>
    new(Math.Round(Amount, places, MidpointRounding.AwayFromZero));
```

Run the tests. Green. `MidpointRounding.AwayFromZero` makes the midpoint rule explicit: for this positive amount, `10.545` becomes `10.55`. The policy is visible in code review instead of hiding in an overload default.

## Wrapping up

What we have covered:

- Why `double` cannot represent `0.1`, `0.2` and `0.3` exactly, and why the error matters for money.
- The `m` suffix and `decimal` arithmetic.
- A `record` for an amount, with value equality and two operations.
- Integer quantities multiplied by decimal amounts.
- `Math.Round` and an explicit midpoint policy.

The [Maths chapter of Learn Go with Tests](https://quii.gitbook.io/learn-go-with-tests/maths) starts from a concrete problem and lets the tests drive the design. This chapter does the same with a smaller problem: money that has to add up.

Next: [Declarations, const and var](../declarations/declarations.md).

