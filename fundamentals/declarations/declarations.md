# Declarations, const and var

[You can find all the code for this chapter here](https://github.com/jerolan/learn-dotnet-with-tests/tree/main/fundamentals/declarations)

A declaration gives a name to a value. You can write the type yourself, let the compiler infer it with `var`, or promise that the value never changes with `const`. The three choices look small and decide how much the compiler can check for you.

Let's learn them by building a price calculator. It takes a price and returns the price plus VAT. The finished solution is in `fundamentals/declarations`; if you are building it from scratch, set it up like the previous chapters: a class library in `src/Declarations`, an MSTest project in `tests/Declarations.Tests`, a solution and a project reference.

## Write the test first

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Declarations.Tests;

[TestClass]
public sealed class PriceCalculatorTests
{
    [TestMethod]
    public void TotalWithVat_AddsTheDefaultRate()
    {
        var calculator = new PriceCalculator();

        Assert.AreEqual(116m, calculator.TotalWithVat(100m));
    }
}
```

The `m` suffix makes `100m` and `116m` decimal values. Money stays decimal; the previous chapter explained why.

## Try to run the test

Run `dotnet test`:

```text
error CS0246: The type or namespace name 'PriceCalculator' could not be found
```

The test names a type that does not exist. That is the first red.

## Write the minimal amount of code for the test to run and check the failing test output

Create the type with a method that compiles:

```csharp
namespace Declarations;

public sealed class PriceCalculator
{
    public decimal TotalWithVat(decimal price)
    {
        return 0;
    }
}
```

Run the tests. The code compiles and the test fails with `Expected:<116>. Actual:<0>`.

## Write enough code to make it pass

Write the calculation directly:

```csharp
public decimal TotalWithVat(decimal price)
{
    return price * 1.16m;
}
```

Green. The `1.16m` literal is the price plus 16 percent, expressed as one number. It works, but it hides the rate inside the expression.

## Refactor

Extract the rate into a constant:

```csharp
public const decimal DefaultVatRate = 0.16m;

public decimal TotalWithVat(decimal price)
{
    var vat = price * DefaultVatRate;
    return price + vat;
}
```

Run the tests. Green, and the code now says what `1.16m` meant.

A `const` is a compile-time constant. The compiler substitutes the value wherever it is used, which is why it must be a literal or another constant. Constants are implicitly static: they belong to the type, not to an instance.

The `var` keyword is different. It is local type inference: the compiler looks at the right-hand side and gives the local that type. Here `vat` is a `decimal` because `price * DefaultVatRate` is decimal. The type is fixed at compile time; `var` is not a dynamic type.

That also means `var` is only for locals. You cannot declare a field or a parameter with it, and you cannot change the type later:

```csharp
var total = 0;      // int
total = 1.5m;       // error CS0266: Cannot implicitly convert type decimal to int
```

Use `var` when the right-hand side makes the type obvious, as in `var vat = price * DefaultVatRate`. Write the type when the reader would otherwise have to guess, especially for method results that do not announce their type in the name.

## Repeat for new requirements

A real calculator needs more than one rate: some products pay 16 percent, some pay 8 percent, some pay none. The rate belongs to the calculator, not to the type.

Write the test:

```csharp
[TestMethod]
public void TotalWithVat_UsesTheRatePassedToTheConstructor()
{
    var calculator = new PriceCalculator(0.08m);

    Assert.AreEqual(108m, calculator.TotalWithVat(100m));
}
```

Run the tests. The compiler says:

```text
error CS1729: 'PriceCalculator' does not contain a constructor that takes 1 arguments
```

Add a field that each calculator can set once:

```csharp
public sealed class PriceCalculator
{
    public const decimal DefaultVatRate = 0.16m;

    private readonly decimal _vatRate;

    public PriceCalculator()
        : this(DefaultVatRate)
    {
    }

    public PriceCalculator(decimal vatRate)
    {
        _vatRate = vatRate;
    }

    public decimal TotalWithVat(decimal price)
    {
        var vat = price * _vatRate;
        return price + vat;
    }
}
```

Run the tests. Both pass.

`readonly` means the field can be assigned in the constructor and nowhere else. After construction it behaves like a constant for that object, but each object can hold a different value. That is the difference from `const`: `const` is one value for the whole type, fixed at compile time; `readonly` is one value per instance, fixed when the instance is created.

Try to assign `_vatRate` from `TotalWithVat` and the compiler stops you:

```text
error CS0191: A readonly field cannot be assigned to (except in a constructor or init-only setter of the type in which the field is defined or a variable initializer)
```

## Wrapping up

What we have covered:

- Explicit types and inferred locals with `var`.
- `var` is local only and the inferred type cannot change.
- `const` for a value fixed at compile time and shared by the type.
- `readonly` for a value fixed per instance in the constructor.
- A calculator whose default rate is a constant and whose actual rate is a readonly field.

Next: [Control Flow](../control-flow/control-flow.md), where the calculator starts making decisions and repeating work.

